using Credito.Modern.Application.Ubigeo;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ubigeo;

public sealed class DepartamentoReadService(IOptions<SqlDatabaseOptions> options) : IDepartamentoReadService
{
    private const string Sql = """
        SELECT d.idDepa AS DepartamentoId,
               d.Denominacion
        FROM MAESTRO.Departamento AS d
        ORDER BY d.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<DepartamentoListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            Sql,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DepartamentoListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
