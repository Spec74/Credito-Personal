using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class AlmacenOficinaReadService(IOptions<SqlDatabaseOptions> options) : IAlmacenOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByAlmacenIdAsync(int almacenId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (almacenId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(almacenId), "almacenId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = "SELECT OficinaId FROM ALMACEN.Almacen WHERE AlmacenId = @AlmacenId;";
        var command = new CommandDefinition(
            sql,
            new { AlmacenId = almacenId },
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
    }
}
