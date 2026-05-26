using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptComprobantesCajaChicaReadService(IOptions<SqlDatabaseOptions> options)
    : IRptComprobantesCajaChicaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptComprobantesCajaChicaRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        var yi = fechaIni.Year;
        var yf = fechaFin.Year;
        if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaIni), "Las fechas deben tener año entre 1900 y 2100.");
        }

        if (fechaIni.Date > fechaFin.Date)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaIni), "fechaIni no puede ser posterior a fechaFin.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<RptComprobantesCajaChicaRowDto>(
            new CommandDefinition(
                """
                SELECT
                    mc.Descripcion AS Gasto,
                    r.Fecha,
                    td.Denominacion AS Documento,
                    r.Serie,
                    r.Numero,
                    r.RUC AS Ruc,
                    r.RazonSocial,
                    r.DetalleGasto,
                    r.Importe
                FROM CREDITO.MovimientoRendidoCajaChica AS r
                INNER JOIN CREDITO.MovimientoCajaChica AS mc ON mc.Id = r.MovimientoCajaChicaId
                INNER JOIN MAESTRO.TipoDocumento AS td ON td.TipoDocumentoId = r.TipoDocumentoId
                WHERE CAST(r.Fecha AS date) >= @FechaIni
                  AND CAST(r.Fecha AS date) <= @FechaFin
                  AND mc.IndRendido = CAST(1 AS bit)
                ORDER BY r.Fecha DESC, r.Id DESC;
                """,
                new { FechaIni = fechaIni.Date, FechaFin = fechaFin.Date },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
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
