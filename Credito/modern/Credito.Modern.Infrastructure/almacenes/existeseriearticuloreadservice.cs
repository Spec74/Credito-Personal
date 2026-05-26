using System.Data;
using Credito.Modern.Application.Almacenes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Almacenes;

public sealed class ExisteSerieArticuloReadService(IOptions<SqlDatabaseOptions> options)
    : IExisteSerieArticuloReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ExisteSerieArticuloResponse> ValidarAsync(
        string listaSerie,
        int? cantidad,
        bool indCorrelativo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (string.IsNullOrWhiteSpace(listaSerie))
        {
            throw new ArgumentOutOfRangeException(nameof(listaSerie), "listaSerie es obligatoria.");
        }

        if (cantidad is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidad), "cantidad, si se indica, debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "ALMACEN.usp_ExisteSerieArticulo",
            new
            {
                ListaSerie = listaSerie.Trim(),
                Cantidad = cantidad,
                IndCorrelativo = indCorrelativo,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var resultado = await connection.QueryFirstOrDefaultAsync<string>(command).ConfigureAwait(false);
        return new ExisteSerieArticuloResponse(resultado);
    }
}
