using System.Data;
using Credito.Modern.Infrastructure.MediosPago;
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

    internal static async Task<int?> OficinaIdPorCajaDiarioAsync(
        IDbConnection connection,
        int cajaDiarioId,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null)
    {
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP (1) c.OficinaId
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.CajaDiarioId = @CajaDiarioId;
                """,
                new { CajaDiarioId = cajaDiarioId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// El TipoPagoId debe estar activo en OficinaMedioPago de la sucursal (CRUD Medios de pago).
    /// Siembra catálogo base si la oficina aún no tiene filas (paridad con ValidateMedioActivoAsync).
    /// </summary>
    internal static async Task<string?> RequireMedioActivoEnOficinaAsync(
        IDbConnection connection,
        int oficinaId,
        int tipoPagoId,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null,
        string fieldName = "tipoPagoId")
    {
        if (oficinaId < 1 || tipoPagoId < 1)
        {
            return $"{fieldName}: oficina/medio inválido.";
        }

        await OficinaMedioPagoSeedSql
            .EnsureSemillaOficinaAsync(connection, transaction, oficinaId, cancellationToken)
            .ConfigureAwait(false);

        var activo = await connection.ExecuteScalarAsync<bool?>(
            new CommandDefinition(
                """
                SELECT TOP (1) CAST(Activo AS bit)
                FROM CREDITO.OficinaMedioPago
                WHERE OficinaId = @OficinaId
                  AND TipoPagoId = @TipoPagoId;
                """,
                new { OficinaId = oficinaId, TipoPagoId = (short)tipoPagoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (activo is null)
        {
            return $"{fieldName}: el medio {tipoPagoId} no está configurado en esta sucursal (Medios de pago).";
        }

        if (activo != true)
        {
            return $"{fieldName}: el medio {tipoPagoId} está inactivo en esta sucursal.";
        }

        return null;
    }
}
