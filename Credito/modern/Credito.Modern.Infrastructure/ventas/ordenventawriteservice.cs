using System.Data;
using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class OrdenVentaWriteService(IOptions<SqlDatabaseOptions> options) : IOrdenVentaWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<OrdenVentaOperacionMensajeResponse> AgregarDetalleAsync(
        int ordenVentaId,
        string numeroSerie,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaId < 1 || usuarioId < 1 || string.IsNullOrWhiteSpace(numeroSerie))
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaId), "Parámetros inválidos.");
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
            var mensaje = await connection.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(
                    "VENTAS.usp_OrdenVentaDet_Ins",
                    new
                    {
                        OrdenVentaId = ordenVentaId,
                        NumeroSerie = numeroSerie,
                        UsuarioId = usuarioId,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new OrdenVentaOperacionMensajeResponse(mensaje ?? string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<OrdenVentaOperacionResultResponse> ActualizarDetalleAsync(
        int ordenVentaDetId,
        decimal descuento,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaDetId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaDetId), "ordenVentaDetId debe ser >= 1.");
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
            var result = await connection.ExecuteAsync(
                new CommandDefinition(
                    "VENTAS.usp_OrdenVentaDet_update",
                    new { OrdenVentaDetId = ordenVentaDetId, Descuento = descuento },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new OrdenVentaOperacionResultResponse(result);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<OrdenVentaOperacionResultResponse> EliminarDetalleAsync(
        int ordenVentaDetId,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaDetId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaDetId), "ordenVentaDetId debe ser >= 1.");
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
            var result = await connection.ExecuteAsync(
                new CommandDefinition(
                    "VENTAS.usp_OrdenVenta_Del",
                    new { OrdenVentaId = 0, OrdenVentaDetId = ordenVentaDetId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new OrdenVentaOperacionResultResponse(result);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<OrdenVentaOperacionResultResponse> EliminarOrdenAsync(
        int ordenVentaId,
        CancellationToken cancellationToken = default)
    {
        if (ordenVentaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaId), "ordenVentaId debe ser >= 1.");
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
            var result = await connection.ExecuteAsync(
                new CommandDefinition(
                    "VENTAS.usp_OrdenVenta_Del",
                    new { OrdenVentaId = ordenVentaId, OrdenVentaDetId = 0 },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new OrdenVentaOperacionResultResponse(result);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<CrearOrdenVentaResponse> CrearAsync(
        int oficinaId,
        int personaId,
        int usuarioId,
        DateTime fechaReg,
        string tipoVenta,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || personaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        if (string.IsNullOrWhiteSpace(tipoVenta))
        {
            tipoVenta = "CON";
        }

        tipoVenta = tipoVenta.Trim().ToUpperInvariant();
        if (tipoVenta is not ("CON" or "CRE"))
        {
            throw new ArgumentOutOfRangeException(nameof(tipoVenta), "tipoVenta debe ser CON o CRE.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var ordenVentaId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                INSERT INTO VENTAS.OrdenVenta (
                    OficinaId, Subtotal, TotalNeto, TotalImpuesto, TotalDescuento,
                    Estado, UsuarioRegId, FechaReg, PersonaId, TipoVenta)
                VALUES (
                    @OficinaId, 0, 0, 0, 0,
                    'PEN', @UsuarioRegId, @FechaReg, @PersonaId, @TipoVenta);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """,
                new
                {
                    OficinaId = oficinaId,
                    UsuarioRegId = usuarioId,
                    FechaReg = fechaReg,
                    PersonaId = personaId,
                    TipoVenta = tipoVenta,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new CrearOrdenVentaResponse(ordenVentaId);
    }
}
