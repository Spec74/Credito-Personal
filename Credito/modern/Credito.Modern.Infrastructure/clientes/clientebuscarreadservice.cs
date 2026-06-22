using Credito.Modern.Application.Clientes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Credito.Modern.Infrastructure.Clientes;

/// <summary>Paridad <c>ClienteBL.BuscarCliente</c> (autocomplete MVC).</summary>
public sealed class ClienteBuscarReadService(IOptions<SqlDatabaseOptions> options)
    : IClienteBuscarReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        var search = ClienteSearchTerms.From(term);
        if (search.FilterTerms.Count == 0)
        {
            return Array.Empty<ClienteBuscarItemDto>();
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var parameters = new DynamicParameters();
        parameters.Add("Exacto", search.ExactTerm);
        var clauses = new List<string>();
        for (var i = 0; i < search.CandidateTerms.Count; i++)
        {
            var name = $"Clave{i}";
            parameters.Add(name, $"%{search.CandidateTerms[i]}%");
            clauses.Add($"""
                (
                    p.NombreCompleto LIKE @{name}
                    OR p.NumeroDocumento LIKE @{name}
                    OR ISNULL(p.Codigo, '') LIKE @{name}
                    OR ISNULL(p.Celular1, '') LIKE @{name}
                )
                """);
        }

        var whereCandidates = string.Join("\nAND ", clauses);
        var rows = await connection.QueryAsync<Row>(
            new CommandDefinition(
                $"""
                SELECT TOP (80)
                    p.PersonaId,
                    p.NumeroDocumento,
                    p.NombreCompleto,
                    p.Codigo,
                    p.Celular1
                FROM MAESTRO.Persona AS p
                INNER JOIN MAESTRO.Cliente AS c ON c.PersonaId = p.PersonaId
                WHERE (
                      {whereCandidates}
                  )
                ORDER BY
                    CASE
                        WHEN p.NumeroDocumento = @Exacto THEN 0
                        WHEN ISNULL(p.Codigo, '') = @Exacto THEN 1
                        WHEN ISNULL(p.Celular1, '') = @Exacto THEN 2
                        WHEN p.NumeroDocumento LIKE CONCAT(@Exacto, '%') THEN 3
                        ELSE 9
                    END,
                    p.NombreCompleto;
                """,
                parameters,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows
            .Where(r => CoincideConTodosLosTerminos(r, search.FilterTerms))
            .OrderBy(r => Rank(r, search))
            .ThenBy(r => r.NombreCompleto)
            .Take(20)
            .Select(r =>
            {
                var codigo = string.IsNullOrWhiteSpace(r.Codigo) ? string.Empty : $" [{r.Codigo}]";
                var label = $"{r.NumeroDocumento} {r.NombreCompleto}{codigo}".Trim();
                return new ClienteBuscarItemDto(r.PersonaId, label);
            })
            .ToList();
    }

    private static bool CoincideConTodosLosTerminos(Row row, IReadOnlyList<string> terminos)
    {
        var hayCoincidencia = false;
        foreach (var term in terminos)
        {
            if (Coincide(row, term))
            {
                hayCoincidencia = true;
                continue;
            }

            return false;
        }

        return hayCoincidencia;
    }

    private static bool Coincide(Row row, string term)
    {
        var normalized = Normalize(term);
        return Contains(row.NumeroDocumento, normalized)
            || Contains(row.NombreCompleto, normalized)
            || Contains(row.Codigo, normalized)
            || Contains(row.Celular1, normalized);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value)
        && Normalize(value).Contains(term, StringComparison.OrdinalIgnoreCase);

    private static int Rank(Row row, ClienteSearchTerms search)
    {
        if (string.Equals(row.NumeroDocumento, search.ExactTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(row.Codigo, search.ExactTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(row.Celular1, search.ExactTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(row.NumeroDocumento)
            && row.NumeroDocumento.StartsWith(search.ExactTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 9;
    }

    private static string Normalize(string value)
    {
        var formD = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record ClienteSearchTerms(
        IReadOnlyList<string> FilterTerms,
        IReadOnlyList<string> CandidateTerms,
        string ExactTerm)
    {
        public static ClienteSearchTerms From(string input)
        {
            var exact = Normalize(input);
            if (exact.Length < 2)
            {
                return new ClienteSearchTerms(Array.Empty<string>(), Array.Empty<string>(), exact);
            }

            var terms = new List<string>();
            void Push(string value)
            {
                var normalized = Normalize(value);
                if (normalized.Length >= 2
                    && !terms.Any(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    terms.Add(normalized);
                }
            }

            foreach (Match match in Regex.Matches(exact, @"\[([^\]]+)\]"))
            {
                Push(match.Groups[1].Value);
            }

            foreach (Match match in Regex.Matches(exact, @"\b\d{6,12}\b"))
            {
                Push(match.Value);
            }

            var withoutBrackets = Regex.Replace(exact, @"\[[^\]]+\]", " ");
            foreach (Match match in Regex.Matches(withoutBrackets, @"[A-Z0-9]+"))
            {
                Push(match.Value);
            }

            var candidateTerms = terms
                .Where(t => t.All(char.IsDigit) && t.Length >= 6)
                .Concat(terms.Where(t => !t.All(char.IsDigit)))
                .Concat(terms.Where(t => t.All(char.IsDigit) && t.Length < 6))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();

            if (candidateTerms.Count == 0)
            {
                candidateTerms.Add(terms[0]);
            }

            return new ClienteSearchTerms(terms, candidateTerms, exact);
        }
    }

    private sealed class Row
    {
        public int PersonaId { get; init; }
        public string NumeroDocumento { get; init; } = string.Empty;
        public string NombreCompleto { get; init; } = string.Empty;
        public string? Codigo { get; init; }
        public string? Celular1 { get; init; }
    }
}
