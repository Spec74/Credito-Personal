using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Credito.Modern.Infrastructure.Maestro;

/// <summary>
/// Integridad Usuario → Persona (multisucursal).
/// Todo movimiento de caja/tesorería exige PersonaId real del operador:
/// misma semántica que el MVC (<c>Usuario.PersonaId</c>), sin NULL ni personas prestadas.
/// </summary>
public static class UsuarioPersonaIntegrity
{
    public const string MensajeOperadorSinPersona =
        "Su usuario no tiene ficha de persona válida en el padrón (MAESTRO.Persona). "
        + "Un administrador debe corregir el vínculo Usuario→Persona en Administración → Usuarios "
        + "antes de operar caja o bóveda.";

    public const string MensajeLoginSinPersona =
        "Su usuario no tiene ficha de persona válida. Contacte a un administrador "
        + "(Administración → Usuarios) para vincular la persona antes de iniciar sesión.";

    /// <summary>
    /// Devuelve <c>PersonaId</c> solo si existe en <c>MAESTRO.Persona</c>.
    /// </summary>
    public static async Task<int?> TryGetPersonaIdUsuarioAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            return null;
        }

        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP (1) u.PersonaId
                FROM MAESTRO.Usuario AS u
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
                WHERE u.UsuarioId = @UsuarioId
                  AND u.PersonaId > 0;
                """,
                new { UsuarioId = usuarioId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Exige persona válida del operador. Error de negocio accionable si falta.
    /// </summary>
    public static async Task<(string? Error, int PersonaId)> RequirePersonaIdUsuarioAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var personaId = await TryGetPersonaIdUsuarioAsync(
                connection,
                transaction,
                usuarioId,
                cancellationToken)
            .ConfigureAwait(false);

        if (personaId is null or < 1)
        {
            return (MensajeOperadorSinPersona, 0);
        }

        return (null, personaId.Value);
    }

    public static void AddPersonaIdParam(DynamicParameters parameters, string name, int personaId) =>
        parameters.Add(name, personaId, DbType.Int32);
}
