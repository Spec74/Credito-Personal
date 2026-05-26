using Credito.Modern.Application.Ubigeo;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ubigeo;

public sealed class DistritoReadService(IOptions<SqlDatabaseOptions> options) : IDistritoReadService
{
    private const string Sql = """
        SELECT d.idDist AS DistritoId,
               d.idProv AS ProvinciaId,
               d.Denominacion
        FROM MAESTRO.Distrito AS d
        WHERE (@ProvinciaId IS NULL OR d.idProv = @ProvinciaId)
        ORDER BY d.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<DistritoListItemDto>> GetAsync(int? provinciaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtro = provinciaId is >= 1 ? provinciaId : null;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(Sql, new { ProvinciaId = filtro }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DistritoListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
