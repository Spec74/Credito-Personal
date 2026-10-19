namespace Credito.Modern.Application.Auth;

public enum LegacyLoginStatus
{
    Success,
    ClienteAccesoDenegado,
    CredencialesInvalidas,
    /// <summary>Hay más de un <c>MAESTRO.Usuario</c> activo con el mismo NombreUsuario en esa oficina.</summary>
    NombreUsuarioAmbiguo,
}
