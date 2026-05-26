using Credito.Modern.Application.Clientes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Clientes;

/// <summary>Paridad <c>ClienteBL.BuscarDistrito</c> y <c>BuscarPersona</c>.</summary>
public sealed class ClienteAuxReadService(IOptions<SqlDatabaseOptions> options)
    : IClienteAuxReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarDistritosAsync(
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

        var rows = await connection.QueryAsync<(int Id, string Label)>(
            new CommandDefinition(
                """
                SELECT TOP (15)
                    d.idDist AS Id,
                    d.Denominacion + ' - ' + pr.Denominacion AS Label
                FROM MAESTRO.Distrito AS d
                INNER JOIN MAESTRO.Provincia AS pr ON pr.idProv = d.idProv
                WHERE pr.idDepa = 5
                  AND (d.Denominacion LIKE @Clave OR pr.Denominacion LIKE @Clave)
                ORDER BY d.Denominacion;
                """,
                new { Clave = clave },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(r => new ClienteBuscarItemDto(r.Id, r.Label)).ToList();
    }

    public async Task<IReadOnlyList<ClienteBuscarItemDto>> BuscarPersonasAsync(
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

        var rows = await connection.QueryAsync<(int Id, string Label)>(
            new CommandDefinition(
                """
                SELECT TOP (10)
                    p.PersonaId AS Id,
                    p.NumeroDocumento + ' ' + p.NombreCompleto AS Label
                FROM MAESTRO.Persona AS p
                WHERE p.Estado = CAST(1 AS bit)
                  AND (p.NombreCompleto LIKE @Clave OR p.NumeroDocumento LIKE @Clave)
                ORDER BY p.NombreCompleto;
                """,
                new { Clave = clave },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(r => new ClienteBuscarItemDto(r.Id, r.Label)).ToList();
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString.");
        }
    }
}
