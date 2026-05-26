using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>Paridad rendiciones <c>CajaChicaController</c>.</summary>
public sealed class CajaChicaRendicionWriteService(
    IOptions<SqlDatabaseOptions> options,
    ICajaChicaEntradaSalidaWriteService entradaSalida) : ICajaChicaRendicionWriteService
{
    private const int TipoOperacionDevolucionId = 18;
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<string> CrearAsync(
        CrearRendicionCajaChicaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MovimientoCajaChicaId < 1 || request.TipoDocumentoId < 1 || request.Importe <= 0)
        {
            throw new ArgumentException("Parámetros inválidos.", nameof(request));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var mov = await connection.QuerySingleOrDefaultAsync<MovimientoRow>(
            new CommandDefinition(
                """
                SELECT mc.Importe,
                       ISNULL(SUM(r.Importe), 0) AS SumRendido
                FROM CREDITO.MovimientoCajaChica AS mc
                LEFT JOIN CREDITO.MovimientoRendidoCajaChica AS r
                    ON r.MovimientoCajaChicaId = mc.Id
                WHERE mc.Id = @Id
                GROUP BY mc.Importe;
                """,
                new { Id = request.MovimientoCajaChicaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (mov is null)
        {
            return "Movimiento de caja chica no encontrado.";
        }

        if (mov.SumRendido + request.Importe > mov.Importe)
        {
            return $"La suma de las rendiciones debe ser menor o igual {mov.Importe}";
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO CREDITO.MovimientoRendidoCajaChica (
                    MovimientoCajaChicaId,
                    TipoDocumentoId,
                    Fecha,
                    Serie,
                    Numero,
                    RUC,
                    RazonSocial,
                    DetalleGasto,
                    Importe)
                VALUES (
                    @MovimientoCajaChicaId,
                    @TipoDocumentoId,
                    @Fecha,
                    @Serie,
                    @Numero,
                    @Ruc,
                    @RazonSocial,
                    @DetalleGasto,
                    @Importe);
                """,
                new
                {
                    request.MovimientoCajaChicaId,
                    request.TipoDocumentoId,
                    Fecha = request.Fecha.Date,
                    Serie = request.Serie?.Trim() ?? string.Empty,
                    Numero = request.Numero?.Trim() ?? string.Empty,
                    Ruc = request.Ruc?.Trim() ?? string.Empty,
                    RazonSocial = request.RazonSocial?.Trim() ?? string.Empty,
                    DetalleGasto = request.DetalleGasto?.Trim() ?? string.Empty,
                    request.Importe,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return string.Empty;
    }

    public async Task DeleteAsync(int rendicionId, CancellationToken cancellationToken = default)
    {
        if (rendicionId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rendicionId));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE FROM CREDITO.MovimientoRendidoCajaChica
                WHERE Id = @Id;
                """,
                new { Id = rendicionId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<(string? Error, CerrarRendicionCajaChicaResponse? Result)> CerrarAsync(
        int movimientoCajaChicaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoCajaChicaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaChicaId));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var mov = await connection.QuerySingleOrDefaultAsync<MovimientoCierreRow>(
                new CommandDefinition(
                    """
                    SELECT mc.Id,
                           mc.PersonaId,
                           mc.Importe,
                           ISNULL(SUM(r.Importe), 0) AS SumRendido
                    FROM CREDITO.MovimientoCajaChica AS mc
                    LEFT JOIN CREDITO.MovimientoRendidoCajaChica AS r
                        ON r.MovimientoCajaChicaId = mc.Id
                    WHERE mc.Id = @Id
                    GROUP BY mc.Id, mc.PersonaId, mc.Importe;
                    """,
                    new { Id = movimientoCajaChicaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (mov is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("Movimiento no encontrado.", null);
            }

            var devolucion = mov.Importe - mov.SumRendido;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.MovimientoCajaChica
                    SET IndRendido = CAST(1 AS bit),
                        ImporteRendido = @ImporteRendido
                    WHERE Id = @Id;
                    """,
                    new { Id = movimientoCajaChicaId, ImporteRendido = mov.SumRendido },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            if (devolucion > 0)
            {
                var (error, _) = await entradaSalida
                    .EjecutarAsync(
                        oficinaId: 0,
                        mov.PersonaId,
                        TipoOperacionDevolucionId,
                        devolucion,
                        "DEVOLUCION DE GASTO",
                        usuarioId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(error))
                {
                    return (error, null);
                }
            }

            return (null, new CerrarRendicionCajaChicaResponse(devolucion));
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

    private sealed class MovimientoRow
    {
        public decimal Importe { get; init; }
        public decimal SumRendido { get; init; }
    }

    private sealed class MovimientoCierreRow
    {
        public int Id { get; init; }
        public int PersonaId { get; init; }
        public decimal Importe { get; init; }
        public decimal SumRendido { get; init; }
    }
}
