using System.Data;
using Dapper;

namespace Credito.Modern.Infrastructure.UsuariosAdmin;

/// <summary>
/// Idempotente: asegura <c>MAESTRO.UsuarioOficina</c> cuando ya hay rol, caja o
/// encargado de oficina, para que Seguridad → Usuarios no quede vacío.
/// </summary>
internal static class UsuarioOficinaVinculoSql
{
    /// <param name="oficinaId">
    /// Si es ≥ 1, solo esa sede; si es null, todas las sedes con evidencia.
    /// </param>
    internal static async Task EnsureVinculosAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int? oficinaId,
        CancellationToken cancellationToken)
    {
        await connection
            .ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
                    SELECT DISTINCT ur.UsuarioId, ur.OficinaId, CAST(1 AS bit)
                    FROM MAESTRO.UsuarioRol AS ur
                    INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
                    WHERE ur.OficinaId >= 1
                      AND (@OficinaId IS NULL OR ur.OficinaId = @OficinaId)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM MAESTRO.UsuarioOficina AS uo
                          WHERE uo.UsuarioId = ur.UsuarioId
                            AND uo.OficinaId = ur.OficinaId);

                    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
                    SELECT DISTINCT c.CajeroId, c.OficinaId, CAST(1 AS bit)
                    FROM CREDITO.Caja AS c
                    INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.CajeroId
                    WHERE c.CajeroId >= 1
                      AND c.OficinaId >= 1
                      AND (@OficinaId IS NULL OR c.OficinaId = @OficinaId)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM MAESTRO.UsuarioOficina AS uo
                          WHERE uo.UsuarioId = c.CajeroId
                            AND uo.OficinaId = c.OficinaId);

                    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
                    SELECT DISTINCT o.UsuarioAsignadoId, o.OficinaId, CAST(1 AS bit)
                    FROM MAESTRO.Oficina AS o
                    INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = o.UsuarioAsignadoId
                    WHERE o.UsuarioAsignadoId IS NOT NULL
                      AND o.UsuarioAsignadoId >= 1
                      AND o.OficinaId >= 1
                      AND (@OficinaId IS NULL OR o.OficinaId = @OficinaId)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM MAESTRO.UsuarioOficina AS uo
                          WHERE uo.UsuarioId = o.UsuarioAsignadoId
                            AND uo.OficinaId = o.OficinaId);

                    UPDATE uo
                    SET uo.Estado = CAST(1 AS bit)
                    FROM MAESTRO.UsuarioOficina AS uo
                    WHERE uo.Estado = CAST(0 AS bit)
                      AND (@OficinaId IS NULL OR uo.OficinaId = @OficinaId)
                      AND (
                            EXISTS (
                                SELECT 1
                                FROM MAESTRO.UsuarioRol AS ur
                                WHERE ur.UsuarioId = uo.UsuarioId
                                  AND ur.OficinaId = uo.OficinaId)
                            OR EXISTS (
                                SELECT 1
                                FROM CREDITO.Caja AS c
                                WHERE c.CajeroId = uo.UsuarioId
                                  AND c.OficinaId = uo.OficinaId)
                            OR EXISTS (
                                SELECT 1
                                FROM MAESTRO.Oficina AS o
                                WHERE o.UsuarioAsignadoId = uo.UsuarioId
                                  AND o.OficinaId = uo.OficinaId)
                          );
                    """,
                    new { OficinaId = oficinaId is >= 1 ? oficinaId : null },
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>Alta puntual de un usuario en una sede (p. ej. al asignar cajero).</summary>
    internal static async Task AsegurarUsuarioSedeAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        if (usuarioId < 1 || oficinaId < 1)
        {
            return;
        }

        var n = await connection
            .ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE MAESTRO.UsuarioOficina
                    SET Estado = CAST(1 AS bit)
                    WHERE UsuarioId = @UsuarioId AND OficinaId = @OficinaId;
                    """,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (n > 0)
        {
            return;
        }

        await connection
            .ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
                    VALUES (@UsuarioId, @OficinaId, CAST(1 AS bit));
                    """,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
}
