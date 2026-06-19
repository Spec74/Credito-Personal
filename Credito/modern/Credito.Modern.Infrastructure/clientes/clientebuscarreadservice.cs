using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.CreditoTareas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

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
        var terminos = TareaBusquedaTerminos.ExtraerDesdeEntrada(term);
        if (terminos.Count == 0)
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

        var porId = new Dictionary<int, Row>();
        foreach (var t in terminos)
        {
            var clave = $"%{t}%";
            var rows = await connection.QueryAsync<Row>(
                new CommandDefinition(
                    """
                    SELECT TOP (20)
                        p.PersonaId,
                        p.NumeroDocumento,
                        p.NombreCompleto,
                        p.Codigo
                    FROM MAESTRO.Persona AS p
                    INNER JOIN MAESTRO.Cliente AS c ON c.PersonaId = p.PersonaId
                    WHERE c.Estado = CAST(1 AS bit)
                      AND (
                          p.NombreCompleto LIKE @Clave
                          OR p.NumeroDocumento LIKE @Clave
                          OR ISNULL(p.Codigo, '') LIKE @Clave
                          OR ISNULL(p.Celular1, '') LIKE @Clave
                      )
                    ORDER BY p.NombreCompleto;
                    """,
                    new { Clave = clave },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            foreach (var r in rows)
            {
                porId.TryAdd(r.PersonaId, r);
            }
        }

        return porId.Values
            .OrderBy(r => r.NombreCompleto)
            .Take(20)
            .Select(r =>
            {
                var codigo = string.IsNullOrWhiteSpace(r.Codigo) ? string.Empty : $" [{r.Codigo}]";
                var label = $"{r.NumeroDocumento} {r.NombreCompleto}{codigo}".Trim();
                return new ClienteBuscarItemDto(r.PersonaId, label);
            })
            .ToList();
    }

    private sealed class Row
    {
        public int PersonaId { get; init; }
        public string NumeroDocumento { get; init; } = string.Empty;
        public string NombreCompleto { get; init; } = string.Empty;
        public string? Codigo { get; init; }
    }
}
