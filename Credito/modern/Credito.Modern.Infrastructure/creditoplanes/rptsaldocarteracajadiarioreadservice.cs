using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptSaldoCarteraCajaDiarioReadService(IOptions<SqlDatabaseOptions> options)
    : IRptSaldoCarteraCajaDiarioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptSaldoCarteraCajaDiarioRowDto>> ListarAsync(
        int? usuarioId,
        int oficinaId,
        int anioIni,
        int mesIni,
        int anioFin,
        int mesFin,
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

        ValidateAnioMes(nameof(anioIni), anioIni, nameof(mesIni), mesIni);
        ValidateAnioMes(nameof(anioFin), anioFin, nameof(mesFin), mesFin);

        var iniOrd = anioIni * 12 + mesIni;
        var finOrd = anioFin * 12 + mesFin;
        if (iniOrd > finOrd)
        {
            throw new ArgumentOutOfRangeException(
                nameof(anioIni),
                "El periodo inicial (anioIni/mesIni) no puede ser posterior al periodo final (anioFin/mesFin).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptSaldoCarteraCajaDiario",
            new
            {
                UsuarioId = usuarioId,
                OficinaId = oficinaId,
                AnioIni = anioIni,
                MesIni = mesIni,
                AnioFin = anioFin,
                MesFin = mesFin,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptSaldoCarteraCajaDiarioRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    private static void ValidateAnioMes(string anioName, int anio, string mesName, int mes)
    {
        if (anio < 1900 || anio > 2100)
        {
            throw new ArgumentOutOfRangeException(anioName, "El año debe estar entre 1900 y 2100.");
        }

        if (mes < 1 || mes > 12)
        {
            throw new ArgumentOutOfRangeException(mesName, "El mes debe estar entre 1 y 12.");
        }
    }
}
