using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.Modelos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Modelos;

public sealed class ModeloReadService(IOptions<SqlDatabaseOptions> options) : IModeloReadService
{
    private const string SqlActivas = """
        SELECT m.ModeloId,
               m.Denominacion,
               m.MarcaId,
               m.Estado
        FROM MAESTRO.Modelo AS m
        WHERE m.Estado = CAST(1 AS bit)
          AND (@MarcaId IS NULL OR m.MarcaId = @MarcaId)
        ORDER BY m.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ModeloListItemDto>> GetActivosAsync(int? marcaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtroMarca = marcaId is >= 1 ? marcaId : null;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            SqlActivas,
            new { MarcaId = filtroMarca },
            transaction: null,
            commandTimeout: 30,
            commandType: System.Data.CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<ModeloListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<ModeloAdminListItemDto>> ListAsync(
        int? marcaId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtroMarca = marcaId is >= 1 ? marcaId : null;
        string sql;
        if (incluirInactivos)
        {
            sql = """
                SELECT m.ModeloId,
                       m.Denominacion,
                       m.MarcaId,
                       ma.Denominacion AS MarcaDenominacion,
                       m.Estado
                FROM MAESTRO.Modelo AS m
                LEFT JOIN MAESTRO.Marca AS ma ON ma.MarcaId = m.MarcaId
                WHERE (@MarcaId IS NULL OR m.MarcaId = @MarcaId)
                ORDER BY m.Denominacion;
                """;
        }
        else
        {
            sql = """
                SELECT m.ModeloId,
                       m.Denominacion,
                       m.MarcaId,
                       ma.Denominacion AS MarcaDenominacion,
                       m.Estado
                FROM MAESTRO.Modelo AS m
                LEFT JOIN MAESTRO.Marca AS ma ON ma.MarcaId = m.MarcaId
                WHERE m.Estado = CAST(1 AS bit)
                  AND (@MarcaId IS NULL OR m.MarcaId = @MarcaId)
                ORDER BY m.Denominacion;
                """;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            sql,
            new { MarcaId = filtroMarca },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ModeloAdminListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
