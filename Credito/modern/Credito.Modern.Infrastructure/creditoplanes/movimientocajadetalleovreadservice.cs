using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MovimientoCajaDetalleOvReadService(IOptions<SqlDatabaseOptions> options)
    : IMovimientoCajaDetalleOvReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<MovimientoCajaDetalleOvDto?> GetAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var header = await connection.QueryFirstOrDefaultAsync<HeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    mc.MovimientoCajaId,
                    RTRIM(mc.Operacion) AS Operacion,
                    c.OficinaId
                FROM CREDITO.MovimientoCaja AS mc
                INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE mc.MovimientoCajaId = @MovimientoCajaId;
                """,
                new { MovimientoCajaId = movimientoCajaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (header is null)
        {
            return null;
        }

        var lineasSql = header.Operacion switch
        {
            "INI" => """
                SELECT ovd.Descripcion
                FROM CREDITO.CuentaxCobrar AS cx
                INNER JOIN CREDITO.Credito AS cr ON cr.CreditoId = cx.CreditoId
                INNER JOIN VENTAS.OrdenVentaDet AS ovd ON ovd.OrdenVentaId = cr.OrdenVentaId AND ovd.Estado = 1
                WHERE cx.MovimientoCajaId = @MovimientoCajaId
                ORDER BY ovd.OrdenVentaDetId;
                """,
            "CON" => """
                SELECT ovd.Descripcion
                FROM CREDITO.MovimientoCaja AS mc
                INNER JOIN VENTAS.OrdenVentaDet AS ovd ON ovd.OrdenVentaId = mc.OrdenVentaId AND ovd.Estado = 1
                WHERE mc.MovimientoCajaId = @MovimientoCajaId
                ORDER BY ovd.OrdenVentaDetId;
                """,
            "CUO" => """
                SELECT ovd.Descripcion
                FROM CREDITO.PlanPago AS pp
                INNER JOIN CREDITO.Credito AS cr ON cr.CreditoId = pp.CreditoId
                INNER JOIN VENTAS.OrdenVentaDet AS ovd ON ovd.OrdenVentaId = cr.OrdenVentaId AND ovd.Estado = 1
                WHERE pp.MovimientoCajaId = @MovimientoCajaId
                ORDER BY ovd.OrdenVentaDetId;
                """,
            _ => null,
        };

        IReadOnlyList<string> lineas = [];
        if (lineasSql is not null)
        {
            var rows = await connection
                .QueryAsync<string>(
                    new CommandDefinition(
                        lineasSql,
                        new { MovimientoCajaId = movimientoCajaId },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            lineas = rows
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();
        }

        return new MovimientoCajaDetalleOvDto(
            header.MovimientoCajaId,
            header.OficinaId,
            header.Operacion,
            lineas);
    }

    private sealed class HeaderRow
    {
        public int MovimientoCajaId { get; init; }
        public string Operacion { get; init; } = string.Empty;
        public int OficinaId { get; init; }
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
