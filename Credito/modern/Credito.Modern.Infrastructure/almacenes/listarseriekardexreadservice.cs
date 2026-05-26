using System.Data;
using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class ListarSerieKardexReadService(IOptions<SqlDatabaseOptions> options)
    : IListarSerieKardexReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ListarSerieKardexResponse> ObtenerPrimeraFilaAsync(
        int movimientoDetalleId,
        bool indStock,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (movimientoDetalleId < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movimientoDetalleId),
                "movimientoDetalleId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "ALMACEN.usp_ListarSerieKardex",
            new { MovimientoDetalleId = movimientoDetalleId, IndStock = indStock },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var texto = await connection.QueryFirstOrDefaultAsync<string>(command).ConfigureAwait(false);
        return new ListarSerieKardexResponse(texto);
    }
}
