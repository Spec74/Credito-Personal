using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class DesembolsoReadService(IOptions<SqlDatabaseOptions> options) : IDesembolsoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<DesembolsoPendienteRowDto>> ListarPendientesAsync(
        int oficinaId,
        int usuarioRegId,
        int personaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        if (personaId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId), "personaId debe ser >= 0.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                c.CreditoId,
                p.Codigo AS PersonaCodigo,
                p.NombreCompleto AS PersonaNombre,
                c.MontoCredito,
                c.MontoGastosAdm,
                c.MontoDesembolso,
                c.Estado
            FROM CREDITO.Credito AS c
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
            WHERE c.Estado = 'APR'
              AND c.OficinaId = @OficinaId
              AND (
                    (@PersonaId = 0 AND c.UsuarioRegId = @UsuarioRegId)
                 OR (@PersonaId > 0 AND c.PersonaId = @PersonaId)
              )
            ORDER BY c.CreditoId;
            """;
        var command = new CommandDefinition(
            sql,
            new { OficinaId = oficinaId, UsuarioRegId = usuarioRegId, PersonaId = personaId },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DesembolsoPendienteRowDto>(command).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<CreditoGestorPendienteRowDto>> ListarCreditosGestorDesembolsadosAsync(
        int usuarioRegId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                c.CreditoId,
                p.Codigo AS PersonaCodigo,
                p.NombreCompleto AS PersonaNombre,
                c.MontoCredito,
                c.PersonaId
            FROM CREDITO.Credito AS c
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
            WHERE c.UsuarioRegId = @UsuarioRegId
              AND c.Estado = 'DES'
            ORDER BY p.NombreCompleto, c.CreditoId;
            """;
        var rows = await connection
            .QueryAsync<CreditoGestorPendienteRowDto>(
                new CommandDefinition(sql, new { UsuarioRegId = usuarioRegId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<ValidarDesembolsoResponse> ValidarAsync(
        int cajaDiarioId,
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cxcPendientes = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CuentaxCobrar
                WHERE CreditoId = @CreditoId
                  AND Estado = 'PEN';
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (cxcPendientes > 0)
        {
            return new ValidarDesembolsoResponse(false, "Tiene Cuentas por cobrar Pendientes!");
        }

        var montoDesembolso = await connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                "SELECT MontoDesembolso FROM CREDITO.Credito WHERE CreditoId = @CreditoId;",
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (montoDesembolso is null)
        {
            return new ValidarDesembolsoResponse(false, "No existe el crédito indicado.");
        }

        var caja = await connection.QueryFirstOrDefaultAsync<CajaSaldoRow>(
            new CommandDefinition(
                """
                SELECT SaldoFinal, IndCierre
                FROM CREDITO.CajaDiario
                WHERE CajaDiarioId = @CajaDiarioId;
                """,
                new { CajaDiarioId = cajaDiarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (caja is null)
        {
            return new ValidarDesembolsoResponse(false, "No existe la caja diario indicada.");
        }

        if (caja.IndCierre)
        {
            return new ValidarDesembolsoResponse(false, "La caja diario está cerrada.");
        }

        if (montoDesembolso.Value > caja.SaldoFinal)
        {
            return new ValidarDesembolsoResponse(false, "Saldo Insuficiente!");
        }

        return new ValidarDesembolsoResponse(true, null);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class CajaSaldoRow
    {
        public decimal SaldoFinal { get; init; }
        public bool IndCierre { get; init; }
    }
}
