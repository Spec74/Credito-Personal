using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptSaldosCajaReadService(IOptions<SqlDatabaseOptions> options) : IRptSaldosCajaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<RptSaldosCajaRowDto>> ListarAsync(
        int cajaDiarioId,
        bool indCajaChica,
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptSaldosCaja",
            new { CajaDiarioId = cajaDiarioId, IndCajaChica = indCajaChica },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptSaldosCajaRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
