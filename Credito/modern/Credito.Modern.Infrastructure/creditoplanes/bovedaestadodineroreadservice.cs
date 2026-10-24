using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaEstadoDineroReadService(
    IOptions<SqlDatabaseOptions> options) : IBovedaEstadoDineroReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<BovedaEstadoDineroDto> ObtenerAsync(
        int oficinaId,
        decimal saldoBovedaAbierta,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId));
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var montoCajas = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                """
                SELECT ISNULL(SUM(cd.SaldoFinal), 0)
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.IndCierre = CAST(0 AS bit)
                  AND cd.TransBoveda = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var montoCajaChica = await connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                """
                SELECT ISNULL(SUM(ccd.SaldoFinal), 0)
                FROM CREDITO.CajaChicaDiario AS ccd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = ccd.Id
                WHERE ccd.IndCierre = CAST(0 AS bit)
                  AND ccd.TransBoveda = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false) ?? 0m;

        decimal montoPlan = 0m;
        try
        {
            montoPlan = await connection.ExecuteScalarAsync<decimal?>(
                new CommandDefinition(
                    "CREDITO.usp_ObtenerMontoPendientePlanPago",
                    new { OficinaId = oficinaId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 8,
                    cancellationToken: cancellationToken)).ConfigureAwait(false) ?? 0m;
        }
        catch (SqlException)
        {
            montoPlan = 0m;
        }

        // Cartera vencida no es efectivo: no se consulta aquí (tabla grande).
        // La baldosa de la página es atajo al informe, no un KPI de tesorería.
        const decimal cv = 0m, menor = 0m, mayor = 0m, irrec = 0m;
        var efectivoOficina = montoCajaChica + montoCajas + saldoBovedaAbierta;

        return new BovedaEstadoDineroDto(
            saldoBovedaAbierta,
            montoCajaChica,
            montoCajas,
            montoPlan,
            cv,
            menor,
            mayor,
            irrec,
            efectivoOficina,
            efectivoOficina);
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
