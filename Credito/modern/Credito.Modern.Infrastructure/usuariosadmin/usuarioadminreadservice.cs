using Credito.Modern.Application.UsuariosAdmin;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.UsuariosAdmin;

public sealed class UsuarioAdminReadService(IOptions<SqlDatabaseOptions> options) : IUsuarioAdminReadService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<UsuarioGestionPageDto> ListGestionAsync(
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

        var estadoFilter = incluirInactivos ? "" : "AND u.Estado = CAST(1 AS bit)";
        var buscarFilter = term is null ? "" : "AND u.NombreUsuario LIKE '%' + @Buscar + '%'";

        var countSql = $"""
            SELECT COUNT(1)
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE 1 = 1
              {estadoFilter}
              {buscarFilter};
            """;

        var sql = $"""
            SELECT u.UsuarioId,
                   u.NombreUsuario,
                   p.NombreCompleto,
                   p.Celular1,
                   p.EmailPersonal,
                   u.Estado
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE 1 = 1
              {estadoFilter}
              {buscarFilter}
            ORDER BY u.NombreUsuario
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var total = await c.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { Buscar = term }, cancellationToken: ct)).ConfigureAwait(false);
        var rows = (await c.QueryAsync<UsuarioGestionListItemDto>(
            new CommandDefinition(sql, new { Buscar = term, Skip = skip, Take = ps }, cancellationToken: ct))
            .ConfigureAwait(false)).ToList();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)ps);
        return new UsuarioGestionPageDto(p, ps, total, totalPages, rows);
    }

    public async Task<IReadOnlyList<UsuarioReporteGestorDto>> ListReporteGestoresAsync(CancellationToken ct = default)
    {
        Ensure();
        const string sql = """
            SELECT u.UsuarioId,
                   u.NombreUsuario,
                   p.NombreCompleto
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE u.Estado = CAST(1 AS bit)
            ORDER BY p.NombreCompleto, u.NombreUsuario;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rows = await c.QueryAsync<UsuarioReporteGestorDto>(
            new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<UsuarioPersonaDetalleDto?> GetDetalleAsync(int usuarioId, CancellationToken ct = default)
    {
        Ensure();
        const string sqlUsuario = """
            SELECT u.UsuarioId,
                   u.NombreUsuario,
                   u.Estado,
                   p.PersonaId,
                   p.ApePaterno,
                   p.ApeMaterno,
                   p.Nombre,
                   p.NombreCompleto,
                   p.NumeroDocumento,
                   p.Sexo,
                   p.FechaNacimiento,
                   p.Celular1,
                   p.EmailPersonal,
                   p.Direccion
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE u.UsuarioId = @UsuarioId;
            """;

        const string sqlOficinas = """
            SELECT o.OficinaId,
                   o.Denominacion,
                   CAST(CASE WHEN uo.UsuarioOficinaId IS NOT NULL THEN 1 ELSE 0 END AS bit) AS Asignado
            FROM MAESTRO.Oficina AS o
            LEFT JOIN MAESTRO.UsuarioOficina AS uo
                ON uo.OficinaId = o.OficinaId
               AND uo.UsuarioId = @UsuarioId
               AND uo.Estado = CAST(1 AS bit)
            WHERE o.Estado = CAST(1 AS bit)
            ORDER BY o.Denominacion;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var row = await c.QuerySingleOrDefaultAsync<UsuarioDetalleRow>(
            new CommandDefinition(sqlUsuario, new { UsuarioId = usuarioId }, cancellationToken: ct))
            .ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        var oficinas = (await c.QueryAsync<OficinaAsignacionDto>(
            new CommandDefinition(sqlOficinas, new { UsuarioId = usuarioId }, cancellationToken: ct))
            .ConfigureAwait(false)).ToList();

        return new UsuarioPersonaDetalleDto(
            row.UsuarioId,
            row.NombreUsuario,
            row.Estado,
            row.PersonaId,
            row.ApePaterno,
            row.ApeMaterno,
            row.Nombre,
            row.NombreCompleto,
            row.NumeroDocumento,
            row.Sexo,
            row.FechaNacimiento,
            row.Celular1,
            row.EmailPersonal,
            row.Direccion,
            oficinas);
    }

    public async Task<PersonaPorDniDto?> GetPersonaPorDniAsync(string numeroDocumento, CancellationToken ct = default)
    {
        Ensure();
        const string sql = """
            SELECT PersonaId,
                   ApePaterno,
                   ApeMaterno,
                   Nombre,
                   NombreCompleto,
                   NumeroDocumento,
                   Sexo,
                   FechaNacimiento,
                   Celular1,
                   EmailPersonal,
                   Direccion
            FROM MAESTRO.Persona
            WHERE NumeroDocumento = @NumeroDocumento;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        return await c.QuerySingleOrDefaultAsync<PersonaPorDniDto>(
            new CommandDefinition(sql, new { NumeroDocumento = numeroDocumento.Trim() }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task<ValidarDniResponse> ValidarDniAsync(
        string numeroDocumento,
        int? usuarioId,
        CancellationToken ct = default)
    {
        Ensure();
        const string sql = """
            SELECT COUNT(1)
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE p.NumeroDocumento = @NumeroDocumento
              AND (@UsuarioId IS NULL OR u.UsuarioId <> @UsuarioId);
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var count = await c.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { NumeroDocumento = numeroDocumento.Trim(), UsuarioId = usuarioId is >= 1 ? usuarioId : null },
                cancellationToken: ct)).ConfigureAwait(false);
        return new ValidarDniResponse(count > 0);
    }

    public async Task<List<RolAsignacionDto>> GetRolesAsignacionAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken ct = default)
    {
        Ensure();
        const string sql = """
            SELECT r.RolId,
                   r.Denominacion,
                   CAST(CASE WHEN ur.UsuarioRolId IS NOT NULL THEN 1 ELSE 0 END AS bit) AS Asignado
            FROM MAESTRO.Rol AS r
            LEFT JOIN MAESTRO.UsuarioRol AS ur
                ON ur.RolId = r.RolId
               AND ur.UsuarioId = @UsuarioId
               AND ur.OficinaId = @OficinaId
            WHERE r.Estado = CAST(1 AS bit)
            ORDER BY r.Denominacion;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rows = await c.QueryAsync<RolAsignacionDto>(
            new CommandDefinition(
                sql,
                new { UsuarioId = usuarioId, OficinaId = oficinaId },
                cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    private sealed class UsuarioDetalleRow
    {
        public int UsuarioId { get; init; }
        public string NombreUsuario { get; init; } = "";
        public bool Estado { get; init; }
        public int PersonaId { get; init; }
        public string ApePaterno { get; init; } = "";
        public string ApeMaterno { get; init; } = "";
        public string Nombre { get; init; } = "";
        public string NombreCompleto { get; init; } = "";
        public string NumeroDocumento { get; init; } = "";
        public string? Sexo { get; init; }
        public DateTime? FechaNacimiento { get; init; }
        public string? Celular1 { get; init; }
        public string? EmailPersonal { get; init; }
        public string? Direccion { get; init; }
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
