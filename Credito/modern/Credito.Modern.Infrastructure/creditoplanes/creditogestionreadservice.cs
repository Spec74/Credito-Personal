using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoGestionReadService(IOptions<SqlDatabaseOptions> options) : ICreditoGestionReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CreditoContextoDto?> ObtenerContextoAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<CreditoContextoDto>(
            new CommandDefinition(
                """
                SELECT c.CreditoId,
                       c.PersonaId,
                       p.NombreCompleto AS PersonaNombre,
                       c.Observacion,
                       c.IndCondonacion,
                       c.MontoCondonacion,
                       c.IndIrrecuperable,
                       c.MontoGastosAdm,
                       c.CentralRiesgo,
                       c.PersonaAvalId,
                       pa.NombreCompleto AS PersonaAvalNombre
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                LEFT JOIN MAESTRO.Persona AS pa ON pa.PersonaId = c.PersonaAvalId
                WHERE c.CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<SolicitudCreditoDetalleDto?> ObtenerSolicitudCreditoAsync(
        int solicitudCreditoId,
        CancellationToken cancellationToken = default)
    {
        if (solicitudCreditoId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<SolicitudCreditoDetalleDto>(
            new CommandDefinition(
                """
                SELECT c.CreditoId AS SolicitudCreditoId,
                       c.PersonaId,
                       CONCAT(p.NumeroDocumento, ' - ', p.NombreCompleto) AS Cliente,
                       c.ProductoId,
                       c.MontoCredito,
                       c.FormaPago,
                       c.NumeroCuotas,
                       c.Interes,
                       c.FechaPrimerPago,
                       c.MontoGastosAdm,
                       c.Observacion,
                       c.CentralRiesgo
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                WHERE c.CreditoId = @SolicitudCreditoId
                  AND c.Estado = 'CRE';
                """,
                new { SolicitudCreditoId = solicitudCreditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<CreditoPrendaDto?> ObtenerPrendaAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await CreditoPrendaSchema.EnsureAsync(connection, cancellationToken).ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<CreditoPrendaDto>(
            new CommandDefinition(
                """
                SELECT CreditoPrendaId,
                       CreditoId,
                       Descripcion,
                       MontoTasacion,
                       FechaRemate,
                       Observacion,
                       Estado
                FROM CREDITO.CreditoPrenda
                WHERE CreditoId = @CreditoId
                  AND Estado = CAST(1 AS bit);
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CargoCreditoRowDto>> ListarCargosAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            return [];
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<CargoCreditoRowDto>(
            new CommandDefinition(
                """
                SELECT c.CargoId,
                       vt.Denominacion AS TipoCargo,
                       c.NumCuota,
                       c.Descripcion,
                       c.Importe,
                       u.NombreUsuario AS UsuarioCargo,
                       c.Estado
                FROM CREDITO.Cargo AS c
                INNER JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 2 AND vt.ItemId = c.TipoCargoT2
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioId
                WHERE c.CreditoId = @CreditoId
                ORDER BY c.CargoId DESC;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CreditoEvidenciaDto>> ListarEvidenciasAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            return [];
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<(int Id, int CreditoId, string Imagen)>(
            new CommandDefinition(
                """
                SELECT Id, CreditoId, Imagen
                FROM CREDITO.CreditoImagen
                WHERE CreditoId = @CreditoId
                ORDER BY Id DESC;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows
            .Select(r => new CreditoEvidenciaDto(
                r.Id,
                r.CreditoId,
                r.Imagen,
                $"/api/v1/credito/evidencia-archivo/{r.Id}"))
            .ToList();
    }

    public async Task<CreditoGrillaPersonaPageDto> ListarCreditosGrillaPersonaAsync(
        int oficinaId,
        int personaId,
        bool grupoActivo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;
        var offset = (page - 1) * pageSize;

        var estados = grupoActivo
            ? new[] { "PEN", "AP1", "APR", "DES" }
            : new[] { "ANU", "PAG", "REP" };

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string fromWhere = """
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.PersonaId = @PersonaId
              AND c.Estado IN @Estados
            """;

        var parameters = new
        {
            OficinaId = oficinaId,
            PersonaId = personaId,
            Estados = estados,
            Offset = offset,
            PageSize = pageSize,
        };

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) " + fromWhere,
                parameters,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var items = (await connection.QueryAsync<CreditoGrillaPersonaRowDto>(
            new CommandDefinition(
                $"""
                 SELECT c.CreditoId,
                        c.PersonaId,
                        c.Estado,
                        c.MontoCredito,
                        c.FechaPrimerPago,
                        c.Descripcion
                 {fromWhere}
                 ORDER BY c.CreditoId DESC
                 OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                 """,
                parameters,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        return new CreditoGrillaPersonaPageDto(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<CreditoAvalRelacionDto>> ListarAvalesPersonaAsync(
        int oficinaId,
        int personaId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<CreditoAvalRelacionDto>(
            new CommandDefinition(
                """
                SELECT 'AVAL' AS Grupo,
                       c.CreditoId,
                       c.PersonaId,
                       c.PersonaAvalId AS PersonaRelacionadaId,
                       c.MontoCredito,
                       c.Estado,
                       pa.NombreCompleto AS Persona,
                       pa.NumeroDocumento AS Dni,
                       pa.Celular1 AS Celular
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS pa ON pa.PersonaId = c.PersonaAvalId
                WHERE c.OficinaId = @OficinaId
                  AND c.PersonaId = @PersonaId
                  AND c.PersonaAvalId IS NOT NULL
                  AND c.Estado <> 'ANU'

                UNION ALL

                SELECT 'AVALADO' AS Grupo,
                       c.CreditoId,
                       c.PersonaId,
                       c.PersonaId AS PersonaRelacionadaId,
                       c.MontoCredito,
                       c.Estado,
                       p.NombreCompleto AS Persona,
                       p.NumeroDocumento AS Dni,
                       p.Celular1 AS Celular
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                WHERE c.OficinaId = @OficinaId
                  AND c.PersonaAvalId = @PersonaId
                  AND c.Estado <> 'ANU'

                ORDER BY CreditoId DESC;
                """,
                new { OficinaId = oficinaId, PersonaId = personaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<(int CreditoId, string FileName)?> ObtenerEvidenciaArchivoAsync(
        int creditoImagenId,
        CancellationToken cancellationToken = default)
    {
        if (creditoImagenId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QueryFirstOrDefaultAsync<EvidenciaArchivoRow>(
            new CommandDefinition(
                """
                SELECT CreditoId, Imagen AS FileName
                FROM CREDITO.CreditoImagen
                WHERE Id = @Id;
                """,
                new { Id = creditoImagenId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null || row.CreditoId < 1 || string.IsNullOrWhiteSpace(row.FileName))
        {
            return null;
        }

        return (row.CreditoId, row.FileName);
    }

    public async Task<PersonaCreditoFichaDto?> ObtenerPersonaCreditoFichaAsync(
        int oficinaId,
        int personaId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || personaId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<PersonaFichaRow>(
            new CommandDefinition(
                """
                SELECT p.PersonaId,
                       p.NumeroDocumento,
                       p.NombreCompleto,
                       p.Codigo,
                       c.Calificacion,
                       c.Estado AS ClienteActivo,
                       c.Bloqueado,
                       c.ClasificacionRiesgoSBS AS ClasificacionRiesgoSbsItemId,
                       c.ClasificacionRiesgoSBSObs AS ClasificacionRiesgoSbsObs,
                       ISNULL(c.TopeCredito, 0) AS TopeCredito,
                       (SELECT COUNT(*)
                        FROM CREDITO.Credito AS cr
                        WHERE cr.PersonaId = @PersonaId
                          AND cr.OficinaId = @OficinaId
                          AND cr.Estado <> 'CRE') AS TotalCreditos,
                       (SELECT COUNT(*)
                        FROM CREDITO.Credito AS cr
                        WHERE cr.PersonaId = @PersonaId
                          AND cr.OficinaId = @OficinaId
                          AND cr.Estado IN ('PEN', 'AP1', 'APR', 'DES')) AS CreditosPendientes,
                       (SELECT TOP (1) cr.CreditoId
                        FROM CREDITO.Credito AS cr
                        WHERE cr.PersonaId = @PersonaId
                          AND cr.OficinaId = @OficinaId
                          AND cr.Estado = 'CRE'
                        ORDER BY cr.FechaReg DESC) AS SolicitudCreditoId,
                       pd.Descripcion AS DepuradoDescripcion
                FROM MAESTRO.Persona AS p
                INNER JOIN MAESTRO.Cliente AS c ON c.PersonaId = p.PersonaId
                LEFT JOIN MAESTRO.PersonaDepurado AS pd
                    ON pd.PersonaId = p.PersonaId AND pd.Estado = CAST(1 AS bit)
                WHERE p.PersonaId = @PersonaId;
                """,
                new { PersonaId = personaId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var calLabel = MapCalificacionLabel(row.Calificacion);
        var sbsCodigo = MapSbsCodigo(row.ClasificacionRiesgoSbsItemId);
        var depurado = string.IsNullOrWhiteSpace(row.DepuradoDescripcion)
            ? null
            : row.DepuradoDescripcion.Trim();
        var puedeCrear = !row.Bloqueado && depurado is null;

        return new PersonaCreditoFichaDto(
            row.PersonaId,
            row.NumeroDocumento.Trim(),
            row.NombreCompleto.Trim(),
            string.IsNullOrWhiteSpace(row.Codigo) ? null : row.Codigo.Trim(),
            row.Calificacion?.Trim() ?? string.Empty,
            calLabel,
            row.ClienteActivo,
            row.ClienteActivo ? "ACTIVO" : "INACTIVO",
            row.Bloqueado,
            row.ClasificacionRiesgoSbsItemId,
            sbsCodigo,
            MapSbsLabel(sbsCodigo),
            string.IsNullOrWhiteSpace(row.ClasificacionRiesgoSbsObs)
                ? null
                : row.ClasificacionRiesgoSbsObs.Trim(),
            row.TopeCredito,
            row.TotalCreditos,
            row.CreditosPendientes,
            row.SolicitudCreditoId,
            depurado,
            puedeCrear);
    }

    private static string MapCalificacionLabel(string? calificacion)
    {
        return (calificacion ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "A" => "BUENO",
            "B" => "REGULAR",
            "C" => "MALO",
            _ => "NO TIENE",
        };
    }

    private static string? MapSbsCodigo(int? itemId)
    {
        if (itemId is null or < 1)
        {
            return null;
        }

        return (itemId.Value - 1).ToString();
    }

    private static string? MapSbsLabel(string? codigo)
    {
        return codigo switch
        {
            "0" => "NORMAL",
            "1" => "CPP",
            "2" => "DEFICIENTE",
            "3" => "DUDOSO",
            "4" => "PÉRDIDA",
            _ => null,
        };
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class PersonaFichaRow
    {
        public int PersonaId { get; init; }
        public string NumeroDocumento { get; init; } = string.Empty;
        public string NombreCompleto { get; init; } = string.Empty;
        public string? Codigo { get; init; }
        public string? Calificacion { get; init; }
        public bool ClienteActivo { get; init; }
        public bool Bloqueado { get; init; }
        public int? ClasificacionRiesgoSbsItemId { get; init; }
        public string? ClasificacionRiesgoSbsObs { get; init; }
        public decimal TopeCredito { get; init; }
        public int TotalCreditos { get; init; }
        public int CreditosPendientes { get; init; }
        public int? SolicitudCreditoId { get; init; }
        public string? DepuradoDescripcion { get; init; }
    }

    private sealed class EvidenciaArchivoRow
    {
        public int CreditoId { get; init; }
        public string FileName { get; init; } = string.Empty;
    }
}
