using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CentralRiesgoGenerarReadService(IOptions<SqlDatabaseOptions> options)
    : ICentralRiesgoGenerarReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<CentralRiesgoGenerarRowDto>> ListarAsync(
        int oficinaId,
        int anio,
        int mes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (anio < 1900 || anio > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(anio), "anio debe estar entre 1900 y 2100.");
        }

        if (mes < 1 || mes > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mes), "mes debe estar entre 1 y 12.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_CentralRiesgoGenerar",
            new { OficinaId = oficinaId, Anio = anio, Mes = mes },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<CentralRiesgoGenerarRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
