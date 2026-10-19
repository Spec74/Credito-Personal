using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class GastosAdmReadService(IOptions<SqlDatabaseOptions> options) : IGastosAdmReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<decimal> CalcularAsync(
        decimal importe,
        bool incluyeCentralRiesgo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (importe <= 0)
        {
            return 0m;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

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
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var filtered = incluyeCentralRiesgo ? rows : rows.Where(static x => !x.Ind);

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
