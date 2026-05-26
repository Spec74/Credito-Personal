using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.Oficinas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Oficinas;

public sealed class OficinaReadService(IOptions<SqlDatabaseOptions> options) : IOficinaReadService
{
    private const string SqlActivas = """
        SELECT o.OficinaId,
               o.Denominacion,
               o.IndPrincipal,
               o.Estado
        FROM MAESTRO.Oficina AS o
        WHERE o.Estado = CAST(1 AS bit)
        ORDER BY o.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<OficinaListItemDto>> GetActivasAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            SqlActivas,
            parameters: null,
            transaction: null,
            commandTimeout: 30,
            commandType: System.Data.CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<OficinaListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<OficinaAdminListItemDto>> ListGestionAsync(
        bool incluirInactivos,
        string? buscar = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        var term = string.IsNullOrWhiteSpace(buscar) ? null : $"%{buscar.Trim()}%";
        var sql = """
            SELECT o.OficinaId,
                   o.Denominacion,
                   o.Descripcion,
                   o.Telefono,
                   o.UsuarioAsignadoId,
                   o.IndPrincipal,
                   o.Estado,
                   o.Latitud,
                   o.Longitud
            FROM MAESTRO.Oficina AS o
            WHERE (@IncluirInactivos = 1 OR o.Estado = CAST(1 AS bit))
              AND (@Buscar IS NULL OR o.Denominacion LIKE @Buscar OR o.Descripcion LIKE @Buscar)
            ORDER BY o.Denominacion;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<OficinaAdminListItemDto>(
            new CommandDefinition(
                sql,
                new { IncluirInactivos = incluirInactivos ? 1 : 0, Buscar = term },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }
}
