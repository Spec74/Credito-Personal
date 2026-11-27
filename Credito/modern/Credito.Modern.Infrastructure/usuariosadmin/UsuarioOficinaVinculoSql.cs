using System.Data;
using Dapper;

namespace Credito.Modern.Infrastructure.UsuariosAdmin;

/// <summary>
/// Fuente de verdad de membresía sede: <c>MAESTRO.UsuarioOficina</c>.
/// Idempotente: recrea filas faltantes a partir de roles, cajero de caja o
/// <c>Oficina.UsuarioAsignadoId</c> (mismo rol que <see cref="MediosPago.OficinaMedioPagoSeedSql"/>).
/// </summary>
internal static class UsuarioOficinaVinculoSql
{
    /// <summary>
    /// Completa vínculos ausentes. <paramref name="oficinaId"/> ≥ 1 limita a una sede;
    /// <c>null</c> procesa todas.
    /// </summary>
    internal static Task EnsureFromEvidenceAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int? oficinaId,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            new CommandDefinition(
                SqlEnsureFromEvidence,
                new { OficinaId = oficinaId is >= 1 ? oficinaId : null },
                transaction: transaction,
                cancellationToken: cancellationToken));

    /// <summary>Inserta o actualiza el vínculo usuario–sede (estado activo/inactivo).</summary>
    internal static async Task UpsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int usuarioId,
        int oficinaId,
        bool activo,
        CancellationToken cancellationToken)
    {
        if (usuarioId < 1 || oficinaId < 1)
        {
            return;
        }

        var updated = await connection
            .ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE MAESTRO.UsuarioOficina
                    SET Estado = @Estado
                    WHERE UsuarioId = @UsuarioId AND OficinaId = @OficinaId;
                    """,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId, Estado = activo },
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (updated > 0)
        {
            return;
        }

        await connection
            .ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
                    VALUES (@UsuarioId, @OficinaId, @Estado);
                    """,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId, Estado = activo },
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    private const string SqlEnsureFromEvidence = """
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
        """;
}
