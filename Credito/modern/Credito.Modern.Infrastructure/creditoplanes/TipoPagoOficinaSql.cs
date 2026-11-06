using System.Data;
using Dapper;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

internal static class TipoPagoOficinaSql
{
    internal static async Task<bool> EsCentralPorCajaDiarioAsync(
        IDbConnection connection,
        int cajaDiarioId,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null)
    {
        var flag = await connection.ExecuteScalarAsync<bool?>(
            new CommandDefinition(
                """
                SELECT TOP (1) CAST(o.IndPrincipal AS bit)
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
                WHERE cd.CajaDiarioId = @CajaDiarioId;
                """,
                new { CajaDiarioId = cajaDiarioId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return flag == true;
    }

    internal static async Task<bool> EsCentralPorOficinaAsync(
        IDbConnection connection,
        int oficinaId,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null)
    {
        var flag = await connection.ExecuteScalarAsync<bool?>(
            new CommandDefinition(
                """
                SELECT TOP (1) CAST(IndPrincipal AS bit)
                FROM MAESTRO.Oficina
                WHERE OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return flag == true;
    }
}
