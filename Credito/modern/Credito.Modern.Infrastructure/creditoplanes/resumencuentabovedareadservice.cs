using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class ResumenCuentaBovedaReadService(IOptions<SqlDatabaseOptions> options)
    : IResumenCuentaBovedaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<string?> ObtenerPrimerTextoAsync(int bovedaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaId), "bovedaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_ResumenCuentaBoveda",
            new { BovedaId = bovedaId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<string>(command).ConfigureAwait(false);
        return rows.FirstOrDefault();
    }
}
