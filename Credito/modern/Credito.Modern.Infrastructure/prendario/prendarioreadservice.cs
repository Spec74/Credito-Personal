using System.Globalization;
using Credito.Modern.Application.Prendario;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Prendario;

/// <summary>
/// Listado y tarjetas del módulo prendario. Paridad <c>PrendarioController</c>, con dos
/// diferencias registradas en <c>docs/migration/BITACORA-DESVIACIONES.md</c>: los resultados se
/// acotan a la oficina de la sesión, y la categoría se calcula en SQL para poder paginar sobre
/// el conjunto completo.
/// </summary>
public sealed class PrendarioReadService(IOptions<SqlDatabaseOptions> options) : IPrendarioReadService
{
    private const int DiasAvisoVencimiento = 3;
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<PrendarioResumenDto> ObtenerResumenAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Las tarjetas miden cartera desembolsada, por eso se limitan a Estado = 'DES'.
        var resumen = await connection.QueryFirstAsync<PrendarioResumenDto>(
            new CommandDefinition(
                """
                DECLARE @Hoy date = dbo.ufnFecha();
                DECLARE @Limite date = DATEADD(DAY, @DiasAviso, @Hoy);

                -- COUNT sobre el CASE en vez de SUM: sin filas, SUM devuelve NULL.
                SELECT
                    COUNT(*) AS Total,
                    COUNT(CASE WHEN c.FechaVencimiento >= @Hoy AND c.FechaVencimiento <= @Limite
                               THEN 1 END) AS PorVencer,
                    COUNT(CASE WHEN c.FechaVencimiento < @Hoy
                                AND (c.FechaRemate IS NULL OR c.FechaRemate >= @Hoy)
                               THEN 1 END) AS Vencidos,
                    COUNT(CASE WHEN c.FechaRemate IS NOT NULL AND c.FechaRemate < @Hoy
                               THEN 1 END) AS Rematados
                FROM CREDITO.Credito AS c
                WHERE c.OficinaId = @OficinaId
                  AND c.EsPrendario = CAST(1 AS bit)
                  AND c.Estado = 'DES';
                """,
                new { OficinaId = oficinaId, DiasAviso = DiasAvisoVencimiento },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return resumen;
    }

    public async Task<PrendarioListaPageDto> ListarAsync(
        int oficinaId,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagina = page < 1 ? 1 : page;
        var tamano = pageSize is < 1 or > 200 ? 20 : pageSize;
        var termino = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // El listado incluye ProductoId = 2 sin EsPrendario para no ocultar los creditos
        // prendarios anteriores a la existencia del indicador: salen como SinBienes.
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                """
                DECLARE @Hoy date = dbo.ufnFecha();
                DECLARE @Limite date = DATEADD(DAY, @DiasAviso, @Hoy);

                SELECT c.CreditoId,
                       c.PersonaId,
                       p.NumeroDocumento,
                       p.NombreCompleto,
                       p.Celular1 AS Celular,
                       c.NumeroContratoPrendario,
                       ISNULL(c.MontoTasacion, 0) AS MontoTasacion,
                       c.MontoCredito,
                       c.FechaVencimiento,
                       c.FechaRemate,
                       c.Estado,
                       c.EsPrendario,
                       (SELECT COUNT(*) FROM CREDITO.Prenda AS pr
                        WHERE pr.CreditoId = c.CreditoId) AS Bienes,
                       CASE
                           WHEN c.EsPrendario = CAST(0 AS bit) THEN 0
                           WHEN c.Estado <> 'DES' THEN 1
                           WHEN c.FechaRemate IS NOT NULL AND c.FechaRemate < @Hoy THEN 2
                           WHEN c.FechaVencimiento < @Hoy THEN 3
                           WHEN c.FechaVencimiento <= @Limite THEN 4
                           ELSE 5
                       END AS Categoria,
                       DATEDIFF(DAY, @Hoy, c.FechaVencimiento) AS DiasParaVencer
                INTO #PrendarioListado
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                WHERE c.OficinaId = @OficinaId
                  AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
                  AND (@Buscar IS NULL
                       OR p.NombreCompleto LIKE '%' + @Buscar + '%'
                       OR p.NumeroDocumento LIKE '%' + @Buscar + '%'
                       OR c.NumeroContratoPrendario LIKE '%' + @Buscar + '%');

                SELECT COUNT(*) FROM #PrendarioListado;

                SELECT CreditoId,
                       PersonaId,
                       NumeroDocumento,
                       NombreCompleto,
                       Celular,
                       NumeroContratoPrendario,
                       MontoTasacion,
                       MontoCredito,
                       FechaVencimiento,
                       FechaRemate,
                       Estado,
                       EsPrendario,
                       Bienes,
                       Categoria,
                       DiasParaVencer
                FROM #PrendarioListado
                ORDER BY CreditoId DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                """,
                new
                {
                    OficinaId = oficinaId,
                    Buscar = termino,
                    DiasAviso = DiasAvisoVencimiento,
                    Offset = (pagina - 1) * tamano,
                    PageSize = tamano,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var total = await multi.ReadFirstAsync<int>().ConfigureAwait(false);
        var items = (await multi.ReadAsync<PrendarioCreditoRowDto>().ConfigureAwait(false)).AsList();
        return new PrendarioListaPageDto(items, total, pagina, tamano);
    }

    public async Task<PrendarioDocumentoConsulta<PrendarioContratoDto>> ObtenerContratoAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        var (cabecera, bienes) = await CargarDocumentoAsync(oficinaId, creditoId, cancellationToken)
            .ConfigureAwait(false);
        if (cabecera is null)
        {
            return new PrendarioDocumentoConsulta<PrendarioContratoDto>(
                PrendarioDocumentoEstado.NoEncontrado, null);
        }

        if (bienes.Count == 0)
        {
            return new PrendarioDocumentoConsulta<PrendarioContratoDto>(
                PrendarioDocumentoEstado.SinBienes, null);
        }

        var tasacion = cabecera.MontoTasacion > 0
            ? cabecera.MontoTasacion
            : bienes.Sum(b => b.ValorTasacion);
        // Ancla comercial del contrato = desembolso (o FechaReg si aún no desembolsó).
        var ancla = (cabecera.FechaDesembolso ?? cabecera.FechaReg).Date;
        var plazoDias = (int)(cabecera.FechaVencimiento.Date - ancla).TotalDays;
        if (plazoDias <= 0)
        {
            plazoDias = 30;
        }

        var interesMensual = cabecera.MontoCredito * (cabecera.Interes / 100m);
        var plazoTexto = cabecera.NumeroCuotas <= 1
            ? "1 MES"
            : cabecera.NumeroCuotas.ToString(CultureInfo.InvariantCulture) + " MESES";
        var contrato = new PrendarioContratoDto(
            cabecera.CreditoId,
            string.IsNullOrWhiteSpace(cabecera.NumeroContratoPrendario)
                ? cabecera.CreditoId.ToString(CultureInfo.InvariantCulture)
                : cabecera.NumeroContratoPrendario.Trim(),
            cabecera.FechaReg,
            cabecera.FechaVencimiento,
            cabecera.FechaRemate ?? cabecera.FechaVencimiento.AddDays(30),
            cabecera.FechaDesembolso ?? cabecera.FechaReg,
            plazoTexto,
            cabecera.NombreCompleto,
            cabecera.NumeroDocumento,
            cabecera.ConyugeNombre,
            cabecera.ConyugeDni,
            cabecera.EmailPersonal,
            cabecera.Celular,
            cabecera.Direccion,
            cabecera.Distrito,
            cabecera.DireccionRef,
            tasacion,
            cabecera.MontoCredito,
            cabecera.Interes,
            interesMensual,
            interesMensual / plazoDias,
            cabecera.MontoGastosAdm,
            cabecera.EjecutivoNombre ?? string.Empty,
            bienes);

        return new PrendarioDocumentoConsulta<PrendarioContratoDto>(PrendarioDocumentoEstado.Ok, contrato);
    }

    public async Task<PrendarioDocumentoConsulta<PrendarioActaDto>> ObtenerActaAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        var (cabecera, bienes) = await CargarDocumentoAsync(oficinaId, creditoId, cancellationToken)
            .ConfigureAwait(false);
        if (cabecera is null)
        {
            return new PrendarioDocumentoConsulta<PrendarioActaDto>(
                PrendarioDocumentoEstado.NoEncontrado, null);
        }

        if (bienes.Count == 0)
        {
            return new PrendarioDocumentoConsulta<PrendarioActaDto>(
                PrendarioDocumentoEstado.SinBienes, null);
        }

        var acta = new PrendarioActaDto(
            cabecera.CreditoId,
            string.IsNullOrWhiteSpace(cabecera.NumeroContratoPrendario)
                ? cabecera.CreditoId.ToString(CultureInfo.InvariantCulture)
                : cabecera.NumeroContratoPrendario.Trim(),
            cabecera.FechaReg,
            cabecera.NombreCompleto,
            cabecera.NumeroDocumento,
            cabecera.Direccion,
            cabecera.Distrito,
            cabecera.Provincia,
            cabecera.Departamento,
            bienes);

        return new PrendarioDocumentoConsulta<PrendarioActaDto>(PrendarioDocumentoEstado.Ok, acta);
    }

    private async Task<(DocumentoCabeceraRow? Cabecera, IReadOnlyList<PrendarioBienDocumentoDto> Bienes)>
        CargarDocumentoAsync(int oficinaId, int creditoId, CancellationToken cancellationToken)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                """
                SELECT c.CreditoId,
                       c.NumeroContratoPrendario,
                       c.FechaReg,
                       c.FechaPrimerPago,
                       c.FechaVencimiento,
                       c.FechaRemate,
                       c.FechaDesembolso,
                       c.NumeroCuotas,
                       c.MontoTasacion,
                       c.MontoCredito,
                       c.Interes,
                       c.MontoGastosAdm,
                       p.NombreCompleto,
                       p.NumeroDocumento,
                       p.EmailPersonal,
                       p.Celular1 AS Celular,
                       p.Direccion,
                       p.DireccionRef,
                       d.Denominacion AS Distrito,
                       pr.Denominacion AS Provincia,
                       dep.Denominacion AS Departamento,
                       cony.NombreCompleto AS ConyugeNombre,
                       cony.NumeroDocumento AS ConyugeDni,
                       eje.NombreCompleto AS EjecutivoNombre
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                LEFT JOIN MAESTRO.Distrito AS d ON d.idDist = p.DistritoId
                LEFT JOIN MAESTRO.Provincia AS pr ON pr.idProv = d.idProv
                LEFT JOIN MAESTRO.Departamento AS dep ON dep.idDepa = pr.idDepa
                LEFT JOIN MAESTRO.Persona AS cony ON cony.PersonaId = p.ConyuguePersonaId
                LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
                LEFT JOIN MAESTRO.Persona AS eje ON eje.PersonaId = u.PersonaId
                WHERE c.CreditoId = @CreditoId
                  AND c.OficinaId = @OficinaId
                  AND c.EsPrendario = CAST(1 AS bit);

                SELECT Descripcion,
                       Marca,
                       Modelo,
                       Serie,
                       Color,
                       ValorTasacion,
                       CodigoInterno,
                       Observaciones
                FROM CREDITO.Prenda
                WHERE CreditoId = @CreditoId
                ORDER BY PrendaId;
                """,
                new { OficinaId = oficinaId, CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var cabecera = await multi.ReadFirstOrDefaultAsync<DocumentoCabeceraRow>().ConfigureAwait(false);
        var bienes = (await multi.ReadAsync<PrendarioBienDocumentoDto>().ConfigureAwait(false)).AsList();
        return (cabecera, bienes);
    }

    public async Task<IReadOnlyList<PrendarioAvisoVencimientoDto>> ListarAvisosVencimientoAsync(
        int? oficinaId,
        int diasAntes,
        CancellationToken cancellationToken = default)
    {
        if (diasAntes < 1 || diasAntes > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(diasAntes));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<PrendarioAvisoVencimientoRow>(
            new CommandDefinition(
                """
                DECLARE @Hoy date = dbo.ufnFecha();
                DECLARE @Objetivo date = DATEADD(DAY, @DiasAntes, @Hoy);

                SELECT c.CreditoId,
                       c.OficinaId,
                       p.NombreCompleto AS NombreCliente,
                       p.Celular1 AS Celular,
                       c.FechaVencimiento,
                       c.MontoCredito,
                       c.Interes
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                WHERE (@OficinaId IS NULL OR c.OficinaId = @OficinaId)
                  AND c.EsPrendario = CAST(1 AS bit)
                  AND c.Estado = 'DES'
                  AND c.FechaVencimiento = @Objetivo
                  AND (c.FechaNotifWhatsapp3d IS NULL
                       OR CAST(c.FechaNotifWhatsapp3d AS date) <> @Hoy)
                ORDER BY c.CreditoId;
                """,
                new { OficinaId = oficinaId, DiasAntes = diasAntes },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows
            .Select(r => new PrendarioAvisoVencimientoDto(
                r.CreditoId,
                r.OficinaId,
                r.NombreCliente,
                r.Celular,
                r.FechaVencimiento,
                r.MontoCredito,
                r.Interes,
                r.MontoCredito + (r.MontoCredito * r.Interes / 100m)))
            .ToList();
    }

    public async Task<bool> MarcarNotificadoWhatsAppAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var n = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Credito
                SET FechaNotifWhatsapp3d = dbo.ufnFecha()
                WHERE CreditoId = @CreditoId
                  AND OficinaId = @OficinaId
                  AND EsPrendario = CAST(1 AS bit);
                """,
                new { OficinaId = oficinaId, CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    private sealed class DocumentoCabeceraRow
    {
        public int CreditoId { get; init; }
        public string? NumeroContratoPrendario { get; init; }
        public DateTime FechaReg { get; init; }
        public DateTime FechaPrimerPago { get; init; }
        public DateTime FechaVencimiento { get; init; }
        public DateTime? FechaRemate { get; init; }
        public DateTime? FechaDesembolso { get; init; }
        public int NumeroCuotas { get; init; }
        public decimal MontoTasacion { get; init; }
        public decimal MontoCredito { get; init; }
        public decimal Interes { get; init; }
        public decimal MontoGastosAdm { get; init; }
        public string NombreCompleto { get; init; } = "";
        public string NumeroDocumento { get; init; } = "";
        public string? EmailPersonal { get; init; }
        public string? Celular { get; init; }
        public string? Direccion { get; init; }
        public string? DireccionRef { get; init; }
        public string? Distrito { get; init; }
        public string? Provincia { get; init; }
        public string? Departamento { get; init; }
        public string? ConyugeNombre { get; init; }
        public string? ConyugeDni { get; init; }
        public string? EjecutivoNombre { get; init; }
    }

    private sealed class PrendarioAvisoVencimientoRow
    {
        public int CreditoId { get; init; }
        public int OficinaId { get; init; }
        public string NombreCliente { get; init; } = "";
        public string? Celular { get; init; }
        public DateTime FechaVencimiento { get; init; }
        public decimal MontoCredito { get; init; }
        public decimal Interes { get; init; }
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
