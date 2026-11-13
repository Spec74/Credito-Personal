using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaTemporalWriteService(IOptions<SqlDatabaseOptions> options) : IBovedaTemporalWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<(string? Error, AsignarBovedaTemporalResponse? Result)> AsignarOTransferirAsync(
        int oficinaId,
        decimal importe,
        string descripcion,
        int usuarioAsignadoId,
        int usuarioRegId,
        DateTime fechaOperacion,
        short tipoPagoId = 1,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        if (importe <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(importe), "importe debe ser > 0.");
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentOutOfRangeException(nameof(descripcion), "descripcion es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var existeTemporal = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM CREDITO.Boveda
                        WHERE OficinaId = @OficinaId AND IndCierre = CAST(0 AS bit) AND IndTemporal = CAST(1 AS bit)
                    ) THEN 1 ELSE 0 END AS bit);
                    """,
                    new { OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            int? usuarioEncargadoResuelto = null;
            if (usuarioAsignadoId > 0)
            {
                if (existeTemporal)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return ("Ya Existe una boveda Temporal Creada", null);
                }

                usuarioEncargadoResuelto = await ResolverUsuarioIdAsync(
                        connection,
                        transaction,
                        usuarioAsignadoId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (usuarioEncargadoResuelto is null)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return (
                        "El encargado seleccionado no es un usuario activo. Busque de nuevo y elija un usuario válido.",
                        null);
                }
            }
            else
            {
                if (!existeTemporal)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return ("NO Existe una boveda Temporal Creada", null);
                }
            }

            var bovedaPrincipalId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP 1 BovedaId
                    FROM CREDITO.Boveda
                    WHERE OficinaId = @OficinaId
                      AND IndCierre = CAST(0 AS bit)
                      AND IndTemporal = CAST(0 AS bit)
                    ORDER BY BovedaId;
                    """,
                    new { OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (bovedaPrincipalId is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe bóveda principal abierta.", null);
            }

            int bovedaTemporalId;
            if (usuarioEncargadoResuelto is > 0)
            {
                bovedaTemporalId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO CREDITO.Boveda (
                            OficinaId,
                            SaldoInicial,
                            Entradas,
                            Salidas,
                            SaldoFinal,
                            FechaIniOperacion,
                            IndCierre,
                            IndTemporal)
                        OUTPUT INSERTED.BovedaId
                        VALUES (
                            @OficinaId,
                            0,
                            0,
                            0,
                            0,
                            @Fecha,
                            CAST(0 AS bit),
                            CAST(1 AS bit));
                        """,
                        new { OficinaId = oficinaId, Fecha = fechaOperacion },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                await EnsureEncargadoRolAsync(
                    connection,
                    transaction,
                    oficinaId,
                    usuarioEncargadoResuelto.Value,
                    cancellationToken).ConfigureAwait(false);

                descripcion = "ASIGNACION TEMPORAL: " + descripcion;
            }
            else
            {
                bovedaTemporalId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        SELECT TOP 1 BovedaId
                        FROM CREDITO.Boveda
                        WHERE OficinaId = @OficinaId
                          AND IndCierre = CAST(0 AS bit)
                          AND IndTemporal = CAST(1 AS bit);
                        """,
                        new { OficinaId = oficinaId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                descripcion = "TRANS A BOVEDA TEMPORAL: " + descripcion;
            }

            if (tipoPagoId < 1)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("TipoPagoId es obligatorio; no se asume efectivo.", null);
            }

            await EjecutarTransferirBovedaAsync(
                connection,
                transaction,
                bovedaPrincipalId.Value,
                bovedaTemporalId,
                descripcion,
                importe,
                usuarioRegId,
                tipoPagoId,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, new AsignarBovedaTemporalResponse(bovedaTemporalId));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Resuelve UsuarioId operativo. Acepta UsuarioId directo; si no existe, intenta PersonaId→Usuario
    /// (compatibilidad con clientes que enviaban personaId por error).
    /// </summary>
    private static async Task<int?> ResolverUsuarioIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int id,
        CancellationToken cancellationToken)
    {
        var porUsuario = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP 1 UsuarioId
                FROM MAESTRO.Usuario
                WHERE UsuarioId = @Id AND Estado = CAST(1 AS bit);
                """,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (porUsuario is > 0)
        {
            return porUsuario;
        }

        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP 1 UsuarioId
                FROM MAESTRO.Usuario
                WHERE PersonaId = @Id AND Estado = CAST(1 AS bit)
                ORDER BY UsuarioId;
                """,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task EnsureEncargadoRolAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        // Exacto "ENCARGADO" (paridad CajaDiarioBL.AsignarUsuarioCaja). No LIKE %ENCARGADO%.
        var rolId = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP 1 RolId
                FROM MAESTRO.Rol
                WHERE Denominacion = N'ENCARGADO'
                  AND Estado = CAST(1 AS bit);
                """,
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (rolId is null)
        {
            rolId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.Rol (Denominacion, Estado)
                    OUTPUT INSERTED.RolId
                    VALUES (N'ENCARGADO', CAST(1 AS bit));
                    """,
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM MAESTRO.UsuarioRol
                    WHERE RolId = @RolId AND UsuarioId = @UsuarioId AND OficinaId = @OficinaId)
                INSERT INTO MAESTRO.UsuarioRol (OficinaId, RolId, UsuarioId)
                VALUES (@OficinaId, @RolId, @UsuarioId);
                """,
                new { RolId = rolId.Value, UsuarioId = usuarioId, OficinaId = oficinaId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var menuId = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP 1 MenuId
                FROM MAESTRO.Menu
                WHERE Url LIKE N'%Saldos%';
                """,
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (menuId is not null)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    IF NOT EXISTS (
                        SELECT 1 FROM MAESTRO.RolMenu
                        WHERE RolId = @RolId AND MenuId = @MenuId)
                    INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
                    VALUES (@RolId, @MenuId);
                    """,
                    new { RolId = rolId.Value, MenuId = menuId.Value },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    private static async Task EjecutarTransferirBovedaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bovedaInicioId,
        int bovedaDestinoId,
        string glosa,
        decimal monto,
        int usuarioRegId,
        short tipoPagoId,
        CancellationToken cancellationToken)
    {
        var dynamicParams = new DynamicParameters(new
        {
            BovedaInicioId = bovedaInicioId,
            BovedaDestinoId = bovedaDestinoId,
            Glosa = glosa,
            Monto = monto,
            UsuarioRegId = usuarioRegId,
            flagAceptar = 0,
            BovedaMovTempId = 0,
            TipoPagoId = tipoPagoId,
        });
        dynamicParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "CREDITO.usp_TransferirBoveda",
                dynamicParams,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
