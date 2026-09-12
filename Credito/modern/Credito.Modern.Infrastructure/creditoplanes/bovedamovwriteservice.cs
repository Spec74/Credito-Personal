using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaMovWriteService(IOptions<SqlDatabaseOptions> options) : IBovedaMovWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public Task<BovedaMovOperacionResponse?> IngresoEgresoAsync(
        int oficinaId,
        decimal importe,
        string descripcion,
        int tipoOperacionId,
        short tipoPagoId,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || tipoOperacionId < 1 || usuarioRegId < 1)
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

        return ExecuteIngresoEgresoAsync(
            oficinaId,
            importe,
            descripcion,
            tipoOperacionId,
            tipoPagoId,
            usuarioRegId,
            fechaOperacion,
            cancellationToken);
    }

    public Task<BovedaMovOperacionResponse?> TransferirACajaAsync(
        int oficinaId,
        int cajaId,
        decimal importe,
        string descripcion,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || cajaId < 1 || usuarioRegId < 1)
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

        return ExecuteTransferirACajaAsync(
            oficinaId,
            cajaId,
            importe,
            descripcion,
            usuarioRegId,
            fechaOperacion,
            cancellationToken);
    }

    private async Task<BovedaMovOperacionResponse?> ExecuteIngresoEgresoAsync(
        int oficinaId,
        decimal importe,
        string descripcion,
        int tipoOperacionId,
        short tipoPagoId,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var tipoOp = await connection.QueryFirstOrDefaultAsync<TipoOperacionRow>(
                new CommandDefinition(
                    """
                    SELECT TipoOperacionId, Codigo, IndEntrada, IndBoveda
                    FROM MAESTRO.TipoOperacion
                    WHERE TipoOperacionId = @TipoOperacionId;
                    """,
                    new { TipoOperacionId = tipoOperacionId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (tipoOp is null || !tipoOp.IndBoveda)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }

            var bovedaId = await GetBovedaAbiertaIdAsync(connection, transaction, oficinaId, cancellationToken)
                .ConfigureAwait(false);
            if (bovedaId is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }

            var movimientoBovedaId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.BovedaMov (
                        BovedaId,
                        CodOperacion,
                        TipoPagoId,
                        Glosa,
                        Importe,
                        IndEntrada,
                        Estado,
                        CajaDiarioId,
                        UsuarioRegId,
                        FechaReg)
                    OUTPUT INSERTED.MovimientoBovedaId
                    VALUES (
                        @BovedaId,
                        @CodOperacion,
                        @TipoPagoId,
                        @Glosa,
                        @Importe,
                        @IndEntrada,
                        CAST(1 AS bit),
                        0,
                        @UsuarioRegId,
                        @Fecha);
                    """,
                    new
                    {
                        BovedaId = bovedaId.Value,
                        CodOperacion = tipoOp.Codigo,
                        TipoPagoId = tipoPagoId,
                        Glosa = descripcion.ToUpperInvariant(),
                        Importe = importe,
                        IndEntrada = tipoOp.IndEntrada,
                        UsuarioRegId = usuarioRegId,
                        Fecha = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await ActualizarSaldosBovedaAsync(connection, transaction, bovedaId.Value, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new BovedaMovOperacionResponse(movimientoBovedaId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<BovedaMovOperacionResponse?> ExecuteTransferirACajaAsync(
        int oficinaId,
        int cajaId,
        decimal importe,
        string descripcion,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var bovedaId = await GetBovedaAbiertaIdAsync(connection, transaction, oficinaId, cancellationToken)
                .ConfigureAwait(false);
            if (bovedaId is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }

            var cajaDiario = await connection.QueryFirstOrDefaultAsync<CajaDiarioTransferenciaRow>(
                new CommandDefinition(
                    """
                    SELECT cd.CajaDiarioId, c.OficinaId
                    FROM CREDITO.CajaDiario AS cd
                    INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                    WHERE cd.CajaId = @CajaId
                      AND cd.IndCierre = CAST(0 AS bit);
                    """,
                    new { CajaId = cajaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (cajaDiario is null || cajaDiario.OficinaId != oficinaId)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return null;
            }

            var personaId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    "SELECT PersonaId FROM MAESTRO.Usuario WHERE UsuarioId = @UsuarioId;",
                    new { UsuarioId = usuarioRegId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var glosaBoveda = "TRANS A CAJA: " + descripcion.ToUpperInvariant();
            var movimientoBovedaId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.BovedaMov (
                        BovedaId,
                        CodOperacion,
                        TipoPagoId,
                        Glosa,
                        Importe,
                        IndEntrada,
                        Estado,
                        CajaDiarioId,
                        UsuarioRegId,
                        FechaReg)
                    OUTPUT INSERTED.MovimientoBovedaId
                    VALUES (
                        @BovedaId,
                        N'TRS',
                        1,
                        @Glosa,
                        @Importe,
                        CAST(0 AS bit),
                        CAST(1 AS bit),
                        @CajaDiarioId,
                        @UsuarioRegId,
                        @Fecha);
                    """,
                    new
                    {
                        BovedaId = bovedaId.Value,
                        Glosa = glosaBoveda,
                        Importe = importe,
                        CajaDiarioId = cajaDiario.CajaDiarioId,
                        UsuarioRegId = usuarioRegId,
                        Fecha = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var descripcionCaja =
                $"[MovBoveda:{movimientoBovedaId}] TRANS DE BOVEDA: {descripcion.ToUpperInvariant()}";

            var movimientoCajaId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.MovimientoCaja (
                        CajaDiarioId,
                        Operacion,
                        ImportePago,
                        PersonaId,
                        Descripcion,
                        IndEntrada,
                        Estado,
                        TipoPagoId,
                        UsuarioRegId,
                        FechaReg)
                    VALUES (
                        @CajaDiarioId,
                        'TRE',
                        @ImportePago,
                        @PersonaId,
                        @Descripcion,
                        CAST(1 AS bit),
                        CAST(1 AS bit),
                        1,
                        @UsuarioRegId,
                        @FechaReg);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        CajaDiarioId = cajaDiario.CajaDiarioId,
                        ImportePago = importe,
                        PersonaId = personaId,
                        Descripcion = descripcionCaja,
                        UsuarioRegId = usuarioRegId,
                        FechaReg = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE cd
                    SET
                        Entradas = ISNULL(agg.Entradas, 0),
                        Salidas = ISNULL(agg.Salidas, 0),
                        SaldoFinal = cd.SaldoInicial + ISNULL(agg.Entradas, 0) - ISNULL(agg.Salidas, 0)
                    FROM CREDITO.CajaDiario AS cd
                    OUTER APPLY (
                        SELECT
                            SUM(CASE WHEN mc.IndEntrada = CAST(1 AS bit) THEN mc.ImportePago ELSE 0 END) AS Entradas,
                            SUM(CASE WHEN mc.IndEntrada = CAST(0 AS bit) THEN mc.ImportePago ELSE 0 END) AS Salidas
                        FROM CREDITO.MovimientoCaja AS mc
                        WHERE mc.CajaDiarioId = cd.CajaDiarioId
                          AND mc.Estado = CAST(1 AS bit)
                    ) AS agg
                    WHERE cd.CajaDiarioId = @CajaDiarioId;
                    """,
                    new { CajaDiarioId = cajaDiario.CajaDiarioId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await ActualizarSaldosBovedaAsync(connection, transaction, bovedaId.Value, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new BovedaMovOperacionResponse(movimientoBovedaId, movimientoCajaId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<(string? Error, BovedaMovOperacionResponse? Result)> TransferirACajaChicaAsync(
        int oficinaId,
        decimal importe,
        string descripcion,
        int usuarioRegId,
        DateTime fechaOperacion,
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

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var bovedaId = await GetBovedaAbiertaIdAsync(connection, transaction, oficinaId, cancellationToken)
                .ConfigureAwait(false);
            if (bovedaId is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe bóveda abierta para la oficina.", null);
            }

            var cajaChica = await connection.QueryFirstOrDefaultAsync<CajaChicaAbiertaRow>(
                new CommandDefinition(
                    """
                    SELECT TOP 1 Id
                    FROM CREDITO.CajaChicaDiario
                    WHERE IndCierre = CAST(0 AS bit);
                    """,
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (cajaChica is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("NO EXISTE CAJA CHICA ABIERTA", null);
            }

            var personaId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    "SELECT PersonaId FROM MAESTRO.Usuario WHERE UsuarioId = @UsuarioId;",
                    new { UsuarioId = usuarioRegId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (personaId is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("Usuario no encontrado.", null);
            }

            var glosaBoveda = "TRANS A CAJA CHICA: " + descripcion.ToUpperInvariant();
            var movimientoBovedaId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.BovedaMov (
                        BovedaId,
                        CodOperacion,
                        TipoPagoId,
                        Glosa,
                        Importe,
                        IndEntrada,
                        Estado,
                        CajaDiarioId,
                        UsuarioRegId,
                        FechaReg)
                    OUTPUT INSERTED.MovimientoBovedaId
                    VALUES (
                        @BovedaId,
                        N'TRS',
                        1,
                        @Glosa,
                        @Importe,
                        CAST(0 AS bit),
                        CAST(1 AS bit),
                        @CajaChicaDiarioId,
                        @UsuarioRegId,
                        @Fecha);
                    """,
                    new
                    {
                        BovedaId = bovedaId.Value,
                        Glosa = glosaBoveda,
                        Importe = importe,
                        CajaChicaDiarioId = cajaChica.Id,
                        UsuarioRegId = usuarioRegId,
                        Fecha = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var descripcionChica = ("TRANS DE BOVEDA: " + descripcion.ToUpperInvariant());
            if (descripcionChica.Length > 254)
            {
                descripcionChica = descripcionChica[..254];
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.MovimientoCajaChica (
                        CajaChicaDiarioId,
                        PersonaId,
                        Operacion,
                        Importe,
                        Descripcion,
                        IndEntrada,
                        Estado,
                        UsuarioRegId,
                        FechaReg,
                        IndRendido,
                        ImporteRendido)
                    VALUES (
                        @CajaChicaDiarioId,
                        @PersonaId,
                        'TRE',
                        @Importe,
                        @Descripcion,
                        CAST(1 AS bit),
                        CAST(1 AS bit),
                        @UsuarioRegId,
                        @FechaReg,
                        CAST(0 AS bit),
                        0);
                    """,
                    new
                    {
                        CajaChicaDiarioId = cajaChica.Id,
                        PersonaId = personaId.Value,
                        Importe = importe,
                        Descripcion = descripcionChica,
                        UsuarioRegId = usuarioRegId,
                        FechaReg = fechaOperacion,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE ccd
                    SET
                        Entradas = ISNULL(agg.Entradas, 0),
                        Salidas = ISNULL(agg.Salidas, 0),
                        SaldoFinal = ccd.SaldoInicial + ISNULL(agg.Entradas, 0) - ISNULL(agg.Salidas, 0)
                    FROM CREDITO.CajaChicaDiario AS ccd
                    OUTER APPLY (
                        SELECT
                            SUM(CASE WHEN mcc.IndEntrada = CAST(1 AS bit) THEN mcc.Importe ELSE 0 END) AS Entradas,
                            SUM(CASE WHEN mcc.IndEntrada = CAST(0 AS bit) THEN mcc.Importe ELSE 0 END) AS Salidas
                        FROM CREDITO.MovimientoCajaChica AS mcc
                        WHERE mcc.CajaChicaDiarioId = ccd.Id
                          AND mcc.Estado = CAST(1 AS bit)
                    ) AS agg
                    WHERE ccd.Id = @Id;
                    """,
                    new { Id = cajaChica.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await ActualizarSaldosBovedaAsync(connection, transaction, bovedaId.Value, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (null, new BovedaMovOperacionResponse(movimientoBovedaId));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<TransferirBovedaBancosResponse> TransferirEntreBancosAsync(
        int oficinaId,
        short tipoPagoOrigenId,
        short tipoPagoDestinoId,
        decimal importe,
        string glosa,
        int usuarioRegId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || usuarioRegId < 1 || tipoPagoOrigenId < 1 || tipoPagoDestinoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        if (tipoPagoOrigenId == tipoPagoDestinoId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoPagoDestinoId),
                "El banco de origen y el de destino no pueden ser iguales.");
        }

        if (importe <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(importe), "El importe debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(glosa))
        {
            throw new ArgumentOutOfRangeException(nameof(glosa), "glosa es obligatoria.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var bovedaId = await GetBovedaAbiertaIdAsync(connection, transaction: null, oficinaId, cancellationToken)
            .ConfigureAwait(false);
        if (bovedaId is null)
        {
            throw new InvalidOperationException(
                "Operación rechazada: La Bóveda de la oficina no está abierta o la sesión expiró.");
        }

        SpResultadoRow? row;
        try
        {
            row = await connection.QueryFirstOrDefaultAsync<SpResultadoRow>(
                new CommandDefinition(
                    "CREDITO.usp_RegistrarTransferenciaBancos",
                    new
                    {
                        BovedaId = bovedaId.Value,
                        TipoPagoOrigenId = tipoPagoOrigenId,
                        TipoPagoDestinoId = tipoPagoDestinoId,
                        Importe = importe,
                        Glosa = glosa.Trim(),
                        UsuarioRegId = usuarioRegId,
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }

        if (row is null)
        {
            throw new InvalidOperationException("El procedimiento de transferencia no devolvió resultado.");
        }

        if (row.Resultado != 1)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(row.Mensaje)
                    ? "No se pudo registrar la transferencia entre bancos."
                    : row.Mensaje);
        }

        await ActualizarSaldosBovedaAsync(connection, transaction: null, bovedaId.Value, cancellationToken)
            .ConfigureAwait(false);

        return new TransferirBovedaBancosResponse(
            true,
            string.IsNullOrWhiteSpace(row.Mensaje)
                ? "Transferencia realizada con éxito."
                : row.Mensaje);
    }

    private static async Task<int?> GetBovedaAbiertaIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        int oficinaId,
        CancellationToken cancellationToken)
    {
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP 1 BovedaId
                FROM CREDITO.Boveda
                WHERE OficinaId = @OficinaId
                  AND IndCierre = CAST(0 AS bit)
                ORDER BY IndTemporal, BovedaId;
                """,
                new { OficinaId = oficinaId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task ActualizarSaldosBovedaAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        int bovedaId,
        CancellationToken cancellationToken)
    {
        var dynamicParams = new DynamicParameters(new { BovedaId = bovedaId });
        dynamicParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "CREDITO.usp_ActualizarSaldosBoveda",
                dynamicParams,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class TipoOperacionRow
    {
        public int TipoOperacionId { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public bool IndEntrada { get; init; }
        public bool IndBoveda { get; init; }
    }

    private sealed class CajaDiarioTransferenciaRow
    {
        public int CajaDiarioId { get; init; }
        public int OficinaId { get; init; }
    }

    private sealed class CajaChicaAbiertaRow
    {
        public int Id { get; init; }
    }

    private sealed class SpResultadoRow
    {
        public int Resultado { get; init; }
        public string Mensaje { get; init; } = string.Empty;
    }
}
