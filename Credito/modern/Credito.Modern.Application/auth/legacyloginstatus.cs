namespace Credito.Modern.Application.Auth;

public enum LegacyLoginStatus
{
    Success,
    ClienteAccesoDenegado,
    CredencialesInvalidas,
    /// <summary>Hay más de un <c>MAESTRO.Usuario</c> activo con el mismo NombreUsuario en esa oficina.</summary>
    NombreUsuarioAmbiguo,
    /// <summary>
    /// <c>MAESTRO.Usuario.PersonaId</c> no existe en <c>MAESTRO.Persona</c> (dato huérfano).
    /// No se permite sesión: caja/bóveda exigen persona del operador.
    /// </summary>
    PersonaUsuarioInvalida,
}
