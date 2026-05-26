using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class SalidaAlmacenReadService(IOptions<SqlDatabaseOptions> options) : ISalidaAlmacenReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<BuscarSerieSalidaResponse> BuscarSerieAsync(
        string numeroSerie,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroSerie))
        {
            return new BuscarSerieSalidaResponse(true, "Indique el número de serie.", null, null, null, null);
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<SerieSalidaRow>(
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

        if (row is null)
        {
            return new BuscarSerieSalidaResponse(
                true,
                "El producto no existe, ingrese otro.",
                null,
                null,
                null,
                null);
        }

        return row.EstadoId switch
        {
            1 => new BuscarSerieSalidaResponse(
                true,
                "El producto se encuentra en estado SIN CONFIRMAR, ingrese otro.",
                null,
                null,
                null,
                null),
            2 => new BuscarSerieSalidaResponse(
                false,
                null,
                row.SerieArticuloId,
                row.NumeroSerie,
                row.ArticuloId,
                row.Denominacion),
            3 or 4 => new BuscarSerieSalidaResponse(
                true,
                "El producto se encuentra en estado PREVENTA o VENDIDO, ingrese otro.",
                null,
                null,
                null,
                null),
            5 => new BuscarSerieSalidaResponse(
                true,
                "El producto se encuentra en estado ANULADO, ingrese otro.",
                null,
                null,
                null,
                null),
            _ => new BuscarSerieSalidaResponse(
                true,
                "Estado de serie no válido para salida.",
                null,
                null,
                null,
                null),
        };
    }

    public async Task<int?> GetAlmacenPredeterminadoOficinaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT TOP (1) AlmacenId
                FROM ALMACEN.Almacen
                WHERE OficinaId = @OficinaId AND Estado = CAST(1 AS bit)
                ORDER BY AlmacenId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> TipoMovimientoEsSalidaAsync(
        int tipoMovimientoId,
        CancellationToken cancellationToken = default)
    {
        if (tipoMovimientoId < 1)
        {
            return false;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM MAESTRO.TipoMovimiento
                    WHERE TipoMovimientoId = @TipoMovimientoId
                      AND Estado = CAST(1 AS bit)
                      AND IndEntrada = CAST(0 AS bit)
                      AND ISNULL(IndTransferencia, 0) = CAST(0 AS bit)
                      AND ISNULL(IndDevolucion, 0) = CAST(0 AS bit)
                      AND TipoMovimientoId <> 2
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { TipoMovimientoId = tipoMovimientoId },
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

    private sealed class SerieSalidaRow
    {
        public int SerieArticuloId { get; init; }
        public string NumeroSerie { get; init; } = string.Empty;
        public int ArticuloId { get; init; }
        public int EstadoId { get; init; }
        public string Denominacion { get; init; } = string.Empty;
    }
}
