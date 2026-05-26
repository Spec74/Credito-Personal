using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptClienteReadService(
    IOptions<SqlDatabaseOptions> options,
    IRptAvalReadService avales)
    : IRptClienteReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<RptClienteInformeDto?> ObtenerAsync(
        int personaId,
        CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<FichaRow>(
            new CommandDefinition(
                """
                SELECT
                    p.PersonaId,
                    p.NombreCompleto,
                    p.NumeroDocumento,
                    p.FechaNacimiento,
                    p.Sexo,
                    p.Direccion,
                    p.DireccionRef,
                    p.Celular1,
                    c.Nota,
                    c.DireccionNegocio,
                    c.DireccionNegocioRef,
                    CASE
                        WHEN p.EstadoCivilId IS NULL THEN ''
                        ELSE ISNULL(ec.Denominacion, '')
                    END AS EstadoCivil,
                    CASE
                        WHEN p.TipoViviendaId IS NULL THEN ''
                        ELSE ISNULL(tv.Denominacion, '')
                    END AS TipoVivienda,
                    CASE
                        WHEN c.ActividadEconId IS NULL THEN ''
                        ELSE ISNULL(oc.Denominacion, '')
                    END AS ActividadEconomica,
                    ISNULL(cy.NombreCompleto, '') AS Conyugue,
                    ISNULL(cy.NumeroDocumento, '') AS ConyugueDni,
                    ISNULL(cy.Celular1, '') AS ConyugueCelular,
                    (
                        SELECT COUNT(*)
                        FROM CREDITO.Credito AS cr
                        WHERE cr.PersonaId = p.PersonaId AND cr.Estado = 'DES'
                    ) AS CreditosDesembolsados
                FROM MAESTRO.Cliente AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                LEFT JOIN MAESTRO.ValorTabla AS ec
                    ON ec.TablaId = 11 AND ec.ItemId = p.EstadoCivilId
                LEFT JOIN MAESTRO.ValorTabla AS tv
                    ON tv.TablaId = 12 AND tv.ItemId = p.TipoViviendaId
                LEFT JOIN MAESTRO.Ocupacion AS oc ON oc.OcupacionId = c.ActividadEconId
                LEFT JOIN MAESTRO.Persona AS cy ON cy.PersonaId = p.ConyuguePersonaId
                WHERE p.PersonaId = @PersonaId;
                """,
                new { PersonaId = personaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var listaAvales = await avales.ListarPorPersonaAsync(personaId, cancellationToken).ConfigureAwait(false);

        var ficha = new RptClienteFichaDto(
            row.PersonaId,
            row.CreditosDesembolsados,
            row.NombreCompleto ?? string.Empty,
            row.NumeroDocumento ?? string.Empty,
            row.FechaNacimiento?.ToString("dd/MM/yyyy"),
            row.Sexo,
            row.Direccion,
            row.DireccionRef,
            row.Celular1,
            row.Conyugue,
            row.ConyugueDni,
            row.ConyugueCelular,
            row.TipoVivienda,
            row.EstadoCivil,
            string.Empty,
            row.ActividadEconomica,
            row.Nota,
            row.DireccionNegocio,
            row.DireccionNegocioRef);

        return new RptClienteInformeDto(ficha, listaAvales);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class FichaRow
    {
        public int PersonaId { get; init; }
        public string? NombreCompleto { get; init; }
        public string? NumeroDocumento { get; init; }
        public DateTime? FechaNacimiento { get; init; }
        public string? Sexo { get; init; }
        public string? Direccion { get; init; }
        public string? DireccionRef { get; init; }
        public string? Celular1 { get; init; }
        public string? Nota { get; init; }
        public string? DireccionNegocio { get; init; }
        public string? DireccionNegocioRef { get; init; }
        public string EstadoCivil { get; init; } = string.Empty;
        public string TipoVivienda { get; init; } = string.Empty;
        public string ActividadEconomica { get; init; } = string.Empty;
        public string Conyugue { get; init; } = string.Empty;
        public string ConyugueDni { get; init; } = string.Empty;
        public string ConyugueCelular { get; init; } = string.Empty;
        public int CreditosDesembolsados { get; init; }
    }
}
