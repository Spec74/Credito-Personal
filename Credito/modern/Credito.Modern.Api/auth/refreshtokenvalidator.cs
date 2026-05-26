using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Credito.Modern.Api.Auth;

public sealed class RefreshTokenValidator(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opts = options.Value;

    public bool TryValidate(string refreshToken, out int usuarioId, out int oficinaId, out int? usuarioOficinaId)
    {
        usuarioId = 0;
        oficinaId = 0;
        usuarioOficinaId = null;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        try
        {
            var principal = handler.ValidateToken(
                refreshToken,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _opts.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _opts.RefreshAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                },
                out _);

            var u = principal.FindFirst(VendixClaims.UsuarioId)?.Value;
            var o = principal.FindFirst(VendixClaims.OficinaId)?.Value;
            if (!int.TryParse(u, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui)
                || !int.TryParse(o, NumberStyles.Integer, CultureInfo.InvariantCulture, out var oi)
                || ui < 1 || oi < 1)
            {
                return false;
            }

            usuarioId = ui;
            oficinaId = oi;
            var uo = principal.FindFirst(VendixClaims.UsuarioOficinaId)?.Value;
            if (!string.IsNullOrEmpty(uo)
                && int.TryParse(uo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uoi)
                && uoi >= 1)
            {
                usuarioOficinaId = uoi;
            }

            var verClaim = principal.FindFirst(VendixClaims.RefreshTokenVersion)?.Value;
            if (!int.TryParse(verClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ver)
                || ver != _opts.RefreshTokenVersion)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
