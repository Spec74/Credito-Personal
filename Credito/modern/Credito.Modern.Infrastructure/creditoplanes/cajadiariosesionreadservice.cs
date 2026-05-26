using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaDiarioSesionReadService(IOptions<SqlDatabaseOptions> options)
    : ICajaDiarioSesionReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CajaDiarioSesionDto?> ObtenerSesionAbiertaAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1 || oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId y oficinaId deben ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<Row>(
            new CommandDefinition(
                """
                SELECT TOP (1)
                    cd.CajaDiarioId,
                    cd.CajaId,
                    c.Denominacion AS CajaDenominacion,
                    cd.FechaIniOperacion,
                    cd.SaldoInicial,
                    cd.Entradas,
                    cd.Salidas,
                    cd.SaldoFinal,
                    cd.IndCierre
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.UsuarioAsignadoId = @UsuarioId
                  AND cd.IndCierre = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId
                ORDER BY cd.CajaDiarioId DESC;
                """,
                new { UsuarioId = usuarioId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var esCentral = row.CajaDenominacion.Contains("CAJA CENTRAL", StringComparison.OrdinalIgnoreCase);
        return new CajaDiarioSesionDto(
            row.CajaDiarioId,
            row.CajaId,
            row.CajaDenominacion,
            row.FechaIniOperacion,
            row.SaldoInicial,
            row.Entradas,
            row.Salidas,
            row.SaldoFinal,
            row.IndCierre,
            esCentral);
    }

    private sealed class Row
    {
        public int CajaDiarioId { get; init; }
        public int CajaId { get; init; }
        public string CajaDenominacion { get; init; } = string.Empty;
        public DateTime FechaIniOperacion { get; init; }
        public decimal SaldoInicial { get; init; }
        public decimal Entradas { get; init; }
        public decimal Salidas { get; init; }
        public decimal SaldoFinal { get; init; }
        public bool IndCierre { get; init; }
    }
}
