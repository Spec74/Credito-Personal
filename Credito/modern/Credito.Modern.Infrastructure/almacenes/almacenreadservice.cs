using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Maestros;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class AlmacenReadService(IOptions<SqlDatabaseOptions> options) : IAlmacenReadService
{
    private const string Sql = """
        SELECT a.AlmacenId,
               a.OficinaId,
               a.Denominacion,
               a.Descripcion,
               a.IndEstadoApertura,
               a.FechaApertura,
               a.Estado
        FROM ALMACEN.Almacen AS a
        WHERE a.Estado = CAST(1 AS bit)
          AND (@OficinaId IS NULL OR a.OficinaId = @OficinaId)
        ORDER BY a.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<AlmacenListItemDto>> GetActivosAsync(int? oficinaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtro = oficinaId is >= 1 ? oficinaId : null;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(Sql, new { OficinaId = filtro }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<AlmacenListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<AlmacenAdminListItemDto>> ListGestionAsync(
        int? oficinaId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtro = oficinaId is >= 1 ? oficinaId : null;
        var sql = incluirInactivos
            ? """
              SELECT a.AlmacenId,
                     a.OficinaId,
                     o.Denominacion AS OficinaDenominacion,
                     a.Denominacion,
                     a.Descripcion,
                     a.Estado
              FROM ALMACEN.Almacen AS a
              INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = a.OficinaId
              WHERE (@OficinaId IS NULL OR a.OficinaId = @OficinaId)
              ORDER BY a.Denominacion;
              """
            : """
              SELECT a.AlmacenId,
                     a.OficinaId,
                     o.Denominacion AS OficinaDenominacion,
                     a.Denominacion,
                     a.Descripcion,
                     a.Estado
              FROM ALMACEN.Almacen AS a
              INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = a.OficinaId
              WHERE a.Estado = CAST(1 AS bit)
                AND (@OficinaId IS NULL OR a.OficinaId = @OficinaId)
              ORDER BY a.Denominacion;
              """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<AlmacenAdminListItemDto>(
            new CommandDefinition(sql, new { OficinaId = filtro }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }
}
