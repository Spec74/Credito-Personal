using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class VerificarPagoTransferenciaWriteService(IOptions<SqlDatabaseOptions> options)
    : IVerificarPagoTransferenciaWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<VerificarPagoTransferenciaResponse> VerificarAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string selectSql = """
            SELECT IndTransferenciaVerificada
            FROM CREDITO.MovimientoCajaExtension
            WHERE MovimientoCajaId = @MovimientoCajaId;
            """;

        var yaVerificado = await connection
            .QueryFirstOrDefaultAsync<bool?>(
                new CommandDefinition(
                    selectSql,
                    new { MovimientoCajaId = movimientoCajaId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (yaVerificado is null)
        {
            throw new InvalidOperationException(
                $"No existe extensión de movimiento de caja para MovimientoCajaId={movimientoCajaId}.");
        }

        if (yaVerificado.Value)
        {
            return new VerificarPagoTransferenciaResponse(movimientoCajaId, YaVerificado: true);
        }

        const string updateSql = """
            UPDATE CREDITO.MovimientoCajaExtension
            SET IndTransferenciaVerificada = CAST(1 AS bit)
            WHERE MovimientoCajaId = @MovimientoCajaId;
            """;

        var rows = await connection
            .ExecuteAsync(
                new CommandDefinition(
                    updateSql,
                    new { MovimientoCajaId = movimientoCajaId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (rows < 1)
        {
            throw new InvalidOperationException(
                $"No se pudo verificar el pago para MovimientoCajaId={movimientoCajaId}.");
        }

        return new VerificarPagoTransferenciaResponse(movimientoCajaId, YaVerificado: false);
    }
}
