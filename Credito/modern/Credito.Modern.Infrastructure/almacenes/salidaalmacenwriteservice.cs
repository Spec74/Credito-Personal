using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class SalidaAlmacenWriteService(IOptions<SqlDatabaseOptions> options) : ISalidaAlmacenWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<RealizarSalidaResponse> RealizarSalidaAsync(
        int oficinaId,
        int almacenId,
        int tipoMovimientoId,
        string glosa,
        IReadOnlyList<SerieSalidaLineaRequest> series,
        DateTime fecha,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || almacenId < 1 || tipoMovimientoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        if (series.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(series), "Debe incluir al menos una serie.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var serieIds = series.Select(s => s.SerieId).Distinct().ToList();
            var estados = (await connection.QueryAsync<(int SerieArticuloId, int EstadoId, int AlmacenId)>(
                new CommandDefinition(
                    """
                    SELECT SerieArticuloId, EstadoId, AlmacenId
                    FROM ALMACEN.SerieArticulo
                    WHERE SerieArticuloId IN @SerieIds;
                    """,
                    new { SerieIds = serieIds },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

            if (estados.Count != serieIds.Count)
            {
                throw new InvalidOperationException("Una o más series no existen.");
            }

            foreach (var e in estados)
            {
                if (e.EstadoId != 2)
                {
                    throw new InvalidOperationException(
                        "Una o más series ya no están en almacén (estado distinto de EN ALMACEN).");
                }

                if (e.AlmacenId != almacenId)
                {
                    throw new InvalidOperationException(
                        "Una o más series no pertenecen al almacén de la oficina.");
                }
            }

            var movimientoId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO ALMACEN.Movimiento (
                        TipoMovimientoId, AlmacenId, Fecha, SubTotal, IGV, AjusteRedondeo,
                        TotalImporte, EstadoId, Observacion, Documento)
                    VALUES (
                        @TipoMovimientoId, @AlmacenId, @Fecha, 0, 0, 0,
                        0, 3, @Observacion, '');
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        TipoMovimientoId = tipoMovimientoId,
                        AlmacenId = almacenId,
                        Fecha = fecha,
                        Observacion = glosa?.Trim() ?? string.Empty,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var grupos = new Dictionary<int, DetalleGrupo>();
            foreach (var item in series)
            {
                if (!grupos.TryGetValue(item.ArticuloId, out var g))
                {
                    g = new DetalleGrupo(item.ArticuloId, item.Denominacion);
                    grupos[item.ArticuloId] = g;
                }

                g.Cantidad++;
                g.Series.Add(item);
            }

            foreach (var grupo in grupos.Values)
            {
                var descripcion = grupo.Denominacion + " " + grupo.Series[0].Serie;
                for (var i = 1; i < grupo.Series.Count; i++)
                {
                    descripcion += ", " + grupo.Series[i].Serie;
                }

                var detalleId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO ALMACEN.MovimientoDet (
                            MovimientoId, ArticuloId, Cantidad, Descripcion, PrecioUnitario,
                            Descuento, Importe, IndCorrelativo, UnidadMedidaT10)
                        VALUES (
                            @MovimientoId, @ArticuloId, @Cantidad, @Descripcion, 0,
                            0, 0, CAST(0 AS bit), 1);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        new
                        {
                            MovimientoId = movimientoId,
                            grupo.ArticuloId,
                            Cantidad = grupo.Cantidad,
                            Descripcion = descripcion,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                foreach (var s in grupo.Series)
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            """
                            UPDATE ALMACEN.SerieArticulo
                            SET EstadoId = 5,
                                MovimientoDetSalId = @MovimientoDetId
                            WHERE SerieArticuloId = @SerieArticuloId
                              AND EstadoId = 2;
                            """,
                            new { MovimientoDetId = detalleId, SerieArticuloId = s.SerieId },
                            transaction: transaction,
                            cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new RealizarSalidaResponse(movimientoId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class DetalleGrupo(int articuloId, string denominacion)
    {
        public int ArticuloId { get; } = articuloId;
        public string Denominacion { get; } = denominacion;
        public int Cantidad { get; set; }
        public List<SerieSalidaLineaRequest> Series { get; } = [];
    }
}
