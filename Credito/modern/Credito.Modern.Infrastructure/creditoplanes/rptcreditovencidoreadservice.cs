using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditoVencidoReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditoVencidoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditoVencidoRowDto>> ListarAsync(
        int oficinaId,
        string? vencidoMenor60,
        string? vencidoMayor60,
        string? vencidoIrrecuperable,
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

        var p1 = NormalizeFlag(nameof(vencidoMenor60), vencidoMenor60);
        var p2 = NormalizeFlag(nameof(vencidoMayor60), vencidoMayor60);
        var p3 = NormalizeFlag(nameof(vencidoIrrecuperable), vencidoIrrecuperable);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptCreditoVencido",
            new
            {
                VencidoMenor60 = p1,
                VencidoMayor60 = p2,
                VencidoIrrecuperable = p3,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<RptCreditoVencidoRowDto>(command).ConfigureAwait(false)).ToList();
        if (rows.Count == 0)
        {
            return rows;
        }

        var allowedIds = await connection.QueryAsync<int>(
            new CommandDefinition(
                """
                SELECT c.CreditoId
                FROM CREDITO.Credito AS c
                WHERE c.OficinaId = @OficinaId
                  AND c.CreditoId IN @CreditoIds;
                """,
                new { OficinaId = oficinaId, CreditoIds = rows.Select(r => r.CreditoId).Distinct().ToArray() },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        var allowed = allowedIds.ToHashSet();
        return rows.Where(r => allowed.Contains(r.CreditoId)).ToList();
    }

    private static string? NormalizeFlag(string paramName, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var t = raw.Trim();
        if (string.Equals(t, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (t.Length != 1)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Debe ser un solo carácter S o N, omitirse o enviarse como null.");
        }

        var c = char.ToUpperInvariant(t[0]);
        if (c is not ('S' or 'N'))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Solo se permiten S o N (indicadores char del procedimiento almacenado).");
        }

        return c.ToString();
    }
}
