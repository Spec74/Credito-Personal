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

        // Paridad usp_RptMovimientoBoveda: oficina, medio tipado, agente (caja asignada o usuario reg).
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
                    m.CajaDiarioId,
                    m.TipoPagoId,
                    COALESCE(o.Denominacion, CONCAT('Oficina ', b.OficinaId)) AS Oficina,
                    COALESCE(vt.Denominacion, CONCAT('Medio ', m.TipoPagoId)) AS TipoPago,
                    COALESCE(
                        NULLIF(LTRIM(RTRIM(pCaja.NombreCompleto)), ''),
                        NULLIF(LTRIM(RTRIM(pReg.NombreCompleto)), ''),
                        NULLIF(LTRIM(RTRIM(uReg.NombreUsuario)), ''),
                        N'SISTEMA') AS Agente
                FROM CREDITO.BovedaMov AS m
                INNER JOIN CREDITO.Boveda AS b ON b.BovedaId = m.BovedaId
                LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
                LEFT JOIN MAESTRO.ValorTabla AS vt
                    ON vt.TablaId = 13 AND vt.ItemId = m.TipoPagoId
                LEFT JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = m.CajaDiarioId
                LEFT JOIN MAESTRO.Usuario AS uCaja ON uCaja.UsuarioId = cd.UsuarioAsignadoId
                LEFT JOIN MAESTRO.Persona AS pCaja ON pCaja.PersonaId = uCaja.PersonaId
                LEFT JOIN MAESTRO.Usuario AS uReg ON uReg.UsuarioId = m.UsuarioRegId
                LEFT JOIN MAESTRO.Persona AS pReg ON pReg.PersonaId = uReg.PersonaId
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
        var medio = ResumenCuentaCajaParser.DisplayLabel(row.TipoPago);
        if (string.IsNullOrWhiteSpace(medio))
        {
            medio = row.TipoPago;
        }

        return new MovimientoCajaTicketDto(
            row.MovimientoBovedaId,
            MovimientoCajaTicketLayout.Simple,
            0,
            "BÓVEDA",
            row.Agente,
            row.FechaReg,
            row.Oficina,
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
            null,
            MedioPago: medio,
            TituloComprobante: "COMPROBANTE DE BÓVEDA",
            Entrada: row.IndEntrada ? row.ImportePago : null,
            Salida: row.IndEntrada ? null : row.ImportePago);
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
        public short TipoPagoId { get; init; }
        public string Oficina { get; init; } = string.Empty;
        public string TipoPago { get; init; } = string.Empty;
        public string Agente { get; init; } = "SISTEMA";
    }
}
