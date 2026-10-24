using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Prendario;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoCicloWriteService(IOptions<SqlDatabaseOptions> options) : ICreditoCicloWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public Task<CreditoCicloOperacionResponse> AprobarAsync(
        int creditoId,
        int opcion,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        ExecuteProcAsync(
            "CREDITO.usp_Credito_Upd",
            new { Opcion = opcion, CreditoId = creditoId, UsuarioId = usuarioId },
            creditoId,
            cancellationToken);

    public Task<CreditoCicloOperacionResponse> AnularAsync(
        int creditoId,
        string observacion,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        ExecuteProcAsync(
            "CREDITO.usp_Credito_Del",
            new
            {
                CreditoId = creditoId,
                Observacion = observacion.ToUpperInvariant(),
                UsuarioId = usuarioId,
            },
            creditoId,
            cancellationToken);

    public Task<CreditoCicloOperacionResponse> ReprogramarAsync(
        int creditoId,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        ExecuteProcAsync(
            "CREDITO.usp_ReprogramarCredito",
            new { CreditoId = creditoId, UsuarioId = usuarioId },
            creditoId,
            cancellationToken);

    public Task<CreditoCicloOperacionResponse> ProrrogarAsync(
        int creditoId,
        int dias,
        CancellationToken cancellationToken = default) =>
        ExecuteProcAsync(
            "CREDITO.usp_ProrrogarCredito",
            new { CreditoId = creditoId, Dias = dias },
            creditoId,
            cancellationToken);

    public async Task<CrearCreditoResponse> CrearDesdeSolicitudAsync(
        int solicitudCreditoId,
        int productoId,
        string tipoCuota,
        decimal montoInicial,
        decimal montoCredito,
        decimal montoGastosAdm,
        string indGastosAdm,
        string formaPago,
        int nroCuotas,
        decimal interes,
        DateTime fechaPrimerPago,
        string observacion,
        int usuarioId,
        bool indCentralRiesgo,
        CrearCreditoPrendaRequest? prenda = null,
        CancellationToken cancellationToken = default)
    {
        if (solicitudCreditoId < 1 || productoId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(solicitudCreditoId), "Parámetros inválidos.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (prenda is not null)
        {
            var errorPrenda = PrendarioValidacion.ValidarPrendaGeneracion(prenda);
            if (errorPrenda is not null)
            {
                throw new ArgumentException(errorPrenda);
            }
        }

        var meta = await connection.QueryFirstOrDefaultAsync<(bool EsPrendario, int? ProductoId, decimal MontoTasacion)>(
            new CommandDefinition(
                """
                SELECT EsPrendario, ProductoId, ISNULL(MontoTasacion, 0) AS MontoTasacion
                FROM CREDITO.Credito
                WHERE CreditoId = @CreditoId;
                """,
                new { CreditoId = solicitudCreditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var esPrendario = meta.EsPrendario
            || meta.ProductoId == PrendarioValidacion.ProductoIdPrendario
            || productoId == PrendarioValidacion.ProductoIdPrendario
            || prenda is not null;

        if (esPrendario)
        {
            var tasacion = prenda?.MontoTasacion
                ?? (meta.MontoTasacion > 0
                    ? meta.MontoTasacion
                    : await connection.ExecuteScalarAsync<decimal?>(
                        new CommandDefinition(
                            """
                            SELECT ISNULL(SUM(ValorTasacion), 0)
                            FROM CREDITO.Prenda
                            WHERE CreditoId = @CreditoId;
                            """,
                            new { CreditoId = solicitudCreditoId },
                            cancellationToken: cancellationToken)).ConfigureAwait(false) ?? 0m);

            var errorGen = PrendarioValidacion.ValidarGeneracionPrendaria(
                formaPago,
                nroCuotas,
                montoCredito,
                tasacion,
                fechaPrimerPago);
            if (errorGen is not null)
            {
                throw new ArgumentException(errorGen);
            }
        }

        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var mensaje = await connection.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(
                    "CREDITO.usp_Credito_Ins",
                    new
                    {
                        SolicitudCreditoId = solicitudCreditoId,
                        ProductoId = productoId,
                        TipoCuota = tipoCuota,
                        MontoInicial = montoInicial,
                        MontoCredito = montoCredito,
                        MontoGastosAdm = montoGastosAdm,
                        IndGastoAdm = indGastosAdm,
                        FormaPago = formaPago,
                        NroCuotas = nroCuotas,
                        Interes = interes,
                        FechaPrimerPago = fechaPrimerPago.Date,
                        Observacion = observacion ?? string.Empty,
                        UsuarioId = usuarioId,
                        IndCentralRiesgo = indCentralRiesgo,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (prenda is not null)
            {
                await GuardarPrendaAsync(
                    connection,
                    transaction,
                    solicitudCreditoId,
                    prenda,
                    usuarioId,
                    cancellationToken).ConfigureAwait(false);
            }

            // Tras usp_Credito_Ins, FechaVencimiento queda = última cuota del plan.
            // Remate prendario = vencimiento + 30 días (paridad PrendaFlex / legacy).
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE CREDITO.Credito
                    SET FechaRemate = DATEADD(DAY, 30, FechaVencimiento)
                    WHERE CreditoId = @CreditoId
                      AND EsPrendario = CAST(1 AS bit);
                    """,
                    new { CreditoId = solicitudCreditoId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CrearCreditoResponse(mensaje ?? string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Alta de bien solo si aún no hay prendas (flujo simulador sin pasar por «Nuevo»).
    /// Si ya se registraron en la solicitud, no se toca el detalle.
    /// </summary>
    private static async Task GuardarPrendaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int creditoId,
        CrearCreditoPrendaRequest prenda,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var existentes = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM CREDITO.Prenda WITH (UPDLOCK, HOLDLOCK) WHERE CreditoId = @CreditoId;",
                new { CreditoId = creditoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (existentes > 0)
        {
            return;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO CREDITO.Prenda (
                    CreditoId, Descripcion, Marca, Modelo, Serie, Color, ValorTasacion,
                    Observaciones, Estado, FechaRegistro, CodigoInterno, UsuarioRegId)
                VALUES (
                    @CreditoId, @Descripcion, @Marca, @Modelo, @Serie, @Color, @ValorTasacion,
                    @Observaciones, @Estado, GETDATE(), @CodigoInterno, @UsuarioId);

                UPDATE CREDITO.Credito
                SET EsPrendario = CAST(1 AS bit),
                    MontoTasacion = @ValorTasacion,
                    NumeroContratoPrendario = ISNULL(
                        NULLIF(LTRIM(RTRIM(NumeroContratoPrendario)), ''),
                        CAST(CreditoId AS nvarchar(50))),
                    FechaRemate = COALESCE(@FechaRemate, DATEADD(DAY, 30, FechaVencimiento))
                WHERE CreditoId = @CreditoId;
                """,
                new
                {
                    CreditoId = creditoId,
                    Descripcion = CreditoGestionWriteService.Mayusculas(prenda.Descripcion)!,
                    Marca = CreditoGestionWriteService.Mayusculas(prenda.Marca),
                    Modelo = CreditoGestionWriteService.Mayusculas(prenda.Modelo),
                    Serie = CreditoGestionWriteService.Mayusculas(prenda.Serie) ?? "N/T",
                    Color = CreditoGestionWriteService.Mayusculas(prenda.Color),
                    ValorTasacion = prenda.MontoTasacion,
                    Observaciones = CreditoGestionWriteService.Mayusculas(prenda.Observacion),
                    Estado = CreditoGestionWriteService.PrendaEstadoEnCustodia,
                    CodigoInterno = CreditoGestionWriteService.Mayusculas(prenda.CodigoInterno),
                    FechaRemate = (DateTime?)prenda.FechaRemate.Date,
                    UsuarioId = usuarioId,
                },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<CreditoCicloOperacionResponse> RechazarAsync(
        int creditoId,
        int? ordenVentaId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
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
            await QuitarDependenciasDeSolicitudAsync(
                    connection,
                    transaction,
                    creditoId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (ordenVentaId is > 0)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "VENTAS.usp_OrdenVenta_Del",
                        new { OrdenVentaId = ordenVentaId.Value, OrdenVentaDetId = 0 },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                var sigue = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "SELECT COUNT(1) FROM CREDITO.Credito WHERE CreditoId = @CreditoId;",
                        new { CreditoId = creditoId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
                if (sigue > 0)
                {
                    throw new InvalidOperationException(
                        "No se pudo rechazar: la orden de venta asociada ya está entregada. Use anular.");
                }
            }
            else
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "CREDITO.usp_SolicitudCredito_Del",
                        new { CreditoId = creditoId },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CreditoCicloOperacionResponse(creditoId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Quita hijos de solicitud PEN que bloquean DELETE Credito
    /// (Aprobacion, prenda, imagen, tarea, cargo, central). No toca caja/CxC.
    /// </summary>
    private static async Task QuitarDependenciasDeSolicitudAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int creditoId,
        CancellationToken cancellationToken)
    {
        var estado = await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(
                """
                SELECT Estado
                FROM CREDITO.Credito WITH (UPDLOCK, HOLDLOCK)
                WHERE CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var financieros = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT
                    (SELECT COUNT(1) FROM CREDITO.MovimientoCaja WHERE CreditoId = @CreditoId)
                  + (SELECT COUNT(1) FROM CREDITO.CuentaxCobrar WHERE CreditoId = @CreditoId)
                  + (SELECT COUNT(1) FROM CREDITO.CreditoMora WHERE CreditoId = @CreditoId)
                  + (SELECT COUNT(1) FROM CREDITO.CreditoCondonacion WHERE CreditoId = @CreditoId);
                """,
                new { CreditoId = creditoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var conflicto = CreditoRechazoReglas.MensajeSiNoRechazable(estado, financieros > 0);
        if (conflicto is not null)
        {
            throw new InvalidOperationException(conflicto);
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL
                    UPDATE CREDITO.PlaInusual SET CreditoId = NULL WHERE CreditoId = @CreditoId;

                DELETE FROM CREDITO.Aprobacion WHERE CreditoId = @CreditoId;
                DELETE FROM CREDITO.CreditoImagen WHERE CreditoId = @CreditoId;
                DELETE FROM CREDITO.CentralRiesgo WHERE CreditoId = @CreditoId;
                DELETE FROM CREDITO.Cargo WHERE CreditoId = @CreditoId;

                DELETE st
                FROM CREDITO.Subtarea st
                INNER JOIN CREDITO.Tarea t ON t.TareaId = st.TareaId
                WHERE t.CreditoId = @CreditoId;

                DELETE FROM CREDITO.Tarea WHERE CreditoId = @CreditoId;
                DELETE FROM CREDITO.Prenda WHERE CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<CreditoCicloOperacionResponse> ExecuteProcAsync(
        string procedureName,
        object parameters,
        int creditoId,
        CancellationToken cancellationToken)
    {
        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
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
            var command = new CommandDefinition(
                procedureName,
                parameters,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CreditoCicloOperacionResponse(creditoId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
