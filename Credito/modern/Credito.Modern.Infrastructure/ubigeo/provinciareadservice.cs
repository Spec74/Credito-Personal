using Credito.Modern.Application.Ubigeo;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ubigeo;

public sealed class ProvinciaReadService(IOptions<SqlDatabaseOptions> options) : IProvinciaReadService
{
    private const string Sql = """
        SELECT p.idProv AS ProvinciaId,
               p.idDepa AS DepartamentoId,
               p.Denominacion
        FROM MAESTRO.Provincia AS p
        WHERE (@DepartamentoId IS NULL OR p.idDepa = @DepartamentoId)
        ORDER BY p.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ProvinciaListItemDto>> GetAsync(int? departamentoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        int? filtro = departamentoId is >= 1 ? departamentoId : null;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(Sql, new { DepartamentoId = filtro }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ProvinciaListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
