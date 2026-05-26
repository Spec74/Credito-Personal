using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class OrdenVentaEnvioWriteService(IOptions<SqlDatabaseOptions> options) : IOrdenVentaEnvioWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<EnviarOrdenVentaResponse> EnviarContadoAsync(
        int ordenVentaId,
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaId), "Parámetros inválidos.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE VENTAS.OrdenVenta
                    SET Estado = 'ENV',
                        TipoVenta = 'CON',
                        UsuarioModId = @UsuarioId,
                        FechaMod = @FechaMod
                    WHERE OrdenVentaId = @OrdenVentaId;
                    """,
                    new { OrdenVentaId = ordenVentaId, UsuarioId = usuarioId, FechaMod = fechaOperacion },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new EnviarOrdenVentaResponse(ordenVentaId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<EnviarOrdenVentaResponse> EnviarCreditoAsync(
        int ordenVentaId,
        int oficinaId,
        int personaId,
        decimal totalNeto,
        IReadOnlyList<string> descripcionesDetalle,
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaId < 1 || oficinaId < 1 || usuarioId < 1 || personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaId), "Parámetros inválidos.");
        }

        EnsureConnection();
        var glosa = string.Join(", " + Environment.NewLine, descripcionesDetalle);
        var inicial = Math.Round(totalNeto * 0.15m, 2);
        var montoCredito = totalNeto - inicial;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var montoGastosAdm = await CalcularGastosAdmAsync(
                connection,
                transaction,
                montoCredito,
                incluyeCentralRiesgo: true,
                cancellationToken).ConfigureAwait(false);

            var creditoId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.Credito (
                        PersonaId, ProductoId, Descripcion, MontoProducto, MontoInicial, MontoCredito,
                        MontoGastosAdm, CentralRiesgo, MontoDesembolso, TipoGastoAdm, FormaPago, NumeroCuotas,
                        Interes, FechaPrimerPago, Observacion, Estado, OficinaId, FechaVencimiento, TipoCuota,
                        Calificacion, IndCondonacion, MontoCondonacion, IndIrrecuperable, OrdenVentaId,
                        UsuarioRegId, FechaReg)
                    VALUES (
                        @PersonaId, 1, @Descripcion, @MontoProducto, @MontoInicial, @MontoCredito,
                        @MontoGastosAdm, 0, 0, 'CUO', 'D', 26,
                        7, @FechaOperacion, '', 'CRE', @OficinaId, @FechaOperacion, 'M',
                        'A', 0, 0, 0, @OrdenVentaId,
                        @UsuarioRegId, @FechaOperacion);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        PersonaId = personaId,
                        Descripcion = glosa,
                        MontoProducto = totalNeto,
                        MontoInicial = inicial,
                        MontoCredito = montoCredito,
                        MontoGastosAdm = montoGastosAdm,
                        OficinaId = oficinaId,
                        OrdenVentaId = ordenVentaId,
                        UsuarioRegId = usuarioId,
                        FechaOperacion = fechaOperacion.Date,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE VENTAS.OrdenVenta
                    SET Estado = 'ENV',
                        TipoVenta = 'CRE',
                        UsuarioModId = @UsuarioId,
                        FechaMod = @FechaMod
                    WHERE OrdenVentaId = @OrdenVentaId;
                    """,
                    new
                    {
                        OrdenVentaId = ordenVentaId,
                        UsuarioId = usuarioId,
                        FechaMod = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new EnviarOrdenVentaResponse(ordenVentaId, creditoId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private static async Task<decimal> CalcularGastosAdmAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        decimal importe,
        bool incluyeCentralRiesgo,
        CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync<GastosAdmRow>(
            new CommandDefinition(
                """
                SELECT Ind, IndPorcentaje, Valor
                FROM CREDITO.GastosAdm
                WHERE Estado = CAST(1 AS bit)
                  AND @Importe >= MontoMinimo
                  AND @Importe <= MontoMaximo;
                """,
                new { Importe = importe },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var filtered = incluyeCentralRiesgo
            ? rows
            : rows.Where(x => !x.Ind);

        decimal gastos = 0;
        foreach (var item in filtered)
        {
            gastos += item.IndPorcentaje ? importe * (item.Valor / 100m) : item.Valor;
        }

        return Math.Round(gastos, 2);
    }

    private sealed class GastosAdmRow
    {
        public bool Ind { get; init; }
        public bool IndPorcentaje { get; init; }
        public decimal Valor { get; init; }
    }
}
