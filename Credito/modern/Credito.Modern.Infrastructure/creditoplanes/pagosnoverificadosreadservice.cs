using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class PagosNoVerificadosReadService(IOptions<SqlDatabaseOptions> options)
    : IPagosNoVerificadosReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<PagosNoVerificadosRowDto>> ListarAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_PagosNoVerificados",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<PagosNoVerificadosRowDto>(command).ConfigureAwait(false)).ToList();
        if (rows.Count == 0)
        {
            return rows;
        }

        var allowedIds = await connection.QueryAsync<int>(
            new CommandDefinition(
                """
                SELECT mc.MovimientoCajaId
                FROM CREDITO.MovimientoCaja AS mc
                INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE c.OficinaId = @OficinaId
                  AND mc.MovimientoCajaId IN @MovimientoCajaIds;
                """,
                new
                {
                    OficinaId = oficinaId,
                    MovimientoCajaIds = rows.Select(r => r.MovimientoCajaId).Distinct().ToArray(),
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        var allowed = allowedIds.ToHashSet();
        return rows.Where(r => allowed.Contains(r.MovimientoCajaId)).ToList();
    }
}
