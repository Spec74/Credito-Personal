using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>
/// Fecha de negocio: hoy en zona del servidor SQL (misma convención que cuotas-pendientes y legado sin offset custom).
/// </summary>
public sealed class FechaOperativaReadService(IOptions<SqlDatabaseOptions> options)
    : IFechaOperativaReadService
{
    private readonly string? _connectionString = options.Value.ConnectionString;

    public async Task<DateTime> ObtenerFechaAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return DateTime.Today;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<DateTime>(
            new CommandDefinition(
                "SELECT CAST(SYSDATETIME() AS date);",
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
