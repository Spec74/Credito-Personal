using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class EntradaSalidaCajaDiarioReadService(IOptions<SqlDatabaseOptions> options)
    : IEntradaSalidaCajaDiarioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<bool?> GetTipoOperacionEsEntradaAsync(
        int tipoOperacionId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (tipoOperacionId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tipoOperacionId), "tipoOperacionId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool?>(
            new CommandDefinition(
                "SELECT IndEntrada FROM MAESTRO.TipoOperacion WHERE TipoOperacionId = @TipoOperacionId;",
                new { TipoOperacionId = tipoOperacionId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<decimal?> GetSaldoFinalCajaDiarioAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                "SELECT SaldoFinal FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId;",
                new { CajaDiarioId = cajaDiarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> CajaDiarioEstaCerradaAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var indCierre = await connection.ExecuteScalarAsync<bool?>(
            new CommandDefinition(
                "SELECT IndCierre FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId;",
                new { CajaDiarioId = cajaDiarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return indCierre ?? true;
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
