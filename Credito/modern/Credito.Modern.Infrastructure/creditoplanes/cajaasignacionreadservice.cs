using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaAsignacionReadService(IOptions<SqlDatabaseOptions> options) : ICajaAsignacionReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<CajaParaAsignarRowDto>> ListarCajasParaAsignarAsync(
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
        const string sql = """
            SELECT CajaId, Denominacion
            FROM CREDITO.Caja
            WHERE Estado = CAST(1 AS bit)
              AND IndAbierto = CAST(0 AS bit)
              AND OficinaId = @OficinaId
            ORDER BY Denominacion;
            """;
        var rows = await connection
            .QueryAsync<CajaParaAsignarRowDto>(
                new CommandDefinition(sql, new { OficinaId = oficinaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<decimal> ObtenerMontoBovedaAsignacionAsync(
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var esEncargado = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CAST(CASE WHEN EXISTS (
                    SELECT 1
                    FROM MAESTRO.UsuarioRol AS ur
                    INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
                    WHERE ur.UsuarioId = @UsuarioId
                      AND ur.OficinaId = @OficinaId
                      AND r.Denominacion = N'ENCARGADO'
                ) THEN 1 ELSE 0 END AS bit);
                """,
                new { UsuarioId = usuarioId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        const string sql = """
            SELECT SaldoFinal
            FROM CREDITO.Boveda
            WHERE OficinaId = @OficinaId
              AND IndCierre = CAST(0 AS bit)
              AND IndTemporal = @IndTemporal;
            """;

        var monto = await connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                sql,
                new { OficinaId = oficinaId, IndTemporal = esEncargado },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return monto ?? 0m;
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
