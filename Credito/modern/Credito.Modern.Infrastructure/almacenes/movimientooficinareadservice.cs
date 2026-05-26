using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class MovimientoOficinaReadService(IOptions<SqlDatabaseOptions> options) : IMovimientoOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByMovimientoIdAsync(
        int movimientoId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoId), "movimientoId debe ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT a.OficinaId
                FROM ALMACEN.Movimiento AS m
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                WHERE m.MovimientoId = @MovimientoId;
                """,
                new { MovimientoId = movimientoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
