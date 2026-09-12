using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Helper
{
    /// <summary>
    /// Paridad con Credito.Modern.Infrastructure.Auth.UsuarioPasswordHasher (PBKDF2 $pbk2$).
    /// .NET Framework 4.8: no usa Rfc2898DeriveBytes.Pbkdf2 estático ni CryptographicOperations.
    /// </summary>
    public static class UsuarioPasswordHasherCompat
    {
        public const int DefaultIterations = 150000;

        public static bool LooksLikeStoredHash(string stored)
        {
            return stored != null && stored.Length > 12 && stored.StartsWith("$pbk2$", StringComparison.Ordinal);
        }

        public static bool Verify(string storedPassword, string plainPassword)
        {
            if (string.IsNullOrEmpty(storedPassword) || plainPassword == null)
            {
                return false;
            }

            if (LooksLikeStoredHash(storedPassword))
            {
                return VerifyPbkdf2(storedPassword, plainPassword);
            }

            return FixedTimeEqualsUtf8(storedPassword, plainPassword);
        }

        private static bool VerifyPbkdf2(string stored, string plainPassword)
        {
            try
            {
                var parts = stored.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 5 || !string.Equals(parts[0], "pbk2", StringComparison.Ordinal))
                {
                    return false;
                }

                if (!string.Equals(parts[1], "1", StringComparison.Ordinal))
                {
                    return false;
                }

                int iter;
                if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out iter)
                    || iter < 1000 || iter > 10000000)
                {
                    return false;
                }

                var salt = Convert.FromBase64String(parts[3]);
                var expected = Convert.FromBase64String(parts[4]);
                if (expected.Length < 16 || expected.Length > 128 || salt.Length < 8 || salt.Length > 256)
                {
                    return false;
                }

                byte[] actual;
                using (var derive = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(plainPassword), salt, iter, HashAlgorithmName.SHA256))
                {
                    actual = derive.GetBytes(expected.Length);
                }

                return FixedTimeEquals(expected, actual);
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
            return FixedTimeEquals(ba, bb);
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
