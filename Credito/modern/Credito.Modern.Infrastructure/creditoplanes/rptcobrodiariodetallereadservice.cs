using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCobroDiarioDetalleReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCobroDiarioDetalleReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<RptCobroDiarioDetalleRowDto>> ListarAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptCobroDiarioDetalle",
            new { UsuarioId = usuarioId, OficinaId = oficinaId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCobroDiarioDetalleRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<RptCobroDiarioDetalleRowDto>> ListarCobranzaAsync(
        int? usuarioId,
        int? oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (usuarioId is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1 o null.");
        }

        if (oficinaId is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1 o null.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var parameters = new DynamicParameters();
        parameters.Add("UsuarioId", usuarioId, DbType.Int32);
        parameters.Add("OficinaId", oficinaId, DbType.Int32);
        var command = new CommandDefinition(
            "CREDITO.usp_RptCobroDiarioDetalle",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<RptCobroDiarioDetalleRowDto>(command).ConfigureAwait(false)).ToList();

        if (usuarioId is > 0)
        {
            rows = await FiltrarFilasPorGestorAsync(connection, rows, usuarioId.Value, oficinaId, cancellationToken)
                .ConfigureAwait(false);
        }

        return rows;
    }

    /// <summary>
    /// Paridad con cartera del gestor (<c>Credito.UsuarioRegId</c>) cuando el SP devuelve toda la oficina.
    /// </summary>
    private static async Task<List<RptCobroDiarioDetalleRowDto>> FiltrarFilasPorGestorAsync(
        SqlConnection connection,
        List<RptCobroDiarioDetalleRowDto> rows,
        int usuarioId,
        int? oficinaId,
        CancellationToken cancellationToken)
    {
        const string sqlClientes = """
            SELECT DISTINCT RTRIM(LTRIM(p.NombreCompleto)) AS Cliente
            FROM CREDITO.Credito AS c
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
            WHERE c.UsuarioRegId = @UsuarioId
              AND (@OficinaId IS NULL OR c.OficinaId = @OficinaId)
              AND c.Estado IN ('APR', 'DES', 'PEN');
            """;

        var clientesGestor = await connection
            .QueryAsync<string>(
                new CommandDefinition(
                    sqlClientes,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var permitidos = new HashSet<string>(
            clientesGestor.Select(NormalizeClienteKey),
            StringComparer.Ordinal);

        if (permitidos.Count == 0)
        {
            return [];
        }

        return rows.Where(r => permitidos.Contains(NormalizeClienteKey(r.Cliente))).ToList();
    }

    private static string NormalizeClienteKey(string? nombre) =>
        (nombre ?? string.Empty).Trim().ToUpperInvariant();
}
