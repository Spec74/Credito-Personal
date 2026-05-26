using Credito.Modern.Application.Clientes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Clientes;

public sealed class ClienteDetalleReadService(IOptions<SqlDatabaseOptions> options)
    : IClienteDetalleReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ClienteDetalleDto?> ObtenerPorPersonaIdAsync(
        int personaId,
        CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            return null;
        }

        EnsureConnection();

        const string sql = """
            SELECT p.PersonaId,
                   c.ClienteId,
                   p.TipoPersona,
                   p.Nombre,
                   p.ApePaterno,
                   p.ApeMaterno,
                   p.NumeroDocumento,
                   p.TipoDocumento,
                   p.Sexo,
                   p.EmailPersonal AS Email,
                   p.Celular1,
                   p.Direccion,
                   p.DireccionRef,
                   p.DistritoId,
                   CASE
                       WHEN d.idDist IS NOT NULL
                       THEN d.Denominacion + ' - ' + pr.Denominacion
                       ELSE NULL
                   END AS DistritoLabel,
                   p.FechaNacimiento,
                   c.ActividadEconId,
                   c.Calificacion,
                   c.Nota,
                   c.Estado AS ClienteEstado,
                   p.Estado AS PersonaEstado,
                   c.Bloqueado,
                   c.TopeCredito,
                   p.EstadoCivilId,
                   p.TipoViviendaId,
                   p.ConyuguePersonaId,
                   CASE
                       WHEN cy.PersonaId IS NOT NULL
                       THEN cy.NumeroDocumento + ' ' + cy.NombreCompleto
                       ELSE NULL
                   END AS ConyugueLabel,
                   c.ClasificacionRiesgoSBS AS ClasificacionRiesgoSbsId,
                   c.ClasificacionRiesgoSBSObs AS ClasificacionRiesgoSbsObs,
                   c.DireccionNegocio,
                   c.DireccionNegocioRef,
                   c.Latitud,
                   c.Longitud,
                   (
                       SELECT TOP (1) pd.Descripcion
                       FROM MAESTRO.PersonaDepurado AS pd
                       WHERE pd.PersonaId = p.PersonaId AND pd.Estado = CAST(1 AS bit)
                   ) AS DepuradoDescripcion
            FROM MAESTRO.Cliente AS c
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
            LEFT JOIN MAESTRO.Distrito AS d ON d.idDist = p.DistritoId
            LEFT JOIN MAESTRO.Provincia AS pr ON pr.idProv = d.idProv
            LEFT JOIN MAESTRO.Persona AS cy ON cy.PersonaId = p.ConyuguePersonaId
            WHERE c.PersonaId = @PersonaId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection
            .QuerySingleOrDefaultAsync<ClienteDetalleDto>(
                new CommandDefinition(sql, new { PersonaId = personaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<PersonaPorDocumentoDto?> ObtenerPersonaPorDocumentoAsync(
        string numeroDocumento,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroDocumento))
        {
            return null;
        }

        EnsureConnection();

        const string sql = """
            SELECT p.PersonaId,
                   CASE WHEN EXISTS (
                       SELECT 1 FROM MAESTRO.Cliente AS c WHERE c.PersonaId = p.PersonaId
                   ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS TieneCliente,
                   p.TipoPersona,
                   p.Nombre,
                   p.ApePaterno,
                   p.ApeMaterno,
                   p.NumeroDocumento,
                   p.Sexo,
                   p.EmailPersonal AS Email,
                   p.Celular1,
                   p.Direccion,
                   p.FechaNacimiento
            FROM MAESTRO.Persona AS p
            WHERE p.NumeroDocumento = @NumeroDocumento;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection
            .QuerySingleOrDefaultAsync<PersonaPorDocumentoDto>(
                new CommandDefinition(
                    sql,
                    new { NumeroDocumento = numeroDocumento.Trim() },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<bool> ExisteDocumentoAsync(
        string numeroDocumento,
        int? excluirPersonaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroDocumento))
        {
            return false;
        }

        EnsureConnection();

        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM MAESTRO.Cliente AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                WHERE p.NumeroDocumento = @NumeroDocumento
                  AND (@ExcluirPersonaId IS NULL OR p.PersonaId <> @ExcluirPersonaId)
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { NumeroDocumento = numeroDocumento.Trim(), ExcluirPersonaId = excluirPersonaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
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
