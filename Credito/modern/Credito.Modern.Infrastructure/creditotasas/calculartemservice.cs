using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoTasas;

public sealed class CalcularTemService(IOptions<SqlDatabaseOptions> options) : ICalcularTemService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<decimal?> CalcularTemAsync(decimal tea, string formaPago, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            "CREDITO.usp_CalcularTEM",
            new { TEA = tea, FormaPago = formaPago },
            transaction: null,
            commandTimeout: 30,
            commandType: System.Data.CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        // El legado usa .First().Value sobre el resultado; ExecuteScalar toma la primera columna de la primera fila.
        return await connection.ExecuteScalarAsync<decimal?>(command).ConfigureAwait(false);
    }
}
