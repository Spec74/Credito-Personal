using Credito.Modern.Application.CreditoTareas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoTareas;

public sealed class TareasReadService(IOptions<SqlDatabaseOptions> options) : ITareasReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<TareaListItemDto>> ListarPorUsuarioAsync(
        int usuarioId,
        int oficinaId,
        string? estado,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1 || oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var esAdmin = await EsAdminOAprrobadorAsync(connection, usuarioId, oficinaId, cancellationToken)
            .ConfigureAwait(false);
        var estadoFiltro = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpperInvariant();

        var rows = await connection.QueryAsync<TareaListItemDto>(
            new CommandDefinition(
                """
                SELECT
                    t.TareaId,
                    t.CreditoId,
                    p.NumeroDocumento AS ClienteDni,
                    p.NombreCompleto AS ClienteNombre,
                    c.MontoCredito,
                    u.NombreUsuario,
                    t.FechaCreacion,
                    t.FechaCompletada,
                    t.Estado,
                    (SELECT COUNT(*) FROM CREDITO.Subtarea AS s WHERE s.TareaId = t.TareaId) AS TotalSubtareas,
                    (SELECT COUNT(*) FROM CREDITO.Subtarea AS s WHERE s.TareaId = t.TareaId AND s.Completada = CAST(1 AS bit)) AS SubtareasCompletadas
                FROM CREDITO.Tarea AS t
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = t.CreditoId
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
                WHERE (@EsAdmin = 1 OR c.UsuarioRegId = @UsuarioId)
                  AND (@Estado IS NULL OR t.Estado = @Estado)
                ORDER BY t.FechaCreacion DESC;
                """,
                new { UsuarioId = usuarioId, EsAdmin = esAdmin, Estado = estadoFiltro },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<TareaDetalleDto?> ObtenerDetalleAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (tareaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tareaId));
        }

        if (!await UsuarioPuedeAccederTareaAsync(tareaId, usuarioId, oficinaId, cancellationToken)
                .ConfigureAwait(false))
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cabecera = await connection.QueryFirstOrDefaultAsync<TareaDetalleRow>(
            new CommandDefinition(
                """
                SELECT
                    t.TareaId,
                    t.CreditoId,
                    p.NumeroDocumento AS ClienteDni,
                    p.NombreCompleto AS ClienteNombre,
                    c.MontoCredito,
                    u.NombreUsuario,
                    t.FechaCreacion,
                    t.FechaCompletada,
                    t.Estado,
                    (SELECT COUNT(*) FROM CREDITO.Subtarea AS s WHERE s.TareaId = t.TareaId) AS TotalSubtareas,
                    (SELECT COUNT(*) FROM CREDITO.Subtarea AS s WHERE s.TareaId = t.TareaId AND s.Completada = CAST(1 AS bit)) AS SubtareasCompletadas
                FROM CREDITO.Tarea AS t
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = t.CreditoId
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
                WHERE t.TareaId = @TareaId;
                """,
                new { TareaId = tareaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (cabecera is null)
        {
            return null;
        }

        var subtareas = await connection.QueryAsync<SubtareaDto>(
            new CommandDefinition(
                """
                SELECT SubtareaId, TareaId, Titulo, Completada, FechaCompletada
                FROM CREDITO.Subtarea
                WHERE TareaId = @TareaId
                ORDER BY SubtareaId;
                """,
                new { TareaId = tareaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new TareaDetalleDto(
            cabecera.TareaId,
            cabecera.CreditoId,
            cabecera.ClienteDni,
            cabecera.ClienteNombre,
            cabecera.MontoCredito,
            cabecera.NombreUsuario,
            cabecera.FechaCreacion,
            cabecera.FechaCompletada,
            cabecera.Estado,
            cabecera.TotalSubtareas,
            cabecera.SubtareasCompletadas,
            subtareas.ToList());
    }

    public async Task<IReadOnlyList<CreditoTareaBuscarDto>> BuscarCreditosAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        var terminos = TareaBusquedaTerminos.ExtraerDesdeEntrada(term);
        if (terminos.Count == 0)
        {
            return Array.Empty<CreditoTareaBuscarDto>();
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var porId = new Dictionary<int, CreditoTareaBuscarDto>();
        foreach (var clave in terminos)
        {
            var rows = await connection.QueryAsync<CreditoTareaBuscarRow>(
                new CommandDefinition(
                    """
                    SELECT TOP (20)
                           c.CreditoId,
                           c.PersonaId,
                           p.NumeroDocumento AS Dni,
                           p.NombreCompleto AS Nombre,
                           c.MontoCredito
                    FROM CREDITO.Credito AS c
                    INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                    WHERE c.Estado IN ('CRE', 'PEN', 'APR', 'DES')
                      AND (
                          p.NumeroDocumento LIKE '%' + @Clave + '%'
                          OR p.NombreCompleto LIKE '%' + @Clave + '%'
                          OR CAST(c.CreditoId AS varchar(20)) = @Clave
                          OR CAST(c.CreditoId AS varchar(20)) LIKE '%' + @Clave + '%'
                      )
                    ORDER BY c.CreditoId DESC;
                    """,
                    new { Clave = clave },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            foreach (var r in rows)
            {
                if (!porId.ContainsKey(r.CreditoId))
                {
                    porId[r.CreditoId] = new CreditoTareaBuscarDto(
                        r.CreditoId,
                        r.PersonaId,
                        r.Dni,
                        r.Nombre,
                        r.MontoCredito,
                        $"{r.Dni} - {r.Nombre} (Crédito #{r.CreditoId} - S/. {r.MontoCredito:N2})");
                }
            }
        }

        return porId.Values
            .OrderByDescending(x => x.CreditoId)
            .Take(15)
            .ToList();
    }

    public async Task<IReadOnlyList<TareaReporteRowDto>> ListarParaReporteAsync(
        int usuarioId,
        int oficinaId,
        string? estado,
        CancellationToken cancellationToken = default)
    {
        var estadoFiltro = NormalizarEstadoReporte(estado);
        var items = await ListarPorUsuarioAsync(usuarioId, oficinaId, estadoFiltro, cancellationToken)
            .ConfigureAwait(false);

        if (items.Count == 0)
        {
            return Array.Empty<TareaReporteRowDto>();
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var ids = items.Select(t => t.TareaId).ToArray();
        var subtareas = await connection.QueryAsync<SubtareaDto>(
            new CommandDefinition(
                """
                SELECT SubtareaId, TareaId, Titulo, Completada, FechaCompletada
                FROM CREDITO.Subtarea
                WHERE TareaId IN @Ids
                ORDER BY TareaId, SubtareaId;
                """,
                new { Ids = ids },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var subtareasPorTarea = subtareas
            .GroupBy(s => s.TareaId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SubtareaDto>)g.ToList());

        return TareaReporteBuilder.ConstruirFilasOrdenadas(items, subtareasPorTarea, estado);
    }

    private static string? NormalizarEstadoReporte(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
        {
            return "PEN";
        }

        var e = estado.Trim().ToUpperInvariant();
        return e is "TODAS" or "TODOS" or "ALL" ? null : e;
    }

    public Task<bool> PuedeEditarTareaAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default) =>
        EsAdminOAprrobadorAsync(usuarioId, oficinaId, cancellationToken);

    public async Task<bool> UsuarioPuedeAccederTareaAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (tareaId < 1)
        {
            return false;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var esAdmin = await EsAdminOAprrobadorAsync(connection, usuarioId, oficinaId, cancellationToken)
            .ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM CREDITO.Tarea AS t
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = t.CreditoId
                    WHERE t.TareaId = @TareaId
                      AND (@EsAdmin = 1 OR c.UsuarioRegId = @UsuarioId)
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { TareaId = tareaId, UsuarioId = usuarioId, EsAdmin = esAdmin },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> EsAdminOAprrobadorAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await EsAdminOAprrobadorAsync(connection, usuarioId, oficinaId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<bool> EsAdminOAprrobadorAsync(
        SqlConnection connection,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken) =>
        await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CAST(CASE WHEN EXISTS (
                    SELECT 1
                    FROM MAESTRO.UsuarioRol AS ur
                    INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
                    WHERE ur.UsuarioId = @UsuarioId
                      AND ur.OficinaId = @OficinaId
                      AND (
                          UPPER(r.Denominacion) LIKE '%ADMINISTRADOR%'
                          OR UPPER(r.Denominacion) LIKE '%APROBADOR%'
                      )
                ) THEN 1 ELSE 0 END AS bit);
                """,
                new { UsuarioId = usuarioId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class CreditoTareaBuscarRow
    {
        public int CreditoId { get; init; }
        public int PersonaId { get; init; }
        public string Dni { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public decimal MontoCredito { get; init; }
    }

    private sealed class TareaDetalleRow
    {
        public int TareaId { get; init; }
        public int CreditoId { get; init; }
        public string ClienteDni { get; init; } = string.Empty;
        public string ClienteNombre { get; init; } = string.Empty;
        public decimal MontoCredito { get; init; }
        public string? NombreUsuario { get; init; }
        public DateTime FechaCreacion { get; init; }
        public DateTime? FechaCompletada { get; init; }
        public string Estado { get; init; } = string.Empty;
        public int TotalSubtareas { get; init; }
        public int SubtareasCompletadas { get; init; }
    }
}
