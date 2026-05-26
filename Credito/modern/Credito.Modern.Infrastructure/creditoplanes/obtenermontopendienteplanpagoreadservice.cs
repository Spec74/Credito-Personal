using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class ObtenerMontoPendientePlanPagoReadService(IOptions<SqlDatabaseOptions> options)
    : IObtenerMontoPendientePlanPagoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<decimal?> ObtenerAsync(int oficinaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_ObtenerMontoPendientePlanPago",
            new { OficinaId = oficinaId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<decimal?>(command).ConfigureAwait(false);
    }
}
