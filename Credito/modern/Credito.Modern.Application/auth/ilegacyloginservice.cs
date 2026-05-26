namespace Credito.Modern.Application.Auth;

public interface ILegacyLoginService
{
    /// <summary>Replica la lógica de <c>HomeController.Autenticar</c> contra SQL (MAESTRO).</summary>
    Task<LegacyLoginOutcome> TryAuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Roles activos del usuario en la oficina (<c>MAESTRO.UsuarioRol</c> + <c>MAESTRO.Rol</c>).</summary>
    Task<IReadOnlyList<string>> GetUsuarioRolesAsync(int usuarioId, int oficinaId, CancellationToken cancellationToken = default);
}
