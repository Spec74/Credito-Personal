using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditoRentabilidadReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditoRentabilidadReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditoRentabilidadRowDto>> ListarAsync(
        int oficinaId,
        DateTime fechaIni,
        DateTime fechaFin,
        string estadoCredito,
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

        var estado = estadoCredito.Trim();
        if (estado.Length is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(estadoCredito),
                "estadoCredito debe tener entre 1 y 32 caracteres (p. ej. DES, PAG).");
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

        // El SSDL del EDMX nombra el parámetro @OficnaId (typo histórico); debe coincidir con el proc en SQL Server.
        var command = new CommandDefinition(
            "CREDITO.usp_RptCreditoRentabilidad",
            new
            {
                OficnaId = oficinaId,
                FechaIni = fechaIni,
                FechaFin = fechaFin,
                EstadoCredito = estado,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCreditoRentabilidadRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
