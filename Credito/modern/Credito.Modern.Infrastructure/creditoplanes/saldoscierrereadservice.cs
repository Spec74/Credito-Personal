using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class SaldosCierreReadService(IOptions<SqlDatabaseOptions> options) : ISaldosCierreReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ValidarCierreSaldosResponse> ValidarCierreMasivoAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var abiertas = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.IndCierre = CAST(0 AS bit)
                  AND cd.TransBoveda = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (abiertas > 0)
        {
            return new ValidarCierreSaldosResponse(false, "EXISTEN CAJAS ABIERTAS.");
        }

        var porCerrar = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.IndCierre = CAST(1 AS bit)
                  AND cd.TransBoveda = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (porCerrar == 0)
        {
            return new ValidarCierreSaldosResponse(false, "NO EXISTEN CAJAS POR CERRAR.");
        }

        return new ValidarCierreSaldosResponse(true, string.Empty);
    }

    public async Task<ValidarCierreSaldosResponse> ValidarCierreCajaChicaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var abiertas = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CajaChicaDiario AS ccd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = ccd.Id
                WHERE ccd.IndCierre = CAST(0 AS bit)
                  AND ccd.TransBoveda = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (abiertas > 0)
        {
            return new ValidarCierreSaldosResponse(false, "EXISTE CAJA ABIERTA.");
        }

        var porCerrar = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CajaChicaDiario AS ccd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = ccd.Id
                WHERE ccd.IndCierre = CAST(1 AS bit)
                  AND ccd.TransBoveda = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (porCerrar == 0)
        {
            return new ValidarCierreSaldosResponse(false, "NO EXISTEN CAJAS POR CERRAR.");
        }

        return new ValidarCierreSaldosResponse(true, string.Empty);
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
