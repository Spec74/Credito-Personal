using System.Data;
using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class MovimientoAlmacenWriteService(IOptions<SqlDatabaseOptions> options) : IMovimientoAlmacenWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public Task<MovimientoOperacionMensajeResponse> ConfirmarAsync(
        int movimientoId,
        CancellationToken cancellationToken = default) =>
        MovimientoUpdAsync(3, movimientoId, null, null, null, cancellationToken);

    public Task<MovimientoOperacionMensajeResponse> DesconfirmarAsync(
        int movimientoId,
        CancellationToken cancellationToken = default) =>
        MovimientoUpdAsync(1, movimientoId, null, null, null, cancellationToken);

    public async Task ActualizarAsync(
        int movimientoId,
        int tipoMovimientoId,
        DateTime fecha,
        string observacion,
        CancellationToken cancellationToken = default)
    {
        await MovimientoUpdAsync(
            2,
            movimientoId,
            tipoMovimientoId,
            fecha,
            observacion ?? string.Empty,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task CrearDetalleAsync(
        int movimientoId,
        int movimientoDetId,
        int articuloId,
        bool indAutogenerar,
        string listaSerie,
        int cantidad,
        bool indCorrelativo,
        decimal precioUnitario,
        decimal descuento,
        int medida,
        CancellationToken cancellationToken = default)
    {
        if (movimientoId < 1 || articuloId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoId), "Parámetros inválidos.");
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
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "ALMACEN.usp_CrearMovimientoDet",
                    new
                    {
                        MovimientoId = movimientoId,
                        MovimientoDetId = movimientoDetId,
                        ArticuloId = articuloId,
                        IndAutogenerar = indAutogenerar,
                        ListaSerie = listaSerie ?? string.Empty,
                        Cantidad = cantidad,
                        IndCorrelativo = indCorrelativo,
                        PrecioUnitario = precioUnitario,
                        Descuento = descuento,
                        Medida = medida,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task EliminarDetalleAsync(int movimientoDetId, CancellationToken cancellationToken = default)
    {
        if (movimientoDetId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoDetId), "movimientoDetId debe ser >= 1.");
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
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "ALMACEN.usp_EliminarMovimientoDet",
                    new { MovimientoDetId = movimientoDetId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<CrearMovimientoResponse> CrearMovimientoAsync(
        int almacenId,
        int tipoMovimientoId,
        DateTime fecha,
        CancellationToken cancellationToken = default)
    {
        if (almacenId < 1 || tipoMovimientoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(almacenId), "Parámetros inválidos.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var movimientoId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                INSERT INTO ALMACEN.Movimiento (
                    TipoMovimientoId, AlmacenId, Fecha, SubTotal, IGV, AjusteRedondeo,
                    TotalImporte, EstadoId, Observacion, Documento)
                VALUES (
                    @TipoMovimientoId, @AlmacenId, @Fecha, 0, 0, 0,
                    0, 1, '', '');
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """,
                new
                {
                    TipoMovimientoId = tipoMovimientoId,
                    AlmacenId = almacenId,
                    Fecha = fecha,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new CrearMovimientoResponse(movimientoId);
    }

    public async Task AgregarDocumentoAsync(
        int movimientoId,
        int tipoDocumentoId,
        string serieDocumento,
        string nroDocumento,
        CancellationToken cancellationToken = default)
    {
        if (movimientoId < 1 || tipoDocumentoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoId), "Parámetros inválidos.");
        }

        EnsureConnectionConfigured();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var estadoId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    "SELECT EstadoId FROM ALMACEN.Movimiento WHERE MovimientoId = @MovimientoId;",
                    new { MovimientoId = movimientoId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (estadoId != 1)
            {
                throw new InvalidOperationException(
                    "Solo se pueden agregar documentos a movimientos en estado pendiente (EstadoId=1).");
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO ALMACEN.MovimientoDoc (
                        MovimientoId, TipoDocumentoId, SerieDocumento, NroDocumento)
                    VALUES (@MovimientoId, @TipoDocumentoId, @SerieDocumento, @NroDocumento);
                    """,
                    new
                    {
                        MovimientoId = movimientoId,
                        TipoDocumentoId = tipoDocumentoId,
                        SerieDocumento = serieDocumento?.Trim() ?? string.Empty,
                        NroDocumento = nroDocumento?.Trim() ?? string.Empty,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await RebuildDocumentoTextoAsync(connection, transaction, movimientoId, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task EliminarDocumentoAsync(
        int movimientoDocId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoDocId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoDocId), "movimientoDocId debe ser >= 1.");
        }

        EnsureConnectionConfigured();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var movimientoId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT d.MovimientoId
                    FROM ALMACEN.MovimientoDoc AS d
                    INNER JOIN ALMACEN.Movimiento AS m ON m.MovimientoId = d.MovimientoId
                    WHERE d.MovimientoDocId = @MovimientoDocId AND m.EstadoId = 1;
                    """,
                    new { MovimientoDocId = movimientoDocId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (movimientoId is null)
            {
                throw new InvalidOperationException(
                    "No se puede eliminar el documento (no existe o el movimiento no está pendiente).");
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM ALMACEN.MovimientoDoc WHERE MovimientoDocId = @MovimientoDocId;",
                    new { MovimientoDocId = movimientoDocId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await RebuildDocumentoTextoAsync(connection, transaction, movimientoId.Value, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task ActualizarImporteAsync(
        int movimientoId,
        decimal ajusteRedondeo,
        CancellationToken cancellationToken = default)
    {
        if (movimientoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoId), "movimientoId debe ser >= 1.");
        }

        EnsureConnectionConfigured();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Movimiento
                SET AjusteRedondeo = @AjusteRedondeo,
                    TotalImporte = SubTotal + IGV + @AjusteRedondeo
                WHERE MovimientoId = @MovimientoId AND EstadoId = 1;
                """,
                new { MovimientoId = movimientoId, AjusteRedondeo = ajusteRedondeo },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (rows == 0)
        {
            throw new InvalidOperationException(
                "No se pudo actualizar el importe (movimiento inexistente o no pendiente).");
        }
    }

    private static async Task RebuildDocumentoTextoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int movimientoId,
        CancellationToken cancellationToken)
    {
        var texto = await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(
                """
                SELECT STRING_AGG(
                    ISNULL(NULLIF(t.Descripcion, ''), t.Denominacion) + ' ' + d.SerieDocumento + '-' + d.NroDocumento + '  ',
                    '')
                WITHIN GROUP (ORDER BY d.TipoDocumentoId)
                FROM ALMACEN.MovimientoDoc AS d
                INNER JOIN MAESTRO.TipoDocumento AS t ON t.TipoDocumentoId = d.TipoDocumentoId
                WHERE d.MovimientoId = @MovimientoId;
                """,
                new { MovimientoId = movimientoId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE ALMACEN.Movimiento SET Documento = @Documento WHERE MovimientoId = @MovimientoId;",
                new { MovimientoId = movimientoId, Documento = texto ?? string.Empty },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private void EnsureConnectionConfigured()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private async Task<MovimientoOperacionMensajeResponse> MovimientoUpdAsync(
        int flag,
        int movimientoId,
        int? tipoMovimientoId,
        DateTime? fechaMov,
        string? observacion,
        CancellationToken cancellationToken)
    {
        if (movimientoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoId), "movimientoId debe ser >= 1.");
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
                    "ALMACEN.usp_Movimiento_Upd",
                    new
                    {
                        Flag = flag,
                        MovimientoId = movimientoId,
                        TipoMovimientoId = tipoMovimientoId,
                        FechaMov = fechaMov,
                        Observacion = observacion,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new MovimientoOperacionMensajeResponse(mensaje ?? string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
