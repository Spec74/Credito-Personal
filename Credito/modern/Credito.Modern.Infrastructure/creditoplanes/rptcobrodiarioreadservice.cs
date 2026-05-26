using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCobroDiarioReadService(IOptions<SqlDatabaseOptions> options) : IRptCobroDiarioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<RptCobroDiarioRowDto>> ListarAsync(
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
        var command = new CommandDefinition(
            "CREDITO.usp_RptCobroDiario",
            new { UsuarioId = usuarioId, OficinaId = oficinaId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCobroDiarioRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
