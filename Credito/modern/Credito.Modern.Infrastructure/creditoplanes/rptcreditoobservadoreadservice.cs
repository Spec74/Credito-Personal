using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditoObservadoReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditoObservadoReadService
{
    private const string Sql = """
        SELECT c.OficinaId,
               o.Denominacion AS Oficina,
               c.CreditoId,
               p.NombreCompleto AS Cliente,
               c.FechaPrimerPago,
               c.FechaVencimiento,
               c.MontoCredito,
               c.Interes,
               c.UsuarioRegId AS AgenteId,
               pa.NombreCompleto AS Agente,
               c.Observacion,
               c.MontoGastosAdm AS TramiteAdm,
               c.CentralRiesgo
        FROM CREDITO.Credito AS c
        INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
        INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
        INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
        INNER JOIN MAESTRO.Persona AS pa ON pa.PersonaId = u.PersonaId
        WHERE c.Observacion IS NOT NULL
          AND LEN(c.Observacion) > 0
          AND c.Estado IN ('PEN', 'DES')
          AND c.OficinaId = @OficinaId
          AND (@UsuarioId IS NULL OR c.UsuarioRegId = @UsuarioId)
        ORDER BY pa.NombreCompleto, p.NombreCompleto;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditoObservadoRowDto>> ListarAsync(
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

        if (usuarioId is { } uid && uid < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId, si se indica, debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            Sql,
            new { OficinaId = oficinaId, UsuarioId = usuarioId },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCreditoObservadoRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
