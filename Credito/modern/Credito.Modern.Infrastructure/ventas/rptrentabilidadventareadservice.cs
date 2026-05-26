using System.Data;
using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class RptRentabilidadVentaReadService(IOptions<SqlDatabaseOptions> options)
    : IRptRentabilidadVentaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptRentabilidadVentaRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        bool indContado,
        bool indCredito,
        int oficinaId,
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "VENTAS.usp_RptRentabilidadVenta",
            new
            {
                FechaIni = fechaIni,
                FechaFin = fechaFin,
                IndContado = indContado,
                IndCredito = indCredito,
                OficinaId = oficinaId,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptRentabilidadVentaRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
