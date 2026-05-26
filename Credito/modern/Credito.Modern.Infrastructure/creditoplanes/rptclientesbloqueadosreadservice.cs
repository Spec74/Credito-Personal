using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptClientesBloqueadosReadService(IOptions<SqlDatabaseOptions> options)
    : IRptClientesBloqueadosReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<RptClientesBloqueadosRowDto>> ListarAsync(
        int oficinaId,
        int? usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioId is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId, si se indica, debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptClientesBloqueados",
            new { OficinaId = oficinaId, UsuarioId = usuarioId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptClientesBloqueadosRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
