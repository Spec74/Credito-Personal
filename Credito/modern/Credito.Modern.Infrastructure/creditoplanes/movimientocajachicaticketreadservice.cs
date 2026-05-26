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

        var row = await connection.QueryFirstOrDefaultAsync<ChicaTicketRow>(
            new CommandDefinition(
                """
                SELECT
                    mc.Id AS MovimientoCajaChicaId,
                    cd.UsuarioId,
                    cd.IndCierre,
                    ISNULL(p.NombreCompleto, '') AS Cliente,
                    u.NombreUsuario AS [User],
                    mc.FechaReg,
                    mc.Importe AS ImportePago,
                    mc.Descripcion,
                    mc.IndEntrada
                FROM CREDITO.MovimientoCajaChica AS mc
                INNER JOIN CREDITO.CajaChicaDiario AS cd ON cd.Id = mc.CajaChicaDiarioId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioId
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

        return new MovimientoCajaTicketDto(
            row.MovimientoCajaChicaId,
            MovimientoCajaTicketLayout.Simple,
            0,
            row.Cliente,
            row.User,
            row.FechaReg,
            "PRINCIPAL",
            "CAJA DIARIO",
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

    private sealed class ChicaTicketRow
    {
        public int MovimientoCajaChicaId { get; init; }
        public int UsuarioId { get; init; }
        public bool IndCierre { get; init; }
        public string Cliente { get; init; } = string.Empty;
        public string User { get; init; } = string.Empty;
        public DateTime FechaReg { get; init; }
        public decimal ImportePago { get; init; }
        public string? Descripcion { get; init; }
        public bool IndEntrada { get; init; }
    }
}
