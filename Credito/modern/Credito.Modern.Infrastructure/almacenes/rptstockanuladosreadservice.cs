using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class RptStockAnuladosReadService(IOptions<SqlDatabaseOptions> options) : IRptStockAnuladosReadService
{
    private const string Sql = """
        SELECT md.MovimientoId,
               tm.Descripcion AS Movimiento,
               m.Observacion,
               m.Fecha,
               md.Cantidad,
               md.Descripcion AS Detalle
        FROM ALMACEN.MovimientoDet AS md
        INNER JOIN ALMACEN.Movimiento AS m ON m.MovimientoId = md.MovimientoId
        INNER JOIN ALMACEN.TipoMovimiento AS tm ON tm.TipoMovimientoId = m.TipoMovimientoId
        WHERE m.EstadoId = 3
          AND m.TipoMovimientoId <> 2
          AND tm.IndEntrada = CAST(0 AS bit)
          AND (tm.IndDevolucion IS NULL OR tm.IndDevolucion = CAST(0 AS bit))
          AND (tm.IndTransferencia IS NULL OR tm.IndTransferencia = CAST(0 AS bit))
        ORDER BY m.Fecha;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptStockAnuladoRowDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(Sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptStockAnuladoRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
