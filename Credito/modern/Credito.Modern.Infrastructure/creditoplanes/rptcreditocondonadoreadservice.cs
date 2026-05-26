using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptCreditoCondonadoReadService(IOptions<SqlDatabaseOptions> options)
    : IRptCreditoCondonadoReadService
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
               c.MontoCondonacion AS MontoCondonado,
               c.UsuarioRegId AS AgenteId,
               pa.NombreCompleto AS Agente,
               c.Observacion
        FROM CREDITO.Credito AS c
        INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
        INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
        INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
        INNER JOIN MAESTRO.Persona AS pa ON pa.PersonaId = u.PersonaId
        WHERE c.Estado = 'PAG'
          AND c.IndCondonacion = CAST(1 AS bit)
          AND c.FechaMod >= @FechaIni
          AND c.FechaMod < @FechaFinExclusive
          AND c.OficinaId = @OficinaId
          AND (@UsuarioId IS NULL OR c.UsuarioRegId = @UsuarioId)
        ORDER BY pa.NombreCompleto, p.NombreCompleto;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptCreditoCondonadoRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        int oficinaId,
        int? usuarioId,
        CancellationToken cancellationToken = default)
    {
        ValidateConnection();
        ValidateDates(fechaIni, fechaFin);
        ValidateOficina(oficinaId);
        ValidateUsuario(usuarioId);

        var fechaIniDate = fechaIni.Date;
        var fechaFinExclusive = fechaFin.Date.AddDays(1);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            Sql,
            new
            {
                FechaIni = fechaIniDate,
                FechaFinExclusive = fechaFinExclusive,
                OficinaId = oficinaId,
                UsuarioId = usuarioId,
            },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptCreditoCondonadoRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }

    private void ValidateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private static void ValidateDates(DateTime fechaIni, DateTime fechaFin)
    {
        if (fechaFin.Date < fechaIni.Date)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaFin), "fechaFin no puede ser anterior a fechaIni.");
        }
    }

    private static void ValidateOficina(int oficinaId)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }
    }

    private static void ValidateUsuario(int? usuarioId)
    {
        if (usuarioId is { } uid && uid < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId, si se indica, debe ser >= 1.");
        }
    }
}
