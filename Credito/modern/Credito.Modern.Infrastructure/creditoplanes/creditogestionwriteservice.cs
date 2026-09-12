using System.Data;
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

            var pendiente = await connection.QueryFirstOrDefaultAsync<CondonacionAprobarRow>(
                new CommandDefinition(
                    """
                    SELECT TOP (1)
                           cc.Id,
                           cc.CajaDiarioId,
                           cx.CuentaxCobrarId
                    FROM CREDITO.CreditoCondonacion AS cc
                    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cc.CreditoId
                    INNER JOIN CREDITO.CuentaxCobrar AS cx ON cx.CreditoId = cc.CreditoId
                    WHERE cc.CreditoId = @CreditoId
                      AND c.OficinaId = @OficinaId
                      AND cc.IndAprobado = CAST(0 AS bit)
                    ORDER BY cc.Id;
                    """,
                    new { request.CreditoId, request.OficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (pendiente is not null)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE CREDITO.CreditoCondonacion
                        SET IndAprobado = CAST(1 AS bit)
                        WHERE Id = @Id;
                        """,
                        new { pendiente.Id },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "CREDITO.usp_PagarCuentaxCobrar",
                        new
                        {
                            OrdenVentaId = 0,
                            pendiente.CuentaxCobrarId,
                            pendiente.CajaDiarioId,
                            UsuarioId = usuarioId,
                        },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

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

    public async Task<CreditoGestionOperacionResponse> GuardarPrendasAsync(
        GuardarPrendasRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken cancellationToken = default)
    {
        // El legacy descarta los renglones sin descripcion: son filas vacias del formulario.
        var prendas = (request.Prendas ?? Array.Empty<PrendaItemRequest>())
            .Where(p => !string.IsNullOrWhiteSpace(p.Descripcion))
            .ToList();

        if (prendas.Count == 0)
        {
            return new CreditoGestionOperacionResponse(false, "Registre al menos un bien con descripción.");
        }

        if (prendas.Any(p => p.ValorTasacion <= 0))
        {
            return new CreditoGestionOperacionResponse(false, "Cada bien debe tener un valor de tasación mayor a cero.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var existeCredito = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)
                FROM CREDITO.Credito WITH (UPDLOCK, HOLDLOCK)
                WHERE CreditoId = @CreditoId AND OficinaId = @OficinaId;
                """,
                new { request.CreditoId, request.OficinaId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (existeCredito == 0)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new CreditoGestionOperacionResponse(false, "Crédito no encontrado.");
        }

        // El formulario envia el detalle completo, asi que se reemplaza en bloque.
        await connection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM CREDITO.Prenda WHERE CreditoId = @CreditoId;",
                new { request.CreditoId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO CREDITO.Prenda (
                    CreditoId, Descripcion, Marca, Modelo, Serie, Color, ValorTasacion,
                    Observaciones, Estado, FechaRegistro, CodigoInterno, UsuarioRegId)
                VALUES (
                    @CreditoId, @Descripcion, @Marca, @Modelo, @Serie, @Color, @ValorTasacion,
                    @Observaciones, @Estado, @FechaRegistro, @CodigoInterno, @UsuarioRegId);
                """,
                prendas.Select(p => new
                {
                    request.CreditoId,
                    Descripcion = Mayusculas(p.Descripcion)!,
                    Marca = Mayusculas(p.Marca),
                    Modelo = Mayusculas(p.Modelo),
                    // "N/T" (no tiene) es el centinela que usa el legacy para serie ausente.
                    Serie = Mayusculas(p.Serie) ?? "N/T",
                    Color = Mayusculas(p.Color),
                    p.ValorTasacion,
                    Observaciones = Mayusculas(p.Observaciones),
                    Estado = PrendaEstadoEnCustodia,
                    FechaRegistro = fechaServidor,
                    CodigoInterno = Mayusculas(p.CodigoInterno),
                    UsuarioRegId = usuarioId,
                }).ToList(),
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        // MontoTasacion se deriva de las prendas guardadas, no de lo que llego en el cuerpo:
        // el legacy sumaba tambien los renglones descartados e inflaba el monto garantizado.
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE c
                SET EsPrendario = CAST(1 AS bit),
                    MontoTasacion = ISNULL((
                        SELECT SUM(p.ValorTasacion)
                        FROM CREDITO.Prenda AS p
                        WHERE p.CreditoId = c.CreditoId), 0),
                    NumeroContratoPrendario = ISNULL(
                        NULLIF(LTRIM(RTRIM(c.NumeroContratoPrendario)), ''),
                        CAST(c.CreditoId AS nvarchar(50))),
                    FechaRemate = COALESCE(@FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento))
                FROM CREDITO.Credito AS c
                WHERE c.CreditoId = @CreditoId
                  AND c.OficinaId = @OficinaId;
                """,
                new
                {
                    request.CreditoId,
                    request.OficinaId,
                    FechaRemate = request.FechaRemate?.Date,
                },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new CreditoGestionOperacionResponse(true, null);
    }

    internal const string PrendaEstadoEnCustodia = "EN CUSTODIA";

    internal static string? Mayusculas(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();

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

    private sealed class CondonacionAprobarRow
    {
        public int Id { get; init; }
        public int CajaDiarioId { get; init; }
        public int CuentaxCobrarId { get; init; }
    }
}
