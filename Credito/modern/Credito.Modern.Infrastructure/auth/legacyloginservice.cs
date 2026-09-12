using Credito.Modern.Application.Auth;
using Credito.Modern.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Auth;

public sealed class LegacyLoginService(
    IOptions<SqlDatabaseOptions> sqlOptions,
    IOptions<AuthOptions> authOptions,
    ILogger<LegacyLoginService> logger)
    : ILegacyLoginService
{
    private const string SqlAccesoCount = """
        SELECT COUNT(1)
        FROM MAESTRO.Acceso AS a
        WHERE a.DireccionIp = @ClienteAcceso;
        """;

    private const string SqlUsuarioOficina = """
        SELECT uo.UsuarioId,
               uo.OficinaId,
               uo.UsuarioOficinaId,
               u.ClaveUsuario AS ClaveAlmacenada
        FROM MAESTRO.UsuarioOficina AS uo
        INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = uo.UsuarioId
        WHERE u.NombreUsuario = @NombreUsuario
          AND uo.OficinaId = @OficinaId
          AND uo.Estado = CAST(1 AS bit)
          AND u.Estado = CAST(1 AS bit);
        """;

    private const string SqlRehashUsuario = """
        UPDATE MAESTRO.Usuario
        SET ClaveUsuario = @NuevoHash
        WHERE UsuarioId = @UsuarioId
          AND ClaveUsuario = @ClavePlainAntes;
        """;

    private const string SqlRoles = """
        SELECT r.Denominacion
        FROM MAESTRO.UsuarioRol AS ur
        INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
        WHERE ur.UsuarioId = @UsuarioId
          AND ur.OficinaId = @OficinaId
          AND r.Estado = CAST(1 AS bit)
        ORDER BY r.Denominacion;
        """;

    private readonly string _connectionString = sqlOptions.Value.ConnectionString;
    private readonly AuthOptions _auth = authOptions.Value;

    public async Task<LegacyLoginOutcome> TryAuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        var nombreUsuario = (request.NombreUsuario ?? string.Empty).Trim();
        var clave = request.Clave ?? string.Empty;
        var oficinaId = request.OficinaId;
        var clienteAcceso = request.ClienteAcceso?.Trim();

        if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(clave) || oficinaId < 1)
        {
            return new LegacyLoginOutcome(LegacyLoginStatus.CredencialesInvalidas, 0, 0, 0, []);
        }

        if (_auth.RequerirClienteAcceso)
        {
            if (string.IsNullOrEmpty(clienteAcceso))
            {
                return new LegacyLoginOutcome(LegacyLoginStatus.ClienteAccesoDenegado, 0, 0, 0, []);
            }

            await using var connectionAcceso = new SqlConnection(_connectionString);
            await connectionAcceso.OpenAsync(cancellationToken).ConfigureAwait(false);
            var accesoCount = await connectionAcceso.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        SqlAccesoCount,
                        new { ClienteAcceso = clienteAcceso },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            if (accesoCount == 0)
            {
                return new LegacyLoginOutcome(LegacyLoginStatus.ClienteAccesoDenegado, 0, 0, 0, []);
            }
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QuerySingleOrDefaultAsync<UsuarioOficinaRow>(
                new CommandDefinition(
                    SqlUsuarioOficina,
                    new { NombreUsuario = nombreUsuario, OficinaId = oficinaId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (row is null || string.IsNullOrEmpty(row.ClaveAlmacenada))
        {
            return new LegacyLoginOutcome(LegacyLoginStatus.CredencialesInvalidas, 0, 0, 0, []);
        }

        if (!UsuarioPasswordHasher.Verify(row.ClaveAlmacenada, clave, out var esClavePlanaLegada))
        {
            return new LegacyLoginOutcome(LegacyLoginStatus.CredencialesInvalidas, 0, 0, 0, []);
        }

        if (esClavePlanaLegada && _auth.MigracionClavePerezosa)
        {
            var nuevoHash = UsuarioPasswordHasher.CreateHash(clave);
            try
            {
                var filas = await connection.ExecuteAsync(
                        new CommandDefinition(
                            SqlRehashUsuario,
                            new { NuevoHash = nuevoHash, UsuarioId = row.UsuarioId, ClavePlainAntes = row.ClaveAlmacenada },
                            cancellationToken: cancellationToken))
                    .ConfigureAwait(false);
                if (filas == 0)
                {
                    logger.LogDebug(
                        "MigracionClavePerezosa: no se actualizó UsuarioId {UsuarioId} (posible carrera o clave ya cambiada).",
                        row.UsuarioId);
                }
                else
                {
                    logger.LogInformation(
                        "MigracionClavePerezosa: UsuarioId {UsuarioId} migrado a hash PBKDF2 en MAESTRO.Usuario.ClaveUsuario.",
                        row.UsuarioId);
                }
            }
            catch (Exception ex) when (IsClaveUsuarioColumnTruncate(ex))
            {
                // Si ClaveUsuario sigue en nvarchar(50), el hash $pbk2$ (~85 chars) no cabe.
                logger.LogWarning(
                    ex,
                    "MigracionClavePerezosa: no se pudo guardar hash (columna ClaveUsuario demasiado corta). Login continúa con clave legado.");
            }
        }

        var roles = await LoadRolesAsync(connection, row.UsuarioId, row.OficinaId, cancellationToken).ConfigureAwait(false);
        return new LegacyLoginOutcome(
            LegacyLoginStatus.Success,
            row.UsuarioId,
            row.OficinaId,
            row.UsuarioOficinaId,
            roles);
    }

    public async Task<IReadOnlyList<string>> GetUsuarioRolesAsync(int usuarioId, int oficinaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (usuarioId < 1 || oficinaId < 1)
        {
            return [];
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await LoadRolesAsync(connection, usuarioId, oficinaId, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<string>> LoadRolesAsync(
        SqlConnection connection,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync<string>(
                new CommandDefinition(
                    SqlRoles,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows
            .Select(static s => (s ?? string.Empty).Trim())
            .Where(static s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed record UsuarioOficinaRow(int UsuarioId, int OficinaId, int UsuarioOficinaId, string? ClaveAlmacenada);

    private static bool IsClaveUsuarioColumnTruncate(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is 8152 or 2628)
            {
                return true;
            }
        }

        return false;
    }
}
