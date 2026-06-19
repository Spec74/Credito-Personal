using System.Data;
using Credito.Modern.Application.CreditoPlanes;
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
            ValidatePrenda(prenda);
            await CreditoPrendaSchema.EnsureAsync(connection, cancellationToken).ConfigureAwait(false);
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
                await UpsertPrendaAsync(
                    connection,
                    transaction,
                    solicitudCreditoId,
                    prenda,
                    usuarioId,
                    cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CrearCreditoResponse(mensaje ?? string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static void ValidatePrenda(CrearCreditoPrendaRequest prenda)
    {
        if (string.IsNullOrWhiteSpace(prenda.Descripcion))
        {
            throw new ArgumentException("La descripción de la prenda es obligatoria.", nameof(prenda));
        }

        if (prenda.MontoTasacion <= 0)
        {
            throw new ArgumentException("El monto de tasación debe ser mayor a cero.", nameof(prenda));
        }
    }

    private static Task UpsertPrendaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int creditoId,
        CrearCreditoPrendaRequest prenda,
        int usuarioId,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            new CommandDefinition(
                """
                IF EXISTS (SELECT 1 FROM CREDITO.CreditoPrenda WHERE CreditoId = @CreditoId)
                BEGIN
                    UPDATE CREDITO.CreditoPrenda
                    SET Descripcion = @Descripcion,
                        MontoTasacion = @MontoTasacion,
                        FechaRemate = @FechaRemate,
                        Observacion = @Observacion,
                        Estado = CAST(1 AS bit),
                        UsuarioModId = @UsuarioId,
                        FechaMod = GETDATE()
                    WHERE CreditoId = @CreditoId;
                END
                ELSE
                BEGIN
                    INSERT INTO CREDITO.CreditoPrenda (
                        CreditoId, Descripcion, MontoTasacion, FechaRemate, Observacion,
                        Estado, UsuarioRegId, FechaReg)
                    VALUES (
                        @CreditoId, @Descripcion, @MontoTasacion, @FechaRemate, @Observacion,
                        CAST(1 AS bit), @UsuarioId, GETDATE());
                END;
                """,
                new
                {
                    CreditoId = creditoId,
                    Descripcion = prenda.Descripcion.Trim().ToUpperInvariant(),
                    prenda.MontoTasacion,
                    FechaRemate = prenda.FechaRemate.Date,
                    Observacion = string.IsNullOrWhiteSpace(prenda.Observacion)
                        ? null
                        : prenda.Observacion.Trim().ToUpperInvariant(),
                    UsuarioId = usuarioId,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

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
            if (ordenVentaId is > 0)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "VENTAS.usp_OrdenVenta_Del",
                        new { OrdenVentaId = ordenVentaId.Value, OrdenVentaDetId = 0 },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
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
