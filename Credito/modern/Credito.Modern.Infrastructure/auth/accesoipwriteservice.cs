using Credito.Modern.Application.Auth;
using Credito.Modern.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Auth;

public sealed class AccesoIpWriteService(IOptions<SqlDatabaseOptions> options) : IAccesoIpWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<bool> RegistrarSiNoExisteAsync(
        string direccionIp,
        CancellationToken cancellationToken = default)
    {
        var ip = direccionIp?.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var existe = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM MAESTRO.Acceso WHERE DireccionIp = @DireccionIp;",
                new { DireccionIp = ip },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (existe > 0)
        {
            return true;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO MAESTRO.Acceso (DireccionIp) VALUES (@DireccionIp);",
                new { DireccionIp = ip },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return true;
    }
}
