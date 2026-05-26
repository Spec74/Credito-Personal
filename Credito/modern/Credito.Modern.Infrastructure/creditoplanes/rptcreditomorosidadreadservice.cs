using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditoMorosidadReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditoMorosidadReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditoMorosidadRowDto>> ListarAsync(
        int oficinaId,
        DateTime hastaFecha,
        int diasAtrazoIni,
        int diasAtrazoFin,
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

        var y = hastaFecha.Year;
        if (y < 1900 || y > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(hastaFecha), "hastaFecha debe tener año entre 1900 y 2100.");
        }

        if (diasAtrazoIni < 0 || diasAtrazoIni > 500_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(diasAtrazoIni),
                "diasAtrazoIni debe estar entre 0 y 500000.");
        }

        if (diasAtrazoFin < 0 || diasAtrazoFin > 500_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(diasAtrazoFin),
                "diasAtrazoFin debe estar entre 0 y 500000.");
        }

        if (diasAtrazoIni > diasAtrazoFin)
        {
            throw new ArgumentOutOfRangeException(
                nameof(diasAtrazoIni),
                "diasAtrazoIni no puede ser mayor que diasAtrazoFin.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptCreditoMorosidad",
            new
            {
                OficinaId = oficinaId,
                HastaFecha = hastaFecha,
                DiasAtrazoIni = diasAtrazoIni,
                DiasAtrazoFin = diasAtrazoFin,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCreditoMorosidadRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
