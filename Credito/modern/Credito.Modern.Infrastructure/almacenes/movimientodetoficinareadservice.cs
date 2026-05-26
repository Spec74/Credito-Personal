using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class MovimientoDetOficinaReadService(IOptions<SqlDatabaseOptions> options) : IMovimientoDetOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByMovimientoDetIdAsync(
        int movimientoDetId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoDetId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoDetId), "movimientoDetId debe ser >= 1.");
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
                FROM ALMACEN.MovimientoDet AS md
                INNER JOIN ALMACEN.Movimiento AS m ON m.MovimientoId = md.MovimientoId
                INNER JOIN ALMACEN.Almacen AS a ON a.AlmacenId = m.AlmacenId
                WHERE md.MovimientoDetId = @MovimientoDetId;
                """,
                new { MovimientoDetId = movimientoDetId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
