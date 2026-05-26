using Credito.Modern.Application.RolAdmin;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.RolAdmin;

public sealed class RolAdminReadService(IOptions<SqlDatabaseOptions> options) : IRolAdminReadService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<List<RolGestionListItemDto>> ListGestionAsync(
        bool incluirInactivos,
        CancellationToken ct = default)
    {
        Ensure();
        var estadoFilter = incluirInactivos ? "" : "WHERE r.Estado = CAST(1 AS bit)";
        const string sql = """
            SELECT r.RolId,
                   r.Denominacion,
                   r.Estado
            FROM MAESTRO.Rol AS r
            """;
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rows = await c.QueryAsync<RolGestionListItemDto>(
            new CommandDefinition($"{sql} {estadoFilter} ORDER BY r.Denominacion", cancellationToken: ct))
            .ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<RolMenusDetalleDto?> GetMenusDetalleAsync(int rolId, CancellationToken ct = default)
    {
        Ensure();
        const string sqlRol = """
            SELECT RolId, Denominacion, Estado
            FROM MAESTRO.Rol
            WHERE RolId = @RolId;
            """;

        const string sqlMenus = """
            SELECT m.MenuId,
                   m.Modulo + N' - ' + m.Denominacion AS Denominacion,
                   CAST(CASE WHEN rm.RolMenuId IS NOT NULL THEN 1 ELSE 0 END AS bit) AS Asignado
            FROM MAESTRO.Menu AS m
            LEFT JOIN MAESTRO.RolMenu AS rm
                ON rm.MenuId = m.MenuId AND rm.RolId = @RolId
            WHERE m.IndPadre = CAST(0 AS bit)
            ORDER BY m.Modulo, m.Denominacion;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rol = await c.QuerySingleOrDefaultAsync<RolDetalleDto>(
            new CommandDefinition(sqlRol, new { RolId = rolId }, cancellationToken: ct))
            .ConfigureAwait(false);
        if (rol is null)
            return null;

        var menus = (await c.QueryAsync<MenuAsignacionDto>(
            new CommandDefinition(sqlMenus, new { RolId = rolId }, cancellationToken: ct))
            .ConfigureAwait(false)).ToList();

        return new RolMenusDetalleDto(rol, menus);
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}
