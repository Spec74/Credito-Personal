using Credito.Modern.Application.CajaMaestro;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CajaMaestro;

public sealed class CajaMaestroReadService(IOptions<SqlDatabaseOptions> options) : ICajaMaestroReadService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<CajaGestionPageDto> ListGestionAsync(
        string? buscar,
        int page,
        int pageSize,
        bool incluirInactivos,
        CancellationToken ct = default)
    {
        Ensure();
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 25 : Math.Min(pageSize, 200);
        var skip = (p - 1) * ps;
        var term = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();

        var estadoFilter = incluirInactivos ? "" : "AND c.Estado = CAST(1 AS bit)";
        var buscarFilter = term is null ? "" : "AND c.Denominacion LIKE '%' + @Buscar + '%'";

        var countSql = $"""
            SELECT COUNT(1)
            FROM CREDITO.Caja AS c
            WHERE 1 = 1
              {estadoFilter}
              {buscarFilter};
            """;

        var sql = $"""
            SELECT c.CajaId,
                   c.Denominacion,
                   c.OficinaId,
                   o.Denominacion AS OficinaDenominacion,
                   c.CajeroId,
                   p.NombreCompleto AS CajeroNombre,
                   c.Estado,
                   c.IndAbierto
            FROM CREDITO.Caja AS c
            INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
            LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.CajeroId
            LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE 1 = 1
              {estadoFilter}
              {buscarFilter}
            ORDER BY c.Denominacion
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var total = await c.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { Buscar = term }, cancellationToken: ct)).ConfigureAwait(false);
        var rows = (await c.QueryAsync<CajaGestionListItemDto>(
            new CommandDefinition(sql, new { Buscar = term, Skip = skip, Take = ps }, cancellationToken: ct))
            .ConfigureAwait(false)).ToList();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)ps);
        return new CajaGestionPageDto(p, ps, total, totalPages, rows);
    }

    public async Task<List<CajaGestorItemDto>> ListGestoresActivosAsync(CancellationToken ct = default)
    {
        Ensure();
        const string sql = """
            SELECT u.UsuarioId,
                   p.NombreCompleto
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE u.Estado = CAST(1 AS bit)
            ORDER BY p.NombreCompleto;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rows = await c.QueryAsync<CajaGestorItemDto>(
            new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<CajaComboItemDto>> ListCajasActivasAsync(CancellationToken ct = default)
    {
        Ensure();
        const string sql = """
            SELECT c.CajaId,
                   c.Denominacion
            FROM CREDITO.Caja AS c
            WHERE c.Estado = CAST(1 AS bit)
            ORDER BY c.Denominacion;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rows = await c.QueryAsync<CajaComboItemDto>(
            new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<string?> GetDenominacionPorCajeroAsync(int cajeroId, CancellationToken ct = default)
    {
        if (cajeroId < 1)
        {
            return null;
        }

        Ensure();
        const string sql = """
            SELECT TOP (1) c.Denominacion
            FROM CREDITO.Caja AS c
            WHERE c.CajeroId = @CajeroId
            ORDER BY c.CajaId;
            """;

        await using var connection = new SqlConnection(_cs);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return await connection
            .ExecuteScalarAsync<string?>(
                new CommandDefinition(sql, new { CajeroId = cajeroId }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
