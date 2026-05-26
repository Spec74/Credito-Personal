using System.Data;
using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class GenerarKardexReadService(IOptions<SqlDatabaseOptions> options) : IGenerarKardexReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<GenerarKardexRowDto>> ListarAsync(
        int articuloId,
        int almacenId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (articuloId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(articuloId), "articuloId debe ser >= 1.");
        }

        if (almacenId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(almacenId), "almacenId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "ALMACEN.usp_GenerarKardex",
            new { ArticuloId = articuloId, AlmacenId = almacenId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<GenerarKardexRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
