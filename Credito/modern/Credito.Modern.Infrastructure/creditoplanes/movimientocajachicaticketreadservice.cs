using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MovimientoCajaChicaTicketReadService(IOptions<SqlDatabaseOptions> options)
    : IMovimientoCajaChicaTicketReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<MovimientoCajaTicketDto?> ObtenerAsync(
        int movimientoCajaChicaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaChicaId < 1 || usuarioId < 1)
        {
            return null;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Oficina real (UsuarioOficina), usuario = quien registró el mov (paridad legacy mc.Usuario).
        var row = await connection.QueryFirstOrDefaultAsync<ChicaTicketRow>(
            new CommandDefinition(
                """
                SELECT
                    mc.Id AS MovimientoCajaChicaId,
                    cd.UsuarioId,
                    cd.IndCierre,
                    mc.PersonaId,
                    mc.Operacion,
                    ISNULL(p.NombreCompleto, '') AS Cliente,
                    ISNULL(uReg.NombreUsuario, uDiario.NombreUsuario) AS [User],
                    mc.FechaReg,
                    mc.Importe AS ImportePago,
                    mc.Descripcion,
                    mc.IndEntrada,
                    COALESCE(
                        (
                            SELECT TOP 1 o.Denominacion
                            FROM MAESTRO.UsuarioOficina AS uo
                            INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = uo.OficinaId
                            WHERE uo.UsuarioId = cd.UsuarioId AND uo.Estado = CAST(1 AS bit)
                            ORDER BY uo.UsuarioOficinaId
                        ),
                        N'PRINCIPAL') AS Oficina
                FROM CREDITO.MovimientoCajaChica AS mc
                INNER JOIN CREDITO.CajaChicaDiario AS cd ON cd.Id = mc.CajaChicaDiarioId
                INNER JOIN MAESTRO.Usuario AS uDiario ON uDiario.UsuarioId = cd.UsuarioId
                LEFT JOIN MAESTRO.Usuario AS uReg ON uReg.UsuarioId = mc.UsuarioRegId
                LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = mc.PersonaId
                WHERE mc.Id = @MovimientoCajaChicaId;
                """,
                new { MovimientoCajaChicaId = movimientoCajaChicaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null || row.UsuarioId != usuarioId || row.IndCierre)
        {
            return null;
        }

        var articulo = string.IsNullOrWhiteSpace(row.Descripcion) ? "*" : row.Descripcion;
        var concepto = row.IndEntrada ? "ENTRADA" : "SALIDA";
        var op = string.IsNullOrWhiteSpace(row.Operacion) ? string.Empty : row.Operacion.Trim();
        var producto = string.IsNullOrEmpty(op) ? "CAJA CHICA" : $"CAJA CHICA · {op}";

        return new MovimientoCajaTicketDto(
            row.MovimientoCajaChicaId,
            MovimientoCajaTicketLayout.Simple,
            row.PersonaId,
            row.Cliente,
            row.User,
            row.FechaReg,
            row.Oficina,
            producto,
            concepto,
            articulo,
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
            TituloComprobante: "COMPROBANTE DE CAJA CHICA",
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

    private sealed class ChicaTicketRow
    {
        public int MovimientoCajaChicaId { get; init; }
        public int UsuarioId { get; init; }
        public bool IndCierre { get; init; }
        public int PersonaId { get; init; }
        public string Operacion { get; init; } = string.Empty;
        public string Cliente { get; init; } = string.Empty;
        public string User { get; init; } = string.Empty;
        public DateTime FechaReg { get; init; }
        public decimal ImportePago { get; init; }
        public string? Descripcion { get; init; }
        public bool IndEntrada { get; init; }
        public string Oficina { get; init; } = "PRINCIPAL";
    }
}
