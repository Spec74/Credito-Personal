using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditosPorPersonaReadService(IOptions<SqlDatabaseOptions> options)
    : ICreditosPorPersonaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<CreditoPorPersonaRowDto>> ListarDesembolsadosPorPersonaAsync(
        int personaId,
        int usuarioId,
        bool esCajaCentral,
        CancellationToken cancellationToken = default)
    {
        if (personaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId), "personaId y usuarioId deben ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = esCajaCentral
            ? """
              SELECT c.CreditoId, ISNULL(c.Descripcion, '') AS Descripcion, c.MontoCredito
              FROM CREDITO.Credito AS c
              WHERE c.Estado = 'DES' AND c.PersonaId = @PersonaId
              ORDER BY c.CreditoId DESC;
              """
            : """
              SELECT c.CreditoId, ISNULL(c.Descripcion, '') AS Descripcion, c.MontoCredito
              FROM CREDITO.Credito AS c
              WHERE c.Estado = 'DES' AND c.PersonaId = @PersonaId AND c.UsuarioRegId = @UsuarioId
              ORDER BY c.CreditoId DESC;
              """;

        var rows = await connection
            .QueryAsync<CreditoPorPersonaRowDto>(
                new CommandDefinition(
                    sql,
                    new { PersonaId = personaId, UsuarioId = usuarioId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return rows.ToList();
    }
}
