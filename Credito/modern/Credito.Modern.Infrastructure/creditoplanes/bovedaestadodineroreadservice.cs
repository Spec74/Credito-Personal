using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaEstadoDineroReadService(
    IOptions<SqlDatabaseOptions> options,
    ICreditoVencidoMetricasReadService creditoVencido) : IBovedaEstadoDineroReadService
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
                SELECT TOP (1) ccd.SaldoFinal
                FROM CREDITO.CajaChicaDiario AS ccd
                WHERE ccd.IndCierre = CAST(0 AS bit)
                  AND ccd.TransBoveda = CAST(0 AS bit)
                ORDER BY ccd.Id DESC;
                """,
                cancellationToken: cancellationToken)).ConfigureAwait(false) ?? 0m;

        var montoPlan = await connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                "CREDITO.usp_ObtenerMontoPendientePlanPago",
                new { OficinaId = oficinaId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false) ?? 0m;

        var vencido = await creditoVencido
            .ObtenerCarteraAsync(cancellationToken)
            .ConfigureAwait(false);

        var cv = vencido?.CreditoVencido ?? 0m;
        var menor = vencido?.VencidoMenor60 ?? 0m;
        var mayor = vencido?.VencidoMayor60 ?? 0m;
        var irrec = vencido?.VencidoIrrecuperable ?? 0m;
        var total = montoCajaChica + montoCajas + saldoBovedaAbierta + montoPlan + cv;

        return new BovedaEstadoDineroDto(
            saldoBovedaAbierta,
            montoCajaChica,
            montoCajas,
            montoPlan,
            cv,
            menor,
            mayor,
            irrec,
            total);
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
