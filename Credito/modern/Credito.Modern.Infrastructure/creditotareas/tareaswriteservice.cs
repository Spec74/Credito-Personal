using Credito.Modern.Application.CreditoTareas;
using Credito.Modern.Application.Validation;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoTareas;

public sealed class TareasWriteService(IOptions<SqlDatabaseOptions> options) : ITareasWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<GuardarTareaResponse> GuardarAsync(
        GuardarTareaRequest request,
        int usuarioId,
        int oficinaId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        var validacion = TareaValidacion.ValidarGuardar(
            request.CreditoId,
            request.Subtareas?.Select(s => s.Titulo));
        if (validacion is not null)
        {
            throw new ArgumentException(validacion);
        }

        if (!await PuedeEditarAsync(usuarioId, oficinaId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("No tiene permiso para crear o editar tareas.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var creditoValido = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM CREDITO.Credito
                    WHERE CreditoId = @CreditoId
                      AND OficinaId = @OficinaId
                      AND Estado IN ('CRE', 'PEN', 'APR', 'DES')
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { request.CreditoId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (!creditoValido)
        {
            throw new InvalidOperationException(
                "El crédito indicado no es válido para tareas o no pertenece a esta oficina.");
        }

        if (request.TareaId > 0
            && !await UsuarioPuedeAccederTareaAsync(connection, request.TareaId, usuarioId, oficinaId, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("No tiene acceso a esta tarea.");
        }

        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            int tareaId;
            if (request.TareaId < 1)
            {
                tareaId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO CREDITO.Tarea (CreditoId, FechaCreacion, Estado, UsuarioCreadorId)
                        VALUES (@CreditoId, @FechaCreacion, 'PEN', @UsuarioCreadorId);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        new
                        {
                            request.CreditoId,
                            FechaCreacion = fechaOperacion,
                            UsuarioCreadorId = usuarioId,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                tareaId = request.TareaId;
                var updated = await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE CREDITO.Tarea
                        SET CreditoId = @CreditoId
                        WHERE TareaId = @TareaId;
                        """,
                        new { TareaId = tareaId, request.CreditoId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                if (updated == 0)
                {
                    throw new InvalidOperationException("La tarea no existe.");
                }

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "DELETE FROM CREDITO.Subtarea WHERE TareaId = @TareaId;",
                        new { TareaId = tareaId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            var subtareas = request.Subtareas ?? Array.Empty<SubtareaGuardarRequest>();
            foreach (var sub in subtareas)
            {
                if (string.IsNullOrWhiteSpace(sub.Titulo))
                {
                    continue;
                }

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO CREDITO.Subtarea (TareaId, Titulo, Completada, FechaCompletada)
                        VALUES (@TareaId, @Titulo, @Completada, @FechaCompletada);
                        """,
                        new
                        {
                            TareaId = tareaId,
                            Titulo = sub.Titulo.Trim(),
                            Completada = sub.Completada,
                            FechaCompletada = sub.Completada ? fechaOperacion : (DateTime?)null,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            var todasCompletadas = subtareas.Count > 0 && subtareas.All(s => s.Completada);
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Tarea
                    SET Estado = @Estado,
                        FechaCompletada = @FechaCompletada
                    WHERE TareaId = @TareaId;
                    """,
                    new
                    {
                        TareaId = tareaId,
                        Estado = todasCompletadas ? "COM" : "PEN",
                        FechaCompletada = todasCompletadas ? fechaOperacion : (DateTime?)null,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new GuardarTareaResponse(tareaId, "Tarea guardada correctamente");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<TareaOperacionResponse> EliminarAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (tareaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tareaId), "tareaId debe ser >= 1.");
        }

        if (!await PuedeEditarAsync(usuarioId, oficinaId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("No tiene permiso para eliminar tareas.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!await UsuarioPuedeAccederTareaAsync(connection, tareaId, usuarioId, oficinaId, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("No tiene acceso a esta tarea.");
        }

        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CREDITO.Subtarea WHERE TareaId = @TareaId;",
                    new { TareaId = tareaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM CREDITO.Tarea WHERE TareaId = @TareaId;",
                    new { TareaId = tareaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return rows > 0
                ? new TareaOperacionResponse(true, "Tarea eliminada correctamente")
                : new TareaOperacionResponse(false, "Error al eliminar la tarea");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<TareaOperacionResponse> CompletarAsync(
        int tareaId,
        bool completada,
        int usuarioId,
        int oficinaId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (tareaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tareaId), "tareaId debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!await UsuarioPuedeAccederTareaAsync(connection, tareaId, usuarioId, oficinaId, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("No tiene acceso a esta tarea.");
        }

        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Subtarea
                    SET Completada = @Completada,
                        FechaCompletada = @FechaCompletada
                    WHERE TareaId = @TareaId;
                    """,
                    new
                    {
                        TareaId = tareaId,
                        Completada = completada,
                        FechaCompletada = completada ? fechaOperacion : (DateTime?)null,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Tarea
                    SET Estado = @Estado,
                        FechaCompletada = @FechaCompletada
                    WHERE TareaId = @TareaId;
                    """,
                    new
                    {
                        TareaId = tareaId,
                        Estado = completada ? "COM" : "PEN",
                        FechaCompletada = completada ? fechaOperacion : (DateTime?)null,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return rows > 0
                ? new TareaOperacionResponse(
                    true,
                    completada ? "Tarea completada" : "Tarea marcada como pendiente")
                : new TareaOperacionResponse(false, "Error al actualizar la tarea");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<bool> UsuarioPuedeAccederTareaAsync(
        SqlConnection connection,
        int tareaId,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        var elevado = await PuedeEditarAsync(connection, usuarioId, oficinaId, cancellationToken)
            .ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM CREDITO.Tarea AS t
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = t.CreditoId
                    WHERE t.TareaId = @TareaId
                      AND c.OficinaId = @OficinaId
                      AND (@Elevado = 1 OR c.UsuarioRegId = @UsuarioId)
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new
                {
                    TareaId = tareaId,
                    UsuarioId = usuarioId,
                    OficinaId = oficinaId,
                    Elevado = elevado,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> PuedeEditarAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await PuedeEditarAsync(connection, usuarioId, oficinaId, cancellationToken).ConfigureAwait(false);
    }

    private static Task<bool> PuedeEditarAsync(
        SqlConnection connection,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken) =>
        TareasReadService.EsElevadoSedeAsync(connection, usuarioId, oficinaId, cancellationToken);

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
