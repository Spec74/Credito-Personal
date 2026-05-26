using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditosMorososPagadosReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditosMorososPagadosReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditosMorososPagadosRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int? usuarioId,
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

        if (usuarioId is { } uid && uid < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId, si se indica, debe ser >= 1.");
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
            "CREDITO.usp_RptCreditosMorososPagados",
            new
            {
                UsuarioId = usuarioId,
                OficinaId = oficinaId,
                FechaInicio = fechaIni,
                FechaFin = fechaFin,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCreditosMorososPagadosRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
