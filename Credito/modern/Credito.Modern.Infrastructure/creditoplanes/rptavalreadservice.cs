using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptAvalReadService(IOptions<SqlDatabaseOptions> options) : IRptAvalReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<RptAvalRowDto>> ListarPorPersonaAsync(
        int personaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId), "personaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptAval",
            new { PersonaId = personaId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptAvalRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
