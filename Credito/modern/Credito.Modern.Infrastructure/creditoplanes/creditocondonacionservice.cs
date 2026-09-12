using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoCondonacionService(IOptions<SqlDatabaseOptions> options)
    : ICreditoCondonacionService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<SolicitarCondonacionResponse> SolicitarAsync(
        SolicitarCondonacionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "oficinaId debe ser >= 1.");
        }

        if (request.CajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "cajaDiarioId debe ser >= 1.");
        }

        if (request.CreditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "creditoId debe ser >= 1.");
        }

        if (request.MoraCondonacion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "moraCondonacion no puede ser negativa.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cajaOk = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS ca ON ca.CajaId = cd.CajaId
                WHERE cd.CajaDiarioId = @CajaDiarioId
                  AND ca.OficinaId = @OficinaId
                  AND cd.IndCierre = CAST(0 AS bit);
                """,
                new { request.CajaDiarioId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (cajaOk == 0)
        {
            throw new InvalidOperationException(
                "La caja diario no está abierta o no pertenece a la oficina de la sesión.");
        }

        var creditoOk = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.Credito
                WHERE CreditoId = @CreditoId
                  AND OficinaId = @OficinaId;
                """,
                new { request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (creditoOk == 0)
        {
            throw new KeyNotFoundException($"No existe el crédito {request.CreditoId} en esta oficina.");
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "CREDITO.usp_SolicitarCondonacion",
                new
                {
                    request.CajaDiarioId,
                    request.CreditoId,
                    request.MoraCondonacion,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new SolicitarCondonacionResponse(true, null);
    }

    public async Task<IReadOnlyList<CondonacionPendienteDto>> ListarPendientesAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<CondonacionPendienteDto>(
            new CommandDefinition(
                """
                SELECT cc.Id,
                       cc.CreditoId,
                       c.PersonaId,
                       ISNULL(p.NombreCompleto, N'') AS NombreCliente,
                       ISNULL(u.NombreUsuario, N'') AS NombreUsuario,
                       c.MontoCredito,
                       cc.MoraCondonacion,
                       cc.TotalPago,
                       cc.Fecha,
                       cc.CajaDiarioId
                FROM CREDITO.CreditoCondonacion AS cc
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cc.CreditoId
                LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
                WHERE cc.IndAprobado = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId
                ORDER BY cc.Id DESC;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.AsList();
    }

    public async Task<CondonacionPendienteCreditoDto> ObtenerPendientePorCreditoAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<PendienteRow>(
            new CommandDefinition(
                """
                SELECT TOP (1)
                       cc.Id,
                       cc.MoraCondonacion,
                       cc.TotalPago
                FROM CREDITO.CreditoCondonacion AS cc
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cc.CreditoId
                WHERE cc.CreditoId = @CreditoId
                  AND c.OficinaId = @OficinaId
                  AND cc.IndAprobado = CAST(0 AS bit)
                ORDER BY cc.Id DESC;
                """,
                new { CreditoId = creditoId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return new CondonacionPendienteCreditoDto(false, 0m, 0m, null);
        }

        return new CondonacionPendienteCreditoDto(true, row.MoraCondonacion, row.TotalPago, row.Id);
    }

    public async Task EliminarAsync(
        int oficinaId,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (id < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "id debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var filas = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE cc
                FROM CREDITO.CreditoCondonacion AS cc
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cc.CreditoId
                WHERE cc.Id = @Id
                  AND c.OficinaId = @OficinaId
                  AND cc.IndAprobado = CAST(0 AS bit);
                """,
                new { Id = id, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (filas == 0)
        {
            throw new KeyNotFoundException("No existe la solicitud pendiente en esta oficina.");
        }
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class PendienteRow
    {
        public int Id { get; init; }
        public decimal MoraCondonacion { get; init; }
        public decimal TotalPago { get; init; }
    }
}
