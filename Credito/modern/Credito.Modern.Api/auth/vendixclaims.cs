namespace Credito.Modern.Api.Auth;

public static class VendixClaims
{
    public const string UsuarioId = "vendix:usuario_id";

    public const string OficinaId = "vendix:oficina_id";

    public const string UsuarioOficinaId = "vendix:usuario_oficina_id";

    /// <summary>Versión global de refresh; debe coincidir con <see cref="JwtOptions.RefreshTokenVersion"/>.</summary>
    public const string RefreshTokenVersion = "vendix:refresh_ver";
}
