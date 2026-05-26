using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class SaldosCajaDiarioReadService(IOptions<SqlDatabaseOptions> options)
    : ISaldosCajaDiarioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<SaldoCajaSesionRowDto>> ListarCajaDiarioPorOficinaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            return Array.Empty<SaldoCajaSesionRowDto>();
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<SaldoCajaSesionRowDto>(
            new CommandDefinition(
                """
                SELECT cd.CajaDiarioId AS Id,
                       c.Denominacion AS Caja,
                       u.NombreUsuario AS Usuario,
                       cd.SaldoInicial,
                       cd.SaldoFinal,
                       cd.FechaIniOperacion,
                       cd.FechaFinOperacion,
                       cd.IndCierre,
                       cd.TransBoveda
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
                WHERE c.OficinaId = @OficinaId
                ORDER BY cd.FechaIniOperacion DESC, cd.CajaDiarioId DESC;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<SaldoCajaSesionRowDto>> ListarCajaChicaDiarioAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<SaldoCajaSesionRowDto>(
            new CommandDefinition(
                """
                SELECT cc.Id,
                       'CAJA CHICA' AS Caja,
                       u.NombreUsuario AS Usuario,
                       cc.SaldoInicial,
                       cc.SaldoFinal,
                       cc.FechaIniOperacion,
                       cc.FechaFinOperacion,
                       cc.IndCierre,
                       cc.TransBoveda
                FROM CREDITO.CajaChicaDiario AS cc
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cc.UsuarioId
                ORDER BY cc.FechaIniOperacion DESC, cc.Id DESC;
                """,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<SaldoCajaSesionRowDto>> ListarCajaDiarioBovedaAsync(
        int bovedaId,
        CancellationToken cancellationToken = default)
    {
        if (bovedaId < 1)
        {
            return Array.Empty<SaldoCajaSesionRowDto>();
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<SaldoCajaSesionRowDto>(
            new CommandDefinition(
                """
                SELECT DISTINCT
                       cd.CajaDiarioId AS Id,
                       c.Denominacion AS Caja,
                       u.NombreUsuario AS Usuario,
                       cd.SaldoInicial,
                       cd.SaldoFinal,
                       cd.FechaIniOperacion,
                       cd.FechaFinOperacion,
                       cd.IndCierre,
                       cd.TransBoveda
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
                INNER JOIN CREDITO.BovedaMov AS bm ON bm.CajaDiarioId = cd.CajaDiarioId
                WHERE bm.BovedaId = @BovedaId
                ORDER BY cd.FechaIniOperacion DESC, cd.CajaDiarioId DESC;
                """,
                new { BovedaId = bovedaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
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
