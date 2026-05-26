using Credito.Modern.Application.Clientes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Clientes;

public sealed class ClienteListadoReadService(IOptions<SqlDatabaseOptions> options)
    : IClienteListadoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Codigo"] = "Codigo",
            ["Cliente"] = "Cliente",
            ["Documento"] = "Documento",
            ["Celular"] = "Celular",
            ["Email"] = "Email",
            ["Direccion"] = "Direccion",
            ["PersonaId"] = "PersonaId",
        };

    public async Task<ClienteListadoResultDto> ListarAsync(
        int usuarioId,
        string? buscar,
        int page,
        int pageSize,
        string sortField,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var offset = (page - 1) * pageSize;
        var buscarTrim = buscar?.Trim() ?? string.Empty;
        var conBuscar = buscarTrim.Length >= 2;
        var clave = $"%{buscarTrim}%";
        var sortCol = SortColumns.TryGetValue(sortField ?? "Codigo", out var col) ? col : "Codigo";
        var sortDir = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        string fromSql;
        object parameters;
        if (conBuscar)
        {
            fromSql = """
                FROM MAESTRO.Persona AS p
                INNER JOIN MAESTRO.Cliente AS c ON c.PersonaId = p.PersonaId
                WHERE (
                    p.NombreCompleto LIKE @Clave
                    OR p.NumeroDocumento LIKE @Clave
                    OR ISNULL(p.Codigo, '') LIKE @Clave
                    OR ISNULL(p.Celular1, '') LIKE @Clave
                    OR ISNULL(p.EmailPersonal, '') LIKE @Clave
                )
                """;
            parameters = new { Clave = clave, UsuarioId = usuarioId, Offset = offset, PageSize = pageSize };
        }
        else
        {
            fromSql = """
                FROM (
                    SELECT DISTINCT cr.PersonaId
                    FROM CREDITO.Credito AS cr
                    WHERE cr.UsuarioRegId = @UsuarioId
                ) AS u
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
                """;
            parameters = new { UsuarioId = usuarioId, Offset = offset, PageSize = pageSize };
        }

        var countSql = $"SELECT COUNT(DISTINCT p.PersonaId) {fromSql};";
        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var dataSql = $"""
            SELECT DISTINCT
                p.PersonaId,
                ISNULL(p.Codigo, '') AS Codigo,
                p.NombreCompleto AS Cliente,
                RTRIM(ISNULL(p.TipoDocumento, '') + ' ' + ISNULL(p.NumeroDocumento, '')) AS Documento,
                p.Celular1 AS Celular,
                p.EmailPersonal AS Email,
                p.Direccion
            {fromSql}
            ORDER BY {sortCol} {sortDir}, p.PersonaId ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var rows = await connection.QueryAsync<ClienteListadoRowDto>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return new ClienteListadoResultDto(rows.ToList(), total, page, pageSize);
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
