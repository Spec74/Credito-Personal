using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptMovimientoCajaAnuladoReadService(IOptions<SqlDatabaseOptions> options)
    : IRptMovimientoCajaAnuladoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptMovimientoCajaAnuladoRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        var yi = fechaIni.Year;
        var yf = fechaFin.Year;
        if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaIni), "Las fechas deben tener año entre 1900 y 2100.");
        }

        if (fechaIni.Date > fechaFin.Date)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaIni), "fechaIni no puede ser posterior a fechaFin.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptMovimientoCajaAnulado",
            new
            {
                FechaIni = fechaIni,
                FechaFin = fechaFin,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptMovimientoCajaAnuladoRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
