using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class VentaRapidaWriteService(IOptions<SqlDatabaseOptions> options) : IVentaRapidaWriteService
{
    private const decimal Igv = 0.18m;
    private const int SerieEnAlmacen = 2;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<(string? Error, RealizarPedidoResponse? Result)> RealizarPedidoAsync(
        int oficinaId,
        int cajaDiarioId,
        int personaId,
        int usuarioId,
        DateTime fechaReg,
        IReadOnlyList<PedidoLineaRequest> pedidos,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || cajaDiarioId < 1 || personaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        if (pedidos is null || pedidos.Count == 0)
        {
            return ("Debe incluir al menos una línea de pedido.", null);
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var lineas = new List<LineaPreparada>();
        foreach (var pedido in pedidos)
        {
            if (pedido.ArticuloId < 1 || pedido.Cantidad < 1)
            {
                return ("Cada línea debe tener articuloId y cantidad >= 1.", null);
            }

            var articulo = await connection.QueryFirstOrDefaultAsync<ArticuloPrecioRow>(
                new CommandDefinition(
                    """
                    SELECT TOP (1)
                        a.ArticuloId,
                        a.Denominacion,
                        lp.Monto AS Precio
                    FROM ALMACEN.Articulo AS a
                    INNER JOIN VENTAS.ListaPrecio AS lp ON lp.ArticuloId = a.ArticuloId AND lp.Estado = CAST(1 AS bit)
                    WHERE a.ArticuloId = @ArticuloId
                      AND a.Estado = CAST(1 AS bit)
                    ORDER BY lp.ListaPrecioId;
                    """,
                    new { pedido.ArticuloId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (articulo is null)
            {
                return ($"No existe artículo {pedido.ArticuloId} con lista de precios activa.", null);
            }

            var series = (await connection.QueryAsync<int>(
                new CommandDefinition(
                    """
                    SELECT TOP (@Cantidad) SerieArticuloId
                    FROM ALMACEN.SerieArticulo
                    WHERE ArticuloId = @ArticuloId
                      AND EstadoId = @EstadoId
                    ORDER BY SerieArticuloId;
                    """,
                    new
                    {
                        pedido.ArticuloId,
                        pedido.Cantidad,
                        EstadoId = SerieEnAlmacen,
                    },
                    cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

            if (series.Count < pedido.Cantidad)
            {
                return ($"Stock insuficiente para el artículo {pedido.ArticuloId}.", null);
            }

            var precio = articulo.Precio;
            var subtotal = pedido.Cantidad * (precio - pedido.Descuento);
            lineas.Add(
                new LineaPreparada(
                    pedido.ArticuloId,
                    pedido.Cantidad,
                    pedido.Descuento,
                    articulo.Denominacion,
                    precio,
                    subtotal,
                    series));
        }

        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var ordenVentaId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO VENTAS.OrdenVenta (
                        OficinaId, Subtotal, TotalNeto, TotalImpuesto, TotalDescuento,
                        Estado, UsuarioRegId, FechaReg, PersonaId, TipoVenta)
                    VALUES (
                        @OficinaId, 0, 0, 0, 0,
                        'ENV', @UsuarioRegId, @FechaReg, @PersonaId, 'CON');
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        OficinaId = oficinaId,
                        UsuarioRegId = usuarioId,
                        FechaReg = fechaReg,
                        PersonaId = personaId,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            decimal totalNeto = 0;
            decimal totalDescuento = 0;

            foreach (var linea in lineas)
            {
                var detalleId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO VENTAS.OrdenVentaDet (
                            OrdenVentaId, ArticuloId, Cantidad, Descripcion, ValorVenta,
                            Descuento, Subtotal, Estado)
                        VALUES (
                            @OrdenVentaId, @ArticuloId, @Cantidad, @Descripcion, @ValorVenta,
                            @Descuento, @Subtotal, CAST(1 AS bit));
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        new
                        {
                            OrdenVentaId = ordenVentaId,
                            linea.ArticuloId,
                            linea.Cantidad,
                            linea.Descripcion,
                            ValorVenta = linea.Precio,
                            linea.Descuento,
                            linea.Subtotal,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                foreach (var serieId in linea.Series)
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            """
                            INSERT INTO VENTAS.OrdenVentaDetSerie (OrdenVentaDetId, SerieArticuloId)
                            VALUES (@OrdenVentaDetId, @SerieArticuloId);
                            """,
                            new { OrdenVentaDetId = detalleId, SerieArticuloId = serieId },
                            transaction: transaction,
                            cancellationToken: cancellationToken)).ConfigureAwait(false);
                }

                totalNeto += linea.Subtotal;
                totalDescuento += linea.Descuento;
            }

            var subtotal = Math.Round(totalNeto / (1 + Igv), 2);
            var totalImpuesto = Math.Round(totalNeto - subtotal, 2);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE VENTAS.OrdenVenta
                    SET TotalNeto = @TotalNeto,
                        TotalDescuento = @TotalDescuento,
                        Subtotal = @Subtotal,
                        TotalImpuesto = @TotalImpuesto
                    WHERE OrdenVentaId = @OrdenVentaId;
                    """,
                    new
                    {
                        OrdenVentaId = ordenVentaId,
                        TotalNeto = totalNeto,
                        TotalDescuento = totalDescuento,
                        Subtotal = subtotal,
                        TotalImpuesto = totalImpuesto,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var resultId = await connection.QueryFirstOrDefaultAsync<int?>(
                new CommandDefinition(
                    "CREDITO.usp_PagarCuentaxCobrar",
                    new
                    {
                        OrdenVentaId = ordenVentaId,
                        CuentaxCobrarId = 0,
                        CajaDiarioId = cajaDiarioId,
                        UsuarioId = usuarioId,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (resultId is null or < 0)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("El procedimiento de pago no completó la venta (resultado negativo o nulo).", null);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, new RealizarPedidoResponse(ordenVentaId, resultId));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private sealed record LineaPreparada(
        int ArticuloId,
        int Cantidad,
        decimal Descuento,
        string Descripcion,
        decimal Precio,
        decimal Subtotal,
        IReadOnlyList<int> Series);

    private sealed class ArticuloPrecioRow
    {
        public int ArticuloId { get; init; }
        public string Denominacion { get; init; } = string.Empty;
        public decimal Precio { get; init; }
    }
}
