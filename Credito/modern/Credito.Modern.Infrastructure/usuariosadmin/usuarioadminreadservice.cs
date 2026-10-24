using Credito.Modern.Application.UsuariosAdmin;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.UsuariosAdmin;

public sealed class UsuarioAdminReadService(IOptions<SqlDatabaseOptions> options) : IUsuarioAdminReadService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<UsuarioAdminScope?> ResolveScopeAsync(int oficinaId, CancellationToken ct = default)
    {
        Ensure();
        if (oficinaId < 1)
        {
            return null;
        }

        const string sql = """
            SELECT o.OficinaId,
                   CAST(o.IndPrincipal AS bit) AS EsCentral,
                   o.Denominacion AS Oficina
            FROM MAESTRO.Oficina AS o
            WHERE o.OficinaId = @OficinaId;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        return await c.QuerySingleOrDefaultAsync<UsuarioAdminScope>(
            new CommandDefinition(sql, new { OficinaId = oficinaId }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task<UsuarioGestionPageDto> ListGestionAsync(
        UsuarioAdminScope scope,
        string? buscar,
        int page,
        int pageSize,
        bool incluirInactivos,
        bool verRed,
        int? sedeId,
        CancellationToken ct = default)
    {
        Ensure();
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 25 : Math.Min(pageSize, 200);
        var skip = (p - 1) * ps;
        var term = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();
        var red = scope.EsCentral && verRed;
        var sedeFiltro = scope.EsCentral && sedeId is >= 1 ? sedeId : red ? null : scope.OficinaId;

        var estadoFilter = incluirInactivos
            ? ""
            : red
                ? "AND u.Estado = CAST(1 AS bit)"
                : "AND u.Estado = CAST(1 AS bit) AND uo.Estado = CAST(1 AS bit)";
        var buscarFilter = term is null ? "" : "AND u.NombreUsuario LIKE '%' + @Buscar + '%'";
        var sedeJoin = red
            ? ""
            : """
              INNER JOIN MAESTRO.UsuarioOficina AS uo
                  ON uo.UsuarioId = u.UsuarioId
                 AND uo.OficinaId = @SedeId
              """;
        var sedeExists = red && sedeFiltro is >= 1
            ? """
              AND EXISTS (
                  SELECT 1
                  FROM MAESTRO.UsuarioOficina AS xf
                  WHERE xf.UsuarioId = u.UsuarioId
                    AND xf.OficinaId = @SedeId
                    AND xf.Estado = CAST(1 AS bit))
              """
            : "";

        var countSql = $"""
            SELECT COUNT(1)
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            {sedeJoin}
            WHERE 1 = 1
              {estadoFilter}
              {buscarFilter}
              {sedeExists};
            """;

        var sql = $"""
            SELECT u.UsuarioId,
                   u.NombreUsuario,
                   p.NombreCompleto,
                   p.Celular1,
                   p.EmailPersonal,
                   u.Estado,
                   CAST(CASE WHEN EXISTS (
                       SELECT 1
                       FROM MAESTRO.UsuarioOficina AS xs
                       WHERE xs.UsuarioId = u.UsuarioId
                         AND xs.OficinaId = @OficinaSesion
                         AND xs.Estado = CAST(1 AS bit)) THEN 1 ELSE 0 END AS bit) AS EnEstaSede,
                   STUFF((
                       SELECT N', ' + o.Denominacion
                       FROM MAESTRO.UsuarioOficina AS xo
                       INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = xo.OficinaId
                       WHERE xo.UsuarioId = u.UsuarioId
                         AND xo.Estado = CAST(1 AS bit)
                         AND o.Estado = CAST(1 AS bit)
                       ORDER BY o.IndPrincipal DESC, o.Denominacion
                       FOR XML PATH(N''), TYPE).value(N'.', N'nvarchar(400)'), 1, 2, N'') AS Sedes
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            {sedeJoin}
            WHERE 1 = 1
              {estadoFilter}
              {buscarFilter}
              {sedeExists}
            ORDER BY u.NombreUsuario
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        var args = new
        {
            Buscar = term,
            Skip = skip,
            Take = ps,
            SedeId = sedeFiltro ?? scope.OficinaId,
            OficinaSesion = scope.OficinaId,
        };

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var total = await c.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, args, cancellationToken: ct)).ConfigureAwait(false);
        var rows = (await c.QueryAsync<UsuarioGestionListItemDto>(
            new CommandDefinition(sql, args, cancellationToken: ct))
            .ConfigureAwait(false)).ToList();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)ps);
        return new UsuarioGestionPageDto(
            p,
            ps,
            total,
            totalPages,
            rows,
            scope.EsCentral,
            scope.OficinaId,
            scope.Oficina,
            red);
    }

    public async Task<IReadOnlyList<UsuarioReporteGestorDto>> ListReporteGestoresAsync(
        UsuarioAdminScope scope,
        CancellationToken ct = default)
    {
        Ensure();
        var sedeFilter = scope.EsCentral
            ? ""
            : """
              AND EXISTS (
                  SELECT 1
                  FROM MAESTRO.UsuarioOficina AS uo
                  WHERE uo.UsuarioId = u.UsuarioId
                    AND uo.OficinaId = @OficinaId
                    AND uo.Estado = CAST(1 AS bit))
              """;
        var sql = $"""
            SELECT u.UsuarioId,
                   u.NombreUsuario,
                   p.NombreCompleto
            FROM MAESTRO.Usuario AS u
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE u.Estado = CAST(1 AS bit)
              {sedeFilter}
            ORDER BY p.NombreCompleto, u.NombreUsuario;
            """;

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rows = await c.QueryAsync<UsuarioReporteGestorDto>(
            new CommandDefinition(sql, new { scope.OficinaId }, cancellationToken: ct))
            .ConfigureAwait(false);
        return rows.ToList();
    }

    public Task<UsuarioPersonaDetalleDto?> GetDetalleAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        GetDetalleAsync(new UsuarioAdminScope(0, EsCentral: true, Oficina: ""), usuarioId, ct);

    public async Task<UsuarioPersonaDetalleDto?> GetDetalleAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        CancellationToken ct = default)
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
                   CAST(CASE WHEN uo.UsuarioOficinaId IS NOT NULL AND uo.Estado = CAST(1 AS bit) THEN 1 ELSE 0 END AS bit) AS Asignado
            FROM MAESTRO.Oficina AS o
            LEFT JOIN MAESTRO.UsuarioOficina AS uo
                ON uo.OficinaId = o.OficinaId
               AND uo.UsuarioId = @UsuarioId
            WHERE o.Estado = CAST(1 AS bit)
            ORDER BY o.IndPrincipal DESC, o.Denominacion;
            """;

        const string sqlEnSede = """
            SELECT COUNT(1)
            FROM MAESTRO.UsuarioOficina AS uo
            WHERE uo.UsuarioId = @UsuarioId
              AND uo.OficinaId = @OficinaId;
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

        if (!scope.EsCentral)
        {
            var enSede = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sqlEnSede,
                    new { UsuarioId = usuarioId, scope.OficinaId },
                    cancellationToken: ct)).ConfigureAwait(false);
            if (enSede < 1)
            {
                return null;
            }
        }

        var oficinasRaw = (await c.QueryAsync<OficinaAsignacionRow>(
            new CommandDefinition(sqlOficinas, new { UsuarioId = usuarioId }, cancellationToken: ct))
            .ConfigureAwait(false)).ToList();

        var oficinas = oficinasRaw
            .Select(o => new OficinaAsignacionDto(
                o.OficinaId,
                o.Denominacion,
                o.Asignado,
                scope.EsCentral || o.OficinaId == scope.OficinaId))
            .ToList();

        var otras = oficinas
            .Where(o => o.Asignado && o.OficinaId != scope.OficinaId)
            .Select(o => o.Denominacion)
            .ToList();
        var enEstaSede = oficinas.Any(o => o.Asignado && o.OficinaId == scope.OficinaId);

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
            oficinas,
            scope.EsCentral,
            scope.EsCentral,
            enEstaSede,
            otras);
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
        UsuarioAdminScope scope,
        int usuarioId,
        int oficinaId,
        CancellationToken ct = default)
    {
        Ensure();
        if (!scope.EsCentral && oficinaId != scope.OficinaId)
        {
            return [];
        }

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

    private sealed class OficinaAsignacionRow
    {
        public int OficinaId { get; init; }
        public string Denominacion { get; init; } = "";
        public bool Asignado { get; init; }
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
