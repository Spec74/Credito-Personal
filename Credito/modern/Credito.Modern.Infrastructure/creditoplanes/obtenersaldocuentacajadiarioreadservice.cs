using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class ObtenerSaldoCuentaCajaDiarioReadService(IOptions<SqlDatabaseOptions> options)
    : IObtenerSaldoCuentaCajaDiarioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<decimal?> ObtenerAsync(
        int cajaDiarioId,
        int tipoPagoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (tipoPagoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tipoPagoId), "tipoPagoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_ObtenerSaldoCuentaCajadiario",
            new { CajaDiarioId = cajaDiarioId, TipoPagoId = tipoPagoId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<decimal?>(command).ConfigureAwait(false);
    }
}
