using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Credito.Modern.Api.Auth;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opts = options.Value;

    public string CreateAccessToken(
        int usuarioId,
        int oficinaId,
        TimeSpan lifetime,
        int? usuarioOficinaId = null,
        IReadOnlyList<string>? roles = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(VendixClaims.UsuarioId, usuarioId.ToString(CultureInfo.InvariantCulture)),
            new(VendixClaims.OficinaId, oficinaId.ToString(CultureInfo.InvariantCulture)),
        };
        if (usuarioOficinaId is { } uo)
        {
            claims.Add(new Claim(VendixClaims.UsuarioOficinaId, uo.ToString(CultureInfo.InvariantCulture)));
        }

        if (roles is { Count: > 0 })
        {
            foreach (var r in roles)
            {
                var trimmed = (r ?? string.Empty).Trim();
                if (trimmed.Length > 0)
                {
                    claims.Add(new Claim(ClaimTypes.Role, trimmed));
                }
            }
        }

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken(int usuarioId, int oficinaId, int? usuarioOficinaId)
    {
        var lifetime = TimeSpan.FromDays(_opts.RefreshTokenLifetimeDays);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(VendixClaims.UsuarioId, usuarioId.ToString(CultureInfo.InvariantCulture)),
            new(VendixClaims.OficinaId, oficinaId.ToString(CultureInfo.InvariantCulture)),
        };
        if (usuarioOficinaId is { } uo)
        {
            claims.Add(new Claim(VendixClaims.UsuarioOficinaId, uo.ToString(CultureInfo.InvariantCulture)));
        }

        claims.Add(
            new Claim(
                VendixClaims.RefreshTokenVersion,
                _opts.RefreshTokenVersion.ToString(CultureInfo.InvariantCulture)));

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.RefreshAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
