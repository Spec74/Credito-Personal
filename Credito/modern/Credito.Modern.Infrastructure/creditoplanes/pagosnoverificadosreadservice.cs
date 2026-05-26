using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class PagosNoVerificadosReadService(IOptions<SqlDatabaseOptions> options)
    : IPagosNoVerificadosReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<PagosNoVerificadosRowDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_PagosNoVerificados",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<PagosNoVerificadosRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
