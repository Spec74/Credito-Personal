using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MovimientoBovedaTicketReadService(IOptions<SqlDatabaseOptions> options)
    : IMovimientoBovedaTicketReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<MovimientoCajaTicketDto?> ObtenerAsync(
        int movimientoBovedaId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoBovedaId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<BovedaMovRow>(
            new CommandDefinition(
                """
                SELECT
                    m.MovimientoBovedaId,
                    m.Glosa,
                    m.CodOperacion AS Operacion,
                    m.Importe AS ImportePago,
                    m.IndEntrada,
                    m.FechaReg,
                    m.Estado,
                    m.CajaDiarioId
                FROM CREDITO.MovimientoBoveda AS m
                WHERE m.MovimientoBovedaId = @MovimientoBovedaId;
                """,
                new { MovimientoBovedaId = movimientoBovedaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var concepto = row.IndEntrada ? "Entrada" : "Salida";
        var estadoTxt = row.Estado ? "Activo" : "Anulado";
        return new MovimientoCajaTicketDto(
            row.MovimientoBovedaId,
            MovimientoCajaTicketLayout.Simple,
            0,
            "BÓVEDA",
            "SISTEMA",
            row.FechaReg,
            "BÓVEDA",
            $"Operación {row.Operacion} ({estadoTxt})",
            concepto,
            row.Glosa ?? "*",
            row.ImportePago,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class BovedaMovRow
    {
        public int MovimientoBovedaId { get; init; }
        public string? Glosa { get; init; }
        public string Operacion { get; init; } = string.Empty;
        public decimal ImportePago { get; init; }
        public bool IndEntrada { get; init; }
        public DateTime FechaReg { get; init; }
        public bool Estado { get; init; }
        public int? CajaDiarioId { get; init; }
    }
}
