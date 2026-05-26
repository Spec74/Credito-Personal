using System.Data;
using Credito.Modern.Application.Time;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Data;

public sealed class DatabaseTimeProvider(IOptions<SqlDatabaseOptions> options) : IDatabaseTimeProvider
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<DateTime?> GetServerTimeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // En el EDMX legado (VENDIXModel.edmx) el proc está en el esquema CREDITO, no en dbo.
        var command = new CommandDefinition(
            "CREDITO.usp_FechaBD",
            parameters: null,
            transaction: null,
            commandTimeout: 30,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DateTime?>(command).ConfigureAwait(false);
    }
}
