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
            return new ValidarCierreSaldosResponse(
                false,
                "NO HAY CAJA CHICA PENDIENTE DE TRANSFERIR A BÓVEDA.");
        }

        return new ValidarCierreSaldosResponse(true, string.Empty);
    }

    /// <summary>
    /// Paridad exacta <c>BovedaController.ValidarCierre</c>:
    /// 1) cajas abiertas → bloquea
    /// 2) cajas cerradas no enviadas a bóveda → bloquea
    /// 3) si no hay pendientes (todas enviadas) → permite cierre de bóveda
    /// </summary>
    public async Task<ValidarCierreSaldosResponse> ValidarCierreBovedaAsync(
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
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (abiertas > 0)
        {
            return new ValidarCierreSaldosResponse(false, "EXISTEN CAJAS ABIERTAS.");
        }

        var cerradasNoEnviadas = await connection.ExecuteScalarAsync<int>(
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

        if (cerradasNoEnviadas > 0)
        {
            return new ValidarCierreSaldosResponse(
                false,
                "EXISTEN CAJAS CERRADAS NO ENVIADAS A BOVEDA. REVISE FORMULARIO SALDOS CAJA.");
        }

        var abiertasBoveda = await connection.QueryAsync<(int BovedaId, decimal SaldoFinal, bool IndTemporal)>(
            new CommandDefinition(
                """
                SELECT BovedaId, SaldoFinal, IndTemporal
                FROM CREDITO.Boveda
                WHERE OficinaId = @OficinaId AND IndCierre = CAST(0 AS bit);
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        foreach (var vault in abiertasBoveda)
        {
            var sumaMedios = await connection.ExecuteScalarAsync<decimal>(
                new CommandDefinition(
                    BovedaCuadreSql.SumaMedios,
                    new { vault.BovedaId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            var delta = decimal.Round(sumaMedios - vault.SaldoFinal, 2);
            if (Math.Abs(delta) > BovedaCuadreSql.Tolerancia)
            {
                var cual = vault.IndTemporal ? "temporal" : "principal";
                return new ValidarCierreSaldosResponse(
                    false,
                    $"La bóveda {cual} no cuadra: Σ medios ({sumaMedios:0.00}) ≠ saldo final ({vault.SaldoFinal:0.00}). Delta {delta:0.00}.");
            }
        }

        return new ValidarCierreSaldosResponse(true, string.Empty);
    }

    public async Task<ValidarCierreSaldosResponse> ValidarCierreBovedaTemporalAsync(
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

        var existeTemporal = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM CREDITO.Boveda
                    WHERE OficinaId = @OficinaId
                      AND IndCierre = CAST(0 AS bit)
                      AND IndTemporal = CAST(1 AS bit)
                ) THEN 1 ELSE 0 END AS bit);
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (!existeTemporal)
        {
            return new ValidarCierreSaldosResponse(false, "No hay bóveda temporal abierta en la oficina.");
        }

        var abiertas = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.IndCierre = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (abiertas > 0)
        {
            return new ValidarCierreSaldosResponse(false, "EXISTEN CAJAS ABIERTAS.");
        }

        var cerradasNoEnviadas = await connection.ExecuteScalarAsync<int>(
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

        if (cerradasNoEnviadas > 0)
        {
            return new ValidarCierreSaldosResponse(
                false,
                "EXISTEN CAJAS CERRADAS NO ENVIADAS A BOVEDA. REVISE FORMULARIO SALDOS CAJA.");
        }

        var temporal = await connection.QueryFirstOrDefaultAsync<(int BovedaId, decimal SaldoFinal)>(
            new CommandDefinition(
                """
                SELECT TOP 1 BovedaId, SaldoFinal
                FROM CREDITO.Boveda
                WHERE OficinaId = @OficinaId
                  AND IndCierre = CAST(0 AS bit)
                  AND IndTemporal = CAST(1 AS bit)
                ORDER BY BovedaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var sumaMedios = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                BovedaCuadreSql.SumaMedios,
                new { BovedaId = temporal.BovedaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        var delta = decimal.Round(sumaMedios - temporal.SaldoFinal, 2);
        if (Math.Abs(delta) > BovedaCuadreSql.Tolerancia)
        {
            return new ValidarCierreSaldosResponse(
                false,
                $"La bóveda temporal no cuadra: Σ medios ({sumaMedios:0.00}) ≠ saldo final ({temporal.SaldoFinal:0.00}). Delta {delta:0.00}.");
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
