using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptClientesInactivosReadService(IOptions<SqlDatabaseOptions> options)
    : IRptClientesInactivosReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptClientesInactivosRowDto>> ListarAsync(
        DateTime? fechaIni,
        DateTime? fechaFin,
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

        if (fechaIni is not null && fechaFin is not null)
        {
            var yi = fechaIni.Value.Year;
            var yf = fechaFin.Value.Year;
            if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
            {
                throw new ArgumentOutOfRangeException(nameof(fechaIni), "Las fechas deben tener año entre 1900 y 2100.");
            }

            if (fechaIni.Value.Date > fechaFin.Value.Date)
            {
                throw new ArgumentOutOfRangeException(nameof(fechaIni), "fechaIni no puede ser posterior a fechaFin.");
            }
        }
        else if (fechaIni is not null || fechaFin is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaIni), "Indique fechaIni y fechaFin, o ninguna (paridad gestor legacy).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptClientesInactivos",
            new
            {
                UsuarioId = usuarioId,
                OficinaId = oficinaId,
                FechaInicio = fechaIni,
                FechaFin = fechaFin,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptClientesInactivosRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
