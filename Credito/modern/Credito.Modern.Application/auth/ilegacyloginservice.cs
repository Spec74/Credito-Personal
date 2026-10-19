namespace Credito.Modern.Application.Auth;

public interface ILegacyLoginService
{
    /// <summary>Replica la lógica de <c>HomeController.Autenticar</c> contra SQL (MAESTRO).</summary>
    Task<LegacyLoginOutcome> TryAuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Roles activos del usuario en la oficina (<c>MAESTRO.UsuarioRol</c> + <c>MAESTRO.Rol</c>).</summary>
    Task<IReadOnlyList<string>> GetUsuarioRolesAsync(int usuarioId, int oficinaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perfil vivo para <c>/auth/me</c>: nombre de usuario, denominación de oficina y roles
    /// actuales en BD (evita mostrar login tipado / roles JWT obsoletos tras cambiar NombreUsuario o roles).
    /// </summary>
    Task<AuthSessionProfile?> GetSessionProfileAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}

/// <summary>Datos de sesión leídos de MAESTRO para alinear UI con BD.</summary>
public sealed record AuthSessionProfile(
    string NombreUsuario,
    string? OficinaDenominacion,
    IReadOnlyList<string> Roles);
