using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditoReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditoRowDto>> ListarAsync(
        int oficinaId,
        int? gestorId,
        string estadoCredito,
        DateTime fechaIni,
        DateTime fechaFin,
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

        if (gestorId is { } gid && gid < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(gestorId), "gestorId, si se indica, debe ser >= 1.");
        }

        var estado = estadoCredito.Trim();
        if (estado.Length is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(estadoCredito),
                "estadoCredito debe tener entre 1 y 32 caracteres (p. ej. CRE, DES, PAG).");
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
            "CREDITO.usp_RptCredito",
            new
            {
                OficinaId = oficinaId,
                GestorId = gestorId,
                Estado = estado,
                FechaDesIni = fechaIni,
                FechaDesFin = fechaFin,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCreditoRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
