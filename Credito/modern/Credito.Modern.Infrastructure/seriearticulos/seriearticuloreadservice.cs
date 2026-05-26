using Credito.Modern.Application.SerieArticulos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.SerieArticulos;

public sealed class SerieArticuloReadService(IOptions<SqlDatabaseOptions> options) : ISerieArticuloReadService
{
    private const string Sql = """
        SELECT TOP (@Take)
               s.SerieArticuloId,
               s.NumeroSerie,
               s.AlmacenId,
               s.ArticuloId,
               s.EstadoId,
               s.MovimientoDetEntId,
               s.MovimientoDetSalId
        FROM ALMACEN.SerieArticulo AS s
        WHERE s.AlmacenId = @AlmacenId
          AND s.ArticuloId = @ArticuloId
          AND s.EstadoId = @EstadoId
        ORDER BY s.SerieArticuloId;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<SerieArticuloListItemDto>> ListarPorAlmacenArticuloAsync(
        int almacenId,
        int articuloId,
        int estadoId,
        int limite,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (almacenId < 1 || articuloId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(almacenId), "almacenId y articuloId deben ser >= 1.");
        }

        if (estadoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(estadoId), "estadoId debe ser >= 1.");
        }

        var take = limite switch
        {
            < 1 => 1,
            > 500 => 500,
            var v => v,
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            Sql,
            new { AlmacenId = almacenId, ArticuloId = articuloId, EstadoId = estadoId, Take = take },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<SerieArticuloListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
