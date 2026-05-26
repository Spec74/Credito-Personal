using System.Data;
using Credito.Modern.Application.Menus;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Menus;

public sealed class MenuReadService(IOptions<SqlDatabaseOptions> options) : IMenuReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<MenuItemDto>> GetMenuAsync(int oficinaId, int usuarioId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            "MAESTRO.usp_MenuLst",
            new { oficinaId, usuarioId },
            transaction: null,
            commandTimeout: 60,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<MenuItemDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
