using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

/// <summary>Paridad <c>CanjearPuntosController</c> y <c>TarjetaPuntoBL.CanjearPuntos</c>.</summary>
public sealed class CanjearPuntosService(IOptions<SqlDatabaseOptions> options) : ICanjearPuntosService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<TarjetaPuntoDto?> ObtenerTarjetaAsync(
        int personaId,
        CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            return null;
        }

        EnsureConnection();

        const string sql = """
            SELECT TOP (1)
                TarjetaPuntoId,
                PersonaId,
                TotalPuntos,
                Estado
            FROM VENTAS.TarjetaPunto
            WHERE PersonaId = @PersonaId
              AND Estado = CAST(1 AS bit);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection
            .QuerySingleOrDefaultAsync<TarjetaPuntoDto>(
                new CommandDefinition(sql, new { PersonaId = personaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ArticuloCanjeListItemDto>> ListarArticulosCanjeablesAsync(
        int personaId,
        CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            return Array.Empty<ArticuloCanjeListItemDto>();
        }

        var tarjeta = await ObtenerTarjetaAsync(personaId, cancellationToken).ConfigureAwait(false);
        if (tarjeta is null)
        {
            return Array.Empty<ArticuloCanjeListItemDto>();
        }

        EnsureConnection();

        const string sql = """
            SELECT lp.ListaPrecioId,
                   lp.ArticuloId,
                   ta.Denominacion AS TipoArticulo,
                   a.Denominacion AS ArticuloDesc,
                   lp.PuntosCanje,
                   lp.Estado
            FROM VENTAS.ListaPrecio AS lp
            INNER JOIN ALMACEN.Articulo AS a ON a.ArticuloId = lp.ArticuloId
            INNER JOIN ALMACEN.TipoArticulo AS ta ON ta.TipoArticuloId = a.TipoArticuloId
            WHERE a.Estado = CAST(1 AS bit)
              AND lp.Estado = CAST(1 AS bit)
              AND a.IndCanjeable = CAST(1 AS bit)
              AND lp.PuntosCanje IS NOT NULL
              AND lp.PuntosCanje <= @TotalPuntos
            ORDER BY lp.PuntosCanje, a.Denominacion;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection
            .QueryAsync<ArticuloCanjeListItemDto>(
                new CommandDefinition(
                    sql,
                    new { TotalPuntos = tarjeta.TotalPuntos },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<string> CanjearAsync(
        int personaId,
        string numeroSerie,
        CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId));
        }

        if (string.IsNullOrWhiteSpace(numeroSerie))
        {
            throw new ArgumentException("numeroSerie es obligatorio.", nameof(numeroSerie));
        }

        EnsureConnection();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                "EXEC dbo.usp_CanjearPuntos @CodCliente, @NumeroSerie;",
                new { CodCliente = (decimal)personaId, NumeroSerie = numeroSerie.Trim() },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.FirstOrDefault()?.Trim() ?? string.Empty;
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
