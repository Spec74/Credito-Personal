namespace Credito.Modern.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Si es <c>true</c>, exige una fila en <c>MAESTRO.Acceso</c> con <c>DireccionIp</c> igual al token de cliente del login (equivalente a <c>tk</c> en el MVC).</summary>
    public bool RequerirClienteAcceso { get; set; } = true;

    /// <summary>
    /// Si es <c>true</c>, tras un login exitoso con clave almacenada en claro (legado), sustituye <c>MAESTRO.Usuario.ClaveUsuario</c>
    /// por un hash <c>$pbk2$...</c> (misma columna). Coordinar con el MVC hasta que también acepte hash.
    /// </summary>
    public bool MigracionClavePerezosa { get; set; }
}
