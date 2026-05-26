using Credito.Modern.Application.Clientes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Clientes;

/// <summary>Paridad <c>ClienteBL.BuscarUsuario</c>.</summary>
public sealed class UsuarioBuscarReadService(IOptions<SqlDatabaseOptions> options)
    : IUsuarioBuscarReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return Array.Empty<ClienteBuscarItemDto>();
        }

        EnsureConnection();

        var clave = $"%{term.Trim()}%";
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<Row>(
            new CommandDefinition(
                """
                SELECT TOP (10)
                    p.PersonaId,
                    p.NumeroDocumento,
                    p.NombreCompleto
                FROM MAESTRO.Usuario AS u
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
                WHERE u.Estado = CAST(1 AS bit)
                  AND (
                      p.NombreCompleto LIKE @Clave
                      OR p.NumeroDocumento LIKE @Clave
                  )
                ORDER BY p.NombreCompleto;
                """,
                new { Clave = clave },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows
            .Select(r => new ClienteBuscarItemDto(
                r.PersonaId,
                $"{r.NumeroDocumento} {r.NombreCompleto}".Trim()))
            .ToList();
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class Row
    {
        public int PersonaId { get; init; }
        public string NumeroDocumento { get; init; } = string.Empty;
        public string NombreCompleto { get; init; } = string.Empty;
    }
}
