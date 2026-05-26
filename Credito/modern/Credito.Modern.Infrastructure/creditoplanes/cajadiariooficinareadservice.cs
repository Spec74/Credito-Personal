using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaDiarioOficinaReadService(IOptions<SqlDatabaseOptions> options)
    : ICajaDiarioOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByCajaDiarioIdAsync(
        int cajaDiarioId,
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
        const string sql = """
            SELECT c.OficinaId
            FROM CREDITO.CajaDiario AS cd
            INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
            WHERE cd.CajaDiarioId = @CajaDiarioId;
            """;
        var command = new CommandDefinition(
            sql,
            new { CajaDiarioId = cajaDiarioId },
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
    }
}
