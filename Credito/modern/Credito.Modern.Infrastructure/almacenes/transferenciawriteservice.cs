using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class TransferenciaWriteService(IOptions<SqlDatabaseOptions> options) : ITransferenciaWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CrearTransferenciaResponse> CrearAsync(
        int oficinaId,
        int usuarioId,
        int almacenDestinoId,
        DateTime fecha,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || usuarioId < 1 || almacenDestinoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var origenId = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP (1) AlmacenId
                FROM ALMACEN.Almacen
                WHERE OficinaId = @OficinaId AND Estado = CAST(1 AS bit)
                ORDER BY AlmacenId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (origenId is null)
        {
            throw new InvalidOperationException("No hay almacén activo para la oficina de origen.");
        }

        var destino = await connection.QueryFirstOrDefaultAsync<(int AlmacenId, int OficinaId)>(
            new CommandDefinition(
                """
                SELECT AlmacenId, OficinaId
                FROM ALMACEN.Almacen
                WHERE AlmacenId = @AlmacenDestinoId AND Estado = CAST(1 AS bit);
                """,
                new { AlmacenDestinoId = almacenDestinoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (destino.AlmacenId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(almacenDestinoId), "Almacén destino no válido.");
        }

        if (destino.OficinaId == oficinaId)
        {
            throw new InvalidOperationException("El almacén destino debe pertenecer a otra oficina.");
        }

        var transferenciaId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                INSERT INTO ALMACEN.Transferencia (
                    AlmacenOrigenId, AlmacenDestinoId, UsuarioId, Fecha, Estado)
                VALUES (
                    @AlmacenOrigenId, @AlmacenDestinoId, @UsuarioId, @Fecha, 'P');
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """,
                new
                {
                    AlmacenOrigenId = origenId.Value,
                    AlmacenDestinoId = almacenDestinoId,
                    UsuarioId = usuarioId,
                    Fecha = fecha,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new CrearTransferenciaResponse(transferenciaId);
    }

    public async Task<ValidarSerieTransferenciaResponse> ValidarYAgregarSerieAsync(
        int oficinaId,
        int transferenciaId,
        string numeroSerie,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroSerie))
        {
            return new ValidarSerieTransferenciaResponse(true, "Indique el número de serie.", null, null, null, null);
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var transferencia = await ObtenerTransferenciaEditableAsync(
                connection,
                transferenciaId,
                oficinaId,
                cancellationToken)
            .ConfigureAwait(false);

        if (transferencia is null)
        {
            return new ValidarSerieTransferenciaResponse(
                true,
                "Transferencia no encontrada o no editable.",
                null,
                null,
                null,
                null);
        }

        var serie = await connection.QueryFirstOrDefaultAsync<SerieRow>(
            new CommandDefinition(
                """
                SELECT s.SerieArticuloId,
                       s.NumeroSerie,
                       s.ArticuloId,
                       s.EstadoId,
                       a.Denominacion
                FROM ALMACEN.SerieArticulo AS s
                INNER JOIN MAESTRO.Articulo AS a ON a.ArticuloId = s.ArticuloId
                WHERE s.NumeroSerie = @NumeroSerie;
                """,
                new { NumeroSerie = numeroSerie.Trim() },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (serie is null || serie.EstadoId != 2)
        {
            return new ValidarSerieTransferenciaResponse(
                true,
                "La serie no esta disponible, ingrese otro.",
                null,
                null,
                null,
                null);
        }

        var duplicada = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM ALMACEN.TransferenciaSerie
                    WHERE TransferenciaId = @TransferenciaId AND SerieArticuloId = @SerieArticuloId
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { TransferenciaId = transferenciaId, serie.SerieArticuloId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (duplicada)
        {
            return new ValidarSerieTransferenciaResponse(
                true,
                "La serie ya esta registrada, ingrese otro",
                null,
                null,
                null,
                null);
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO ALMACEN.TransferenciaSerie (TransferenciaId, SerieArticuloId)
                VALUES (@TransferenciaId, @SerieArticuloId);
                """,
                new { TransferenciaId = transferenciaId, serie.SerieArticuloId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new ValidarSerieTransferenciaResponse(
            false,
            null,
            serie.SerieArticuloId,
            serie.NumeroSerie,
            serie.ArticuloId,
            serie.Denominacion);
    }

    public async Task<bool> EliminarSeriesPorArticuloAsync(
        int oficinaId,
        int transferenciaId,
        int articuloId,
        CancellationToken cancellationToken = default)
    {
        if (transferenciaId < 1 || articuloId < 1)
        {
            return false;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (await ObtenerTransferenciaEditableAsync(connection, transferenciaId, oficinaId, cancellationToken)
                .ConfigureAwait(false) is null)
        {
            return false;
        }

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE ts
                FROM ALMACEN.TransferenciaSerie AS ts
                INNER JOIN ALMACEN.SerieArticulo AS sa ON ts.SerieArticuloId = sa.SerieArticuloId
                WHERE ts.TransferenciaId = @TransferenciaId AND sa.ArticuloId = @ArticuloId;
                """,
                new { TransferenciaId = transferenciaId, ArticuloId = articuloId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows > 0;
    }

    public async Task<bool> DesconfirmarAsync(
        int oficinaId,
        int transferenciaId,
        CancellationToken cancellationToken = default)
    {
        if (transferenciaId < 1)
        {
            return false;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!await TieneAccesoAsync(connection, transferenciaId, oficinaId, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Transferencia
                SET Estado = 'P'
                WHERE TransferenciaId = @TransferenciaId;
                """,
                new { TransferenciaId = transferenciaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows > 0;
    }

    public async Task<ConfirmarTransferenciaResponse> ConfirmarAsync(
        int oficinaId,
        int transferenciaId,
        CancellationToken cancellationToken = default)
    {
        if (transferenciaId < 1)
        {
            return new ConfirmarTransferenciaResponse(false, "Transferencia inválida.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (!await TieneAccesoAsync(connection, transferenciaId, oficinaId, cancellationToken, transaction)
                    .ConfigureAwait(false))
            {
                return new ConfirmarTransferenciaResponse(false, "Transferencia no encontrada o sin acceso.");
            }

            var estado = await connection.ExecuteScalarAsync<string>(
                new CommandDefinition(
                    "SELECT Estado FROM ALMACEN.Transferencia WHERE TransferenciaId = @TransferenciaId;",
                    new { TransferenciaId = transferenciaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (estado != "P")
            {
                return new ConfirmarTransferenciaResponse(false, "La transferencia ya está confirmada.");
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE ALMACEN.Transferencia
                    SET Estado = 'C'
                    WHERE TransferenciaId = @TransferenciaId;
                    """,
                    new { TransferenciaId = transferenciaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE sa
                    SET sa.EstadoId = 6
                    FROM ALMACEN.SerieArticulo AS sa
                    INNER JOIN ALMACEN.TransferenciaSerie AS ts ON ts.SerieArticuloId = sa.SerieArticuloId
                    WHERE ts.TransferenciaId = @TransferenciaId;
                    """,
                    new { TransferenciaId = transferenciaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new ConfirmarTransferenciaResponse(true, string.Empty);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new ConfirmarTransferenciaResponse(false, ex.Message);
        }
    }

    private static async Task<TransferenciaAccesoRow?> ObtenerTransferenciaEditableAsync(
        SqlConnection connection,
        int transferenciaId,
        int oficinaId,
        CancellationToken cancellationToken,
        SqlTransaction? transaction = null)
    {
        return await connection.QueryFirstOrDefaultAsync<TransferenciaAccesoRow>(
            new CommandDefinition(
                """
                SELECT t.TransferenciaId, t.Estado
                FROM ALMACEN.Transferencia AS t
                INNER JOIN ALMACEN.Almacen AS ao ON ao.AlmacenId = t.AlmacenOrigenId
                INNER JOIN ALMACEN.Almacen AS ad ON ad.AlmacenId = t.AlmacenDestinoId
                WHERE t.TransferenciaId = @TransferenciaId
                  AND t.Estado = 'P'
                  AND (ao.OficinaId = @OficinaId OR ad.OficinaId = @OficinaId);
                """,
                new { TransferenciaId = transferenciaId, OficinaId = oficinaId },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task<bool> TieneAccesoAsync(
        SqlConnection connection,
        int transferenciaId,
        int oficinaId,
        CancellationToken cancellationToken,
        SqlTransaction? transaction = null)
    {
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM ALMACEN.Transferencia AS t
                    INNER JOIN ALMACEN.Almacen AS ao ON ao.AlmacenId = t.AlmacenOrigenId
                    INNER JOIN ALMACEN.Almacen AS ad ON ad.AlmacenId = t.AlmacenDestinoId
                    WHERE t.TransferenciaId = @TransferenciaId
                      AND (ao.OficinaId = @OficinaId OR ad.OficinaId = @OficinaId)
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { TransferenciaId = transferenciaId, OficinaId = oficinaId },
                transaction: transaction,
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

    private sealed class SerieRow
    {
        public int SerieArticuloId { get; init; }
        public string NumeroSerie { get; init; } = string.Empty;
        public int ArticuloId { get; init; }
        public int EstadoId { get; init; }
        public string Denominacion { get; init; } = string.Empty;
    }

    private sealed class TransferenciaAccesoRow
    {
        public int TransferenciaId { get; init; }
        public string Estado { get; init; } = string.Empty;
    }
}
