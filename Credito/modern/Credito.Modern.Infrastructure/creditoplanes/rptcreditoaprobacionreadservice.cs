using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditoAprobacionReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditoAprobacionReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditoAprobacionRowDto>> ListarAsync(
        DateTime fechaAprobacion,
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

        var y = fechaAprobacion.Year;
        if (y < 1900 || y > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaAprobacion), "fechaAprobacion debe tener año entre 1900 y 2100.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptCreditoAprobacion",
            new
            {
                FechaAprobacion = fechaAprobacion,
                UsuarioId = usuarioId,
                OficinaId = oficinaId,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCreditoAprobacionRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
