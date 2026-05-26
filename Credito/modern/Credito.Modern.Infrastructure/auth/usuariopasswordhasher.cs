using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Credito.Modern.Infrastructure.Auth;

/// <summary>
/// Hash PBKDF2-HMAC-SHA256 en la misma columna <c>MAESTRO.Usuario.ClaveUsuario</c> con prefijo <c>$pbk2$</c> (Fase B del roadmap de contraseñas).
/// Debe mantenerse alineado con <c>Web/Helper/UsuarioPasswordHasherCompat.cs</c> del MVC (.NET Framework 4.8).
/// </summary>
public static class UsuarioPasswordHasher
{
    private const string Scheme = "pbk2";

    /// <summary>Iteraciones por defecto al crear hash nuevos (compatible con verificación que lee el valor del token).</summary>
    public const int DefaultIterations = 150_000;

    /// <summary>Detecta formato moderno (no legado en claro).</summary>
    public static bool LooksLikeStoredHash(string? stored) =>
        stored is { Length: > 12 } && stored.StartsWith("$pbk2$", StringComparison.Ordinal);

    /// <summary>Genera un valor listo para <c>UPDATE Usuario.ClaveUsuario</c>.</summary>
    public static string CreateHash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            32);
        return string.Concat(
            "$pbk2$1$",
            DefaultIterations.ToString(CultureInfo.InvariantCulture),
            "$",
            Convert.ToBase64String(salt),
            "$",
            Convert.ToBase64String(subkey));
    }

    /// <summary>
    /// Verifica contraseña: hash <c>$pbk2$...</c> o igualdad en claro con la cadena almacenada (legado).
    /// <paramref name="matchedLegacyPlainText"/> es <c>true</c> solo si coincidió comparación en claro.
    /// </summary>
    public static bool Verify(string? storedPassword, string plainPassword, out bool matchedLegacyPlainText)
    {
        matchedLegacyPlainText = false;
        if (string.IsNullOrEmpty(storedPassword) || plainPassword is null)
        {
            return false;
        }

        if (LooksLikeStoredHash(storedPassword))
        {
            return VerifyPbkdf2(storedPassword, plainPassword);
        }

        matchedLegacyPlainText = FixedTimeEqualsUtf8(storedPassword, plainPassword);
        return matchedLegacyPlainText;
    }

    private static bool VerifyPbkdf2(string stored, string plainPassword)
    {
        try
        {
            var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || !string.Equals(parts[0], Scheme, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(parts[1], "1", StringComparison.Ordinal))
            {
                return false;
            }

            var iter = int.Parse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (iter is < 1_000 or > 10_000_000)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            if (expected.Length is < 16 or > 128 || salt.Length is < 8 or > 256)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plainPassword),
                salt,
                iter,
                HashAlgorithmName.SHA256,
                expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool FixedTimeEqualsUtf8(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
