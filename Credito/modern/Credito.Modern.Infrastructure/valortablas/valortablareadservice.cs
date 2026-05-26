using Credito.Modern.Application.ValorTablas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.ValorTablas;

public sealed class ValorTablaReadService(IOptions<SqlDatabaseOptions> options) : IValorTablaReadService
{
    private const string Sql = """
        SELECT v.TablaId,
               v.ItemId,
               v.Denominacion,
               v.DesCorta,
               v.Valor
        FROM MAESTRO.ValorTabla AS v
        WHERE v.TablaId = @TablaId
          AND (@SoloPositivo = 0 OR v.ItemId > 0)
        ORDER BY v.ItemId, v.Denominacion;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ValorTablaListItemDto>> GetByTablaIdAsync(
        int tablaId,
        bool soloItemIdPositivo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (tablaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tablaId), "tablaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            Sql,
            new { TablaId = tablaId, SoloPositivo = soloItemIdPositivo ? 1 : 0 },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ValorTablaListItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
