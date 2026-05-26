using Credito.Modern.Application.Ventas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Ventas;

public sealed class VentaRapidaCajaDiarioReadService(IOptions<SqlDatabaseOptions> options)
    : IVentaRapidaCajaDiarioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CajaDiarioVentaRapidaDto?> ObtenerAbiertaPorUsuarioAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
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
                    cd.FechaIniOperacion
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

        return new CajaDiarioVentaRapidaDto(
            row.CajaDiarioId,
            row.CajaId,
            row.CajaDenominacion,
            row.FechaIniOperacion);
    }

    private sealed class Row
    {
        public int CajaDiarioId { get; init; }
        public int CajaId { get; init; }
        public string CajaDenominacion { get; init; } = string.Empty;
        public DateTime FechaIniOperacion { get; init; }
    }
}
