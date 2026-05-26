using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.UsuariosAdmin;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Credito.Modern.Api.Credito;

/// <summary>Paridad MVC <c>Reporte/CobranzaPagos</c> (JSON + export).</summary>
internal static class CobranzaPagosEndpoints
{
    public static void MapCobranzaPagosEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-cobranza-pagos",
                async Task<Results<Ok<CobranzaPagosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    [FromQuery] int? usuarioId,
                    [FromQuery(Name = "pGestorid")] int? gestorId,
                    [FromQuery] int? oficinaId,
                    [FromQuery(Name = "pOficinaid")] int? pOficinaid,
                    IRptCobroDiarioDetalleReadService rptDetalle,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (uid, oid, err) = ResolveCobranzaQuery(
                        httpContext,
                        usuarioId ?? gestorId,
                        oficinaId ?? pOficinaid);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptCobranzaPagos");
                    try
                    {
                        var items = await rptDetalle.ListarCobranzaAsync(uid, oid, ct).ConfigureAwait(false);
                        return TypedResults.Ok(CobranzaPagosResponse.FromRows(items, uid, oid));
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or ArgumentOutOfRangeException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptCobranzaPagos")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CobranzaPagosResponse>();

        app.MapGet(
                "/api/v1/credito/rpt-cobranza-pagos-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    [FromQuery] int? usuarioId,
                    [FromQuery(Name = "pGestorid")] int? gestorId,
                    [FromQuery] int? oficinaId,
                    [FromQuery(Name = "pOficinaid")] int? pOficinaid,
                    IRptCobroDiarioDetalleReadService rptDetalle,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (uid, oid, err) = ResolveCobranzaQuery(
                        httpContext,
                        usuarioId ?? gestorId,
                        oficinaId ?? pOficinaid);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptCobranzaPagosCsv");
                    try
                    {
                        var items = await rptDetalle.ListarCobranzaAsync(uid, oid, ct).ConfigureAwait(false);
                        var bytes = RptCobroDiarioDetalleCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", "cobranza-pagos.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or ArgumentOutOfRangeException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptCobranzaPagosCsv")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/credito/rpt-cobranza-pagos-excel",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    [FromQuery] int? usuarioId,
                    [FromQuery(Name = "pGestorid")] int? gestorId,
                    [FromQuery] int? oficinaId,
                    [FromQuery(Name = "pOficinaid")] int? pOficinaid,
                    IRptCobroDiarioDetalleReadService rptDetalle,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (uid, oid, err) = ResolveCobranzaQuery(
                        httpContext,
                        usuarioId ?? gestorId,
                        oficinaId ?? pOficinaid);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptCobranzaPagosExcel");
                    try
                    {
                        var items = await rptDetalle.ListarCobranzaAsync(uid, oid, ct).ConfigureAwait(false);
                        var (gestorNombre, oficinaNombre) = await CobranzaPagosExportLabels.ResolveAsync(
                            uid,
                            oid,
                            usuarios,
                            oficinas,
                            ct).ConfigureAwait(false);
                        var fecha = DateTime.Now;
                        var bytes = CobranzaPagosXlsxFormatter.ToXlsx(
                            items,
                            gestorNombre,
                            oficinaNombre,
                            fecha);
                        var fileName = $"CobranzaPagos_{fecha:yyyyMMdd_HHmmss}.xlsx";
                        return TypedResults.File(
                            bytes,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error generando Excel cobranza pagos");
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptCobranzaPagosExcel")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static (int? UsuarioId, int? OficinaId, ProblemHttpResult? Error) ResolveCobranzaQuery(
        HttpContext httpContext,
        int? usuarioId,
        int? oficinaId) =>
        CobroDiarioReportAccess.ResolveCobranzaFiltros(httpContext, usuarioId, oficinaId);

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        log.LogError(ex, "Error en cobranza pagos");
        var detail = "No se pudo obtener la cobranza por gestor.";
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        var status = ex is ArgumentOutOfRangeException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status503ServiceUnavailable;
        return TypedResults.Problem(
            statusCode: status,
            title: status == StatusCodes.Status400BadRequest ? "Parámetros inválidos" : "Error de base de datos",
            detail: detail);
    }
}

internal sealed class CobranzaPagosResponse
{
    public required IReadOnlyList<CobranzaPagosRowResponse> Data { get; init; }

    public required CobranzaPagosResumenResponse Resumen { get; init; }

    public CobranzaPagosFiltrosResponse? FiltrosAplicados { get; init; }

    public static CobranzaPagosResponse FromRows(
        IReadOnlyList<RptCobroDiarioDetalleRowDto> rows,
        int? usuarioIdAplicado,
        int? oficinaIdAplicado)
    {
        var data = rows.Select(CobranzaPagosRowResponse.FromDto).ToList();
        return new CobranzaPagosResponse
        {
            Data = data,
            Resumen = new CobranzaPagosResumenResponse
            {
                TotalClientes = data.Count,
                TotalCredito = data.Sum(x => x.MontoCredito),
                TotalPagado = data.Sum(x => x.TotalPago ?? 0),
                TotalSaldo = data.Sum(x => x.Saldo ?? 0),
            },
            FiltrosAplicados = new CobranzaPagosFiltrosResponse
            {
                UsuarioId = usuarioIdAplicado,
                OficinaId = oficinaIdAplicado,
            },
        };
    }
}

internal sealed class CobranzaPagosFiltrosResponse
{
    public int? UsuarioId { get; init; }

    public int? OficinaId { get; init; }
}

internal sealed class CobranzaPagosResumenResponse
{
    public int TotalClientes { get; init; }

    public decimal TotalCredito { get; init; }

    public decimal TotalPagado { get; init; }

    public decimal TotalSaldo { get; init; }
}

internal sealed class CobranzaPagosRowResponse
{
    public long? Nro { get; init; }

    public string? Cliente { get; init; }

    public string FormaPago { get; init; } = string.Empty;

    public decimal MontoCredito { get; init; }

    public decimal Interes { get; init; }

    public decimal? MontoTotal { get; init; }

    public DateTime FechaPrimerPago { get; init; }

    public DateTime FechaVencimiento { get; init; }

    public decimal? Saldo { get; init; }

    public decimal? TotalPago { get; init; }

    public int? DiasAtrazoMora { get; init; }

    public string? Pagos { get; init; }

    public IReadOnlyList<string> PagosLista { get; init; } = [];

    public int PagosCount { get; init; }

    public int ImpagosCount { get; init; }

    public static CobranzaPagosRowResponse FromDto(RptCobroDiarioDetalleRowDto row)
    {
        var lista = string.IsNullOrWhiteSpace(row.Pagos)
            ? []
            : row.Pagos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        var pagosCount = 0;
        var impagosCount = 0;
        foreach (var pago in lista)
        {
            if (IsImpago(pago))
            {
                impagosCount++;
            }
            else
            {
                pagosCount++;
            }
        }

        return new CobranzaPagosRowResponse
        {
            Nro = row.Nro,
            Cliente = row.Cliente,
            FormaPago = row.FormaPago,
            MontoCredito = row.MontoCredito,
            Interes = row.Interes,
            MontoTotal = row.MontoTotal,
            FechaPrimerPago = row.FechaPrimerPago,
            FechaVencimiento = row.FechaVencimiento,
            Saldo = row.Saldo,
            TotalPago = row.TotalPago,
            DiasAtrazoMora = row.DiasAtrazoMora,
            Pagos = row.Pagos,
            PagosLista = lista,
            PagosCount = pagosCount,
            ImpagosCount = impagosCount,
        };
    }

    private static bool IsImpago(string pagoRaw)
    {
        var pagoTrim = pagoRaw.Trim();
        var open = pagoTrim.IndexOf('(');
        if (open >= 0)
        {
            pagoTrim = pagoTrim[..open].Trim();
        }

        if (decimal.TryParse(pagoTrim, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
        {
            return v == 0;
        }

        return pagoTrim is "0" or "0.00" || pagoTrim.StartsWith("0.00", StringComparison.Ordinal);
    }
}
