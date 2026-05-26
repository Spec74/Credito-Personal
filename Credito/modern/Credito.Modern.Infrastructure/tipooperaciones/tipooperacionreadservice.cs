using Credito.Modern.Application.TipoOperaciones;
using Credito.Modern.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.TipoOperaciones;

public sealed class TipoOperacionReadService(IOptions<SqlDatabaseOptions> options) : ITipoOperacionReadService
{
    private const string SqlAll = """
        SELECT t.TipoOperacionId,
               LTRIM(RTRIM(t.Codigo)) AS Codigo,
               t.Denominacion,
               t.IndEntrada,
               t.IndCajaDiario,
               t.IndBoveda,
               t.IndCajaChica
        FROM MAESTRO.TipoOperacion AS t
        ORDER BY t.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<TipoOperacionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            SqlAll,
            parameters: null,
            transaction: null,
            commandTimeout: 30,
            commandType: System.Data.CommandType.Text,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TipoOperacionListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
