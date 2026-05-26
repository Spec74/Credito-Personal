using System.Text;
using Dapper;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>
/// Búsqueda multi-palabra para bandejas de crédito (cada término debe coincidir en algún campo).
/// </summary>
internal static class CreditoBandejaBusquedaSql
{
    private const int MaxTokens = 8;

    public static IReadOnlyList<string> ParseTokens(string? buscar)
    {
        if (string.IsNullOrWhiteSpace(buscar))
        {
            return Array.Empty<string>();
        }

        var normalized = buscar.Trim().Replace('[', ' ').Replace(']', ' ');
        var parts = normalized.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tokens = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (part.All(char.IsDigit))
            {
                if (part.Length >= 1)
                {
                    tokens.Add(part);
                }
            }
            else if (part.Length >= 2)
            {
                tokens.Add(part);
            }

            if (tokens.Count >= MaxTokens)
            {
                break;
            }
        }

        return tokens;
    }

    /// <summary>
    /// Devuelve cláusula SQL (sin AND inicial) y parámetros @T0, @T1…
    /// </summary>
    public static string BuildWhereClause(IReadOnlyList<string> tokens, DynamicParameters parameters)
    {
        if (tokens.Count == 0)
        {
            return "1 = 1";
        }

        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var paramName = $"T{i}";
            parameters.Add(paramName, $"%{tokens[i]}%");
            if (i > 0)
            {
                sb.Append(" AND ");
            }

            sb.Append('(');
            sb.Append($"p.NombreCompleto LIKE @{paramName}");
            sb.Append($" OR p.NumeroDocumento LIKE @{paramName}");
            sb.Append($" OR ISNULL(p.Codigo, '') LIKE @{paramName}");
            sb.Append($" OR CAST(c.CreditoId AS varchar(20)) LIKE @{paramName}");
            sb.Append($" OR up.NombreCompleto LIKE @{paramName}");
            sb.Append(')');
        }

        return sb.ToString();
    }
}
