using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoGestionWriteService(
    IOptions<SqlDatabaseOptions> sqlOptions,
    IOptions<CreditoStorageOptions> storageOptions) : ICreditoGestionWriteService
{
    private readonly string _connectionString = sqlOptions.Value.ConnectionString;
    private readonly string _storageRoot = ResolveStorageRoot(storageOptions.Value);

    public async Task<CreditoGestionOperacionResponse> CondonarAsync(
        CondonarCreditoRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var existe = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT cx.CuentaxCobrarId
                    FROM CREDITO.CuentaxCobrar AS cx
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cx.CreditoId
                    WHERE cx.CreditoId = @CreditoId
                      AND c.OficinaId = @OficinaId;
                    """,
                    new { request.CreditoId, request.OficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (existe is null)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO CREDITO.CuentaxCobrar (Operacion, Monto, Estado, CreditoId)
                        SELECT 'CDN', @Monto, 'PEN', @CreditoId
                        WHERE EXISTS (
                            SELECT 1
                            FROM CREDITO.Credito
                            WHERE CreditoId = @CreditoId
                              AND OficinaId = @OficinaId
                        );
                        """,
                        new { Monto = request.MontoCxc, request.CreditoId, request.OficinaId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE cx
                        SET Operacion = 'CDN', Monto = @Monto, Estado = 'PEN'
                        FROM CREDITO.CuentaxCobrar AS cx
                        INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cx.CreditoId
                        WHERE cx.CreditoId = @CreditoId
                          AND c.OficinaId = @OficinaId;
                        """,
                        new { Monto = request.MontoCxc, request.CreditoId, request.OficinaId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            var obs = $"{fechaServidor:yyyy-MM-dd HH:mm:ss} {request.Observacion}".Trim();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Credito
                    SET Observacion = @Obs,
                        FechaMod = @FechaMod,
                        UsuarioModId = @UsuarioModId,
                        IndCondonacion = CAST(1 AS bit),
                        MontoCondonacion = @MontoCondonacion
                    WHERE CreditoId = @CreditoId
                      AND OficinaId = @OficinaId;
                    """,
                    new
                    {
                        Obs = obs,
                        FechaMod = fechaServidor,
                        UsuarioModId = usuarioId,
                        request.MontoCondonacion,
                        request.CreditoId,
                        request.OficinaId,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CreditoGestionOperacionResponse(true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new CreditoGestionOperacionResponse(false, ex.Message);
        }
    }

    public async Task<CreditoGestionOperacionResponse> ObservarAsync(
        ObservarCreditoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET Observacion = @Obs
                WHERE CreditoId = @CreditoId
                  AND OficinaId = @OficinaId;
                """,
                new { Obs = request.Observacion, request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Crédito no encontrado.");
    }

    public async Task<CreditoGestionOperacionResponse> GuardarCargoAsync(
        GuardarCargoCreditoRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var numCuota = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    request.Final
                        ? """
                          SELECT TOP (1) pp.Numero
                          FROM CREDITO.PlanPago AS pp
                          INNER JOIN CREDITO.Credito AS c ON c.CreditoId = pp.CreditoId
                          WHERE pp.CreditoId = @CreditoId
                            AND pp.Estado = 'PEN'
                            AND c.OficinaId = @OficinaId
                          ORDER BY Numero DESC;
                          """
                        : """
                          SELECT TOP (1) pp.Numero
                          FROM CREDITO.PlanPago AS pp
                          INNER JOIN CREDITO.Credito AS c ON c.CreditoId = pp.CreditoId
                          WHERE pp.CreditoId = @CreditoId
                            AND pp.Estado = 'PEN'
                            AND c.OficinaId = @OficinaId
                          ORDER BY Numero ASC;
                          """,
                    new { request.CreditoId, request.OficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (numCuota < 1)
            {
                throw new InvalidOperationException("No hay cuotas pendientes para asignar el cargo.");
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.Cargo (
                        CreditoId, NumCuota, Descripcion, TipoCargoT2, Importe,
                        UsuarioId, Fecha, Estado)
                    SELECT
                        @CreditoId, @NumCuota, @Descripcion, @TipoCargoT2, @Importe,
                        @UsuarioId, @Fecha, 'PEN'
                    WHERE EXISTS (
                        SELECT 1
                        FROM CREDITO.Credito
                        WHERE CreditoId = @CreditoId
                          AND OficinaId = @OficinaId
                    );
                    """,
                    new
                    {
                        request.CreditoId,
                        request.OficinaId,
                        NumCuota = numCuota,
                        request.Descripcion,
                        TipoCargoT2 = request.TipoCargoId,
                        Importe = request.Monto,
                        UsuarioId = usuarioId,
                        Fecha = fechaServidor,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var montocargo = await connection.ExecuteScalarAsync<decimal>(
                new CommandDefinition(
                    """
                    SELECT ISNULL(SUM(ca.Importe), 0)
                    FROM CREDITO.Cargo AS ca
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = ca.CreditoId
                    WHERE ca.CreditoId = @CreditoId
                      AND ca.NumCuota = @NumCuota
                      AND ca.Estado = 'PEN'
                      AND c.OficinaId = @OficinaId;
                    """,
                    new { request.CreditoId, request.OficinaId, NumCuota = numCuota },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE pp
                    SET pp.Cargo = @Montocargo,
                        pp.PagoCuota = pp.Cuota + pp.ImporteMora + @Montocargo - pp.PagoLibre
                    FROM CREDITO.PlanPago AS pp
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = pp.CreditoId
                    WHERE pp.CreditoId = @CreditoId
                      AND pp.Numero = @NumCuota
                      AND c.OficinaId = @OficinaId;
                    """,
                    new { Montocargo = montocargo, request.CreditoId, request.OficinaId, NumCuota = numCuota },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CreditoGestionOperacionResponse(true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new CreditoGestionOperacionResponse(false, ex.Message);
        }
    }

    public async Task<CreditoGestionOperacionResponse> SubirEvidenciaAsync(
        int oficinaId,
        int creditoId,
        string extension,
        Stream contenido,
        CancellationToken cancellationToken = default)
    {
        var archivo = Guid.NewGuid().ToString("N") + extension;
        var dir = Path.Combine(_storageRoot, creditoId.ToString());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, archivo);
        await using (var fs = File.Create(path))
        {
            await contenido.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO CREDITO.CreditoImagen (CreditoId, Imagen)
                SELECT @CreditoId, @Imagen
                WHERE EXISTS (
                    SELECT 1
                    FROM CREDITO.Credito
                    WHERE CreditoId = @CreditoId
                      AND OficinaId = @OficinaId
                );
                """,
                new { CreditoId = creditoId, OficinaId = oficinaId, Imagen = archivo },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (rows > 0)
        {
            return new CreditoGestionOperacionResponse(true, null);
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return new CreditoGestionOperacionResponse(false, "Crédito no encontrado para la oficina.");
    }

    public async Task<CreditoGestionOperacionResponse> EliminarEvidenciaAsync(
        int oficinaId,
        int creditoImagenId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QueryFirstOrDefaultAsync<(int CreditoId, string Imagen)>(
            new CommandDefinition(
                """
                SELECT ci.CreditoId, ci.Imagen
                FROM CREDITO.CreditoImagen AS ci
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = ci.CreditoId
                WHERE ci.Id = @Id
                  AND c.OficinaId = @OficinaId;
                """,
                new { Id = creditoImagenId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row.CreditoId < 1)
        {
            return new CreditoGestionOperacionResponse(false, "Evidencia no encontrada.");
        }

        var path = Path.Combine(_storageRoot, row.CreditoId.ToString(), row.Imagen);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE ci
                FROM CREDITO.CreditoImagen AS ci
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = ci.CreditoId
                WHERE ci.Id = @Id
                  AND c.OficinaId = @OficinaId;
                """,
                new { Id = creditoImagenId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        _ = oficinaId;
        return new CreditoGestionOperacionResponse(true, null);
    }

    public async Task<CreditoGestionOperacionResponse> CambiarAnalistaAsync(
        CambiarAnalistaCreditoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET UsuarioRegId = @AnalistaId
                WHERE CreditoId = @CreditoId
                  AND OficinaId = @OficinaId;
                """,
                new { request.AnalistaId, request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Crédito no encontrado.");
    }

    public async Task<CreditoGestionOperacionResponse> ActualizarTopeAsync(
        ActualizarTopeCreditoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE MAESTRO.Cliente
                SET TopeCredito = @TopeCredito
                WHERE PersonaId = @PersonaId;
                """,
                new { request.TopeCredito, request.PersonaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Cliente no encontrado.");
    }

    public async Task<CreditoGestionOperacionResponse> DepurarPersonaAsync(
        DepurarPersonaCreditoRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO MAESTRO.PersonaDepurado (
                    PersonaId, Descripcion, Estado, UsuarioRegId, FechaReg)
                VALUES (
                    @PersonaId, @Descripcion, CAST(1 AS bit), @UsuarioRegId, @FechaReg);
                """,
                new
                {
                    request.PersonaId,
                    request.Observacion,
                    UsuarioRegId = usuarioId,
                    FechaReg = fechaServidor,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return new CreditoGestionOperacionResponse(true, null);
    }

    public async Task<CreditoGestionOperacionResponse> ActualizarIrrecuperableAsync(
        ActualizarIrrecuperableRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET IndIrrecuperable = @IndIrrecuperable
                WHERE CreditoId = @CreditoId
                  AND OficinaId = @OficinaId;
                """,
                new { request.IndIrrecuperable, request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Crédito no encontrado.");
    }

    public async Task<CreditoGestionOperacionResponse> ModificarTramiteAdmAsync(
        ModificarTramiteAdmCreditoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET MontoGastosAdm = @Valor
                WHERE CreditoId = @CreditoId AND OficinaId = @OficinaId;
                """,
                new { request.Valor, request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Crédito no encontrado.");
    }

    public async Task<CreditoGestionOperacionResponse> ModificarCentralRiesgoAsync(
        ModificarCentralRiesgoCreditoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET CentralRiesgo = @Valor
                WHERE CreditoId = @CreditoId AND OficinaId = @OficinaId;
                """,
                new { request.Valor, request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Crédito no encontrado.");
    }

    public async Task<CreditoGestionOperacionResponse> ActualizarDescuentoPlanPagoAsync(
        ActualizarDescuentoPlanPagoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE pp
                SET pp.Descuento = @Descuento
                FROM CREDITO.PlanPago AS pp
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = pp.CreditoId
                WHERE pp.PlanPagoId = @PlanPagoId
                  AND pp.CreditoId = @CreditoId
                  AND c.OficinaId = @OficinaId;
                """,
                new
                {
                    request.Descuento,
                    request.PlanPagoId,
                    request.CreditoId,
                    request.OficinaId,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Cuota de plan de pagos no encontrada.");
    }

    public async Task<CreditoGestionOperacionResponse> ActualizarAvalAsync(
        ActualizarAvalCreditoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET PersonaAvalId = @PersonaAvalId
                WHERE CreditoId = @CreditoId AND OficinaId = @OficinaId;
                """,
                new { request.PersonaAvalId, request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "Crédito no encontrado.");
    }

    public async Task<CreditoGestionOperacionResponse> GuardarPrendaAsync(
        GuardarCreditoPrendaRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
        {
            return new CreditoGestionOperacionResponse(false, "La descripción de la prenda es obligatoria.");
        }

        if (request.MontoTasacion <= 0)
        {
            return new CreditoGestionOperacionResponse(false, "El monto de tasación debe ser mayor a cero.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await CreditoPrendaSchema.EnsureAsync(connection, cancellationToken).ConfigureAwait(false);

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                IF EXISTS (
                    SELECT 1
                    FROM CREDITO.CreditoPrenda AS cp
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cp.CreditoId
                    WHERE cp.CreditoId = @CreditoId
                      AND c.OficinaId = @OficinaId
                )
                BEGIN
                    UPDATE cp
                    SET Descripcion = @Descripcion,
                        MontoTasacion = @MontoTasacion,
                        FechaRemate = @FechaRemate,
                        Observacion = @Observacion,
                        Estado = CAST(1 AS bit),
                        UsuarioModId = @UsuarioId,
                        FechaMod = @FechaServidor
                    FROM CREDITO.CreditoPrenda AS cp
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cp.CreditoId
                    WHERE cp.CreditoId = @CreditoId
                      AND c.OficinaId = @OficinaId;
                END
                ELSE
                BEGIN
                    INSERT INTO CREDITO.CreditoPrenda (
                        CreditoId, Descripcion, MontoTasacion, FechaRemate, Observacion,
                        Estado, UsuarioRegId, FechaReg)
                    SELECT
                        @CreditoId, @Descripcion, @MontoTasacion, @FechaRemate, @Observacion,
                        CAST(1 AS bit), @UsuarioId, @FechaServidor
                    WHERE EXISTS (
                        SELECT 1
                        FROM CREDITO.Credito
                        WHERE CreditoId = @CreditoId
                          AND OficinaId = @OficinaId
                    );
                END;
                """,
                new
                {
                    request.CreditoId,
                    request.OficinaId,
                    Descripcion = request.Descripcion.Trim().ToUpperInvariant(),
                    request.MontoTasacion,
                    FechaRemate = request.FechaRemate.Date,
                    Observacion = string.IsNullOrWhiteSpace(request.Observacion)
                        ? null
                        : request.Observacion.Trim().ToUpperInvariant(),
                    UsuarioId = usuarioId,
                    FechaServidor = fechaServidor,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows > 0
            ? new CreditoGestionOperacionResponse(true, null)
            : new CreditoGestionOperacionResponse(false, "No se pudo guardar la prenda.");
    }

    public static string ResolveStorageRoot(CreditoStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.RootPath))
        {
            return Path.GetFullPath(options.RootPath);
        }

        return Path.Combine(AppContext.BaseDirectory, "storage");
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
