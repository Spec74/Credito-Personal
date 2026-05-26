using System.Data;
using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class CodigoBarrasLstReadService(IOptions<SqlDatabaseOptions> options) : ICodigoBarrasLstReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<CodigoBarrasLstRowDto>> ListarPorMovimientoAsync(
        int movimientoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (movimientoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoId), "movimientoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "VENTAS.usp_CodigoBarras_Lst",
            new { pMovimientoId = movimientoId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<CodigoBarrasLstRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
