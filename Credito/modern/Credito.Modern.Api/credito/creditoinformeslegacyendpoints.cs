using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Application.Reportes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Credito.Modern.Api.Credito;

/// <summary>Informes crédito con paridad BL legacy (plan pagos, estado crédito).</summary>
internal static class CreditoInformesLegacyEndpoints
{
    public static void MapCreditoInformesLegacyEndpoints(this WebApplication app)
    {
        MapPlanPagos(app);
        MapEstadoCredito(app);
        MapCliente(app);
        MapSimuladorPlanPagos(app);
    }

    private static void MapPlanPagos(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-plan-pagos",
                async Task<Results<Ok<List<RptPlanPagosRowDto>>, ProblemHttpResult>> (
                    [FromQuery] int creditoId,
                    IRptPlanPagosReadService planPagos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptPlanPagos");
                    try
                    {
                        var items = await planPagos.ListarAsync(creditoId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "plan de pagos");
                    }
                })
            .WithName("CreditoRptPlanPagos")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptPlanPagosRowDto>>();

        app.MapGet(
                "/api/v1/credito/rpt-plan-pagos-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int creditoId,
                    IRptPlanPagosReadService planPagos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptPlanPagosCsv");
                    try
                    {
                        var items = await planPagos.ListarAsync(creditoId, ct).ConfigureAwait(false);
                        var bytes = RptPlanPagosCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", $"plan-pagos-{creditoId}.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "plan de pagos CSV");
                    }
                })
            .WithName("CreditoRptPlanPagosCsv")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/credito/rpt-plan-pagos-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int creditoId,
                    IRptPlanPagosReadService planPagos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptPlanPagosPdf");
                    try
                    {
                        var items = await planPagos.ListarAsync(creditoId, ct).ConfigureAwait(false);
                        var pdfBytes = RptPlanPagosFichaPdfDocument.Build(creditoId, items);
                        return TypedResults.File(pdfBytes, "application/pdf", $"plan-pagos-{creditoId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "plan de pagos PDF");
                    }
                })
            .WithName("CreditoRptPlanPagosPdf")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static void MapEstadoCredito(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-estado-credito",
                async Task<Results<Ok<RptEstadoCreditoInformeDto>, ProblemHttpResult>> (
                    [FromQuery] int creditoId,
                    IRptEstadoCreditoReadService estadoCredito,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptEstadoCredito");
                    try
                    {
                        var dto = await estadoCredito.ObtenerAsync(creditoId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Crédito no existe.");
                        }

                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "estado crédito");
                    }
                })
            .WithName("CreditoRptEstadoCredito")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RptEstadoCreditoInformeDto>();

        app.MapGet(
                "/api/v1/credito/rpt-estado-credito-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int creditoId,
                    IRptEstadoCreditoReadService estadoCredito,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptEstadoCreditoCsv");
                    try
                    {
                        var dto = await estadoCredito.ObtenerAsync(creditoId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Crédito no existe.");
                        }

                        var bytes = RptEstadoCreditoCsvFormatter.ToUtf8BomCsv(dto);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", $"estado-credito-{creditoId}.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "estado crédito CSV");
                    }
                })
            .WithName("CreditoRptEstadoCreditoCsv")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/credito/rpt-estado-credito-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int creditoId,
                    IRptEstadoCreditoReadService estadoCredito,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptEstadoCreditoPdf");
                    try
                    {
                        var dto = await estadoCredito.ObtenerAsync(creditoId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Crédito no existe.");
                        }

                        var pdfBytes = RptEstadoCreditoFichaPdfDocument.Build(dto);
                        return TypedResults.File(pdfBytes, "application/pdf", $"estado-credito-{creditoId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "estado crédito PDF");
                    }
                })
            .WithName("CreditoRptEstadoCreditoPdf")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static void MapCliente(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-cliente",
                async Task<Results<Ok<RptClienteInformeDto>, ProblemHttpResult>> (
                    [FromQuery] int personaId,
                    IRptClienteReadService cliente,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptCliente");
                    try
                    {
                        var dto = await cliente.ObtenerAsync(personaId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Cliente no existe.");
                        }

                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "ficha cliente");
                    }
                })
            .WithName("CreditoRptCliente")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RptClienteInformeDto>();

        app.MapGet(
                "/api/v1/credito/rpt-cliente-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int personaId,
                    IRptClienteReadService cliente,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptClienteCsv");
                    try
                    {
                        var dto = await cliente.ObtenerAsync(personaId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Cliente no existe.");
                        }

                        var bytes = RptClienteCsvFormatter.ToUtf8BomCsv(dto);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", $"cliente-{personaId}.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "ficha cliente CSV");
                    }
                })
            .WithName("CreditoRptClienteCsv")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/credito/rpt-cliente-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int personaId,
                    IRptClienteReadService cliente,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptClientePdf");
                    try
                    {
                        var dto = await cliente.ObtenerAsync(personaId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Cliente no existe.");
                        }

                        var pdfBytes = RptClienteFichaPdfDocument.Build(dto);
                        return TypedResults.File(pdfBytes, "application/pdf", $"cliente-{personaId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "ficha cliente PDF");
                    }
                })
            .WithName("CreditoRptClientePdf")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static void MapSimuladorPlanPagos(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-simulador-plan-pagos",
                async Task<Results<Ok<RptSimuladorPlanPagosInformeDto>, ProblemHttpResult>> (
                    [FromQuery] int productoId,
                    [FromQuery] decimal monto,
                    [FromQuery] int nroCuotas,
                    [FromQuery] decimal interesMensual,
                    [FromQuery] DateTime? fechaPrimerPago,
                    [FromQuery] string? formaPago,
                    [FromQuery] decimal? gastosAdm,
                    [FromQuery] string? ga,
                    [FromQuery] string? cliente,
                    [FromQuery] string? tipoDocumento,
                    [FromQuery] string? nroDocumento,
                    [FromQuery] string? direccionCliente,
                    [FromQuery] string? direccionNegocio,
                    [FromQuery] string? prendaDescripcion,
                    [FromQuery] string? asesor,
                    [FromQuery] string? telefonoCliente,
                    IRptSimuladorPlanPagosReadService simuladorPlan,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (error, query) = ParseSimuladorQuery(
                        productoId,
                        monto,
                        nroCuotas,
                        interesMensual,
                        fechaPrimerPago,
                        formaPago,
                        gastosAdm,
                        ga,
                        cliente,
                        tipoDocumento,
                        nroDocumento,
                        direccionCliente,
                        direccionNegocio,
                        prendaDescripcion,
                        asesor,
                        telefonoCliente);
                    if (error is not null)
                    {
                        return error;
                    }

                    var log = loggerFactory.CreateLogger("RptSimuladorPlanPagos");
                    try
                    {
                        var dto = await simuladorPlan.GenerarAsync(query!, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "Sin datos",
                                detail: "No se generó plan (revise monto, cuotas, producto o parámetros).");
                        }

                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "simulador plan pagos");
                    }
                })
            .WithName("CreditoRptSimuladorPlanPagos")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RptSimuladorPlanPagosInformeDto>();

        app.MapGet(
                "/api/v1/credito/rpt-simulador-plan-pagos-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int productoId,
                    [FromQuery] decimal monto,
                    [FromQuery] int nroCuotas,
                    [FromQuery] decimal interesMensual,
                    [FromQuery] DateTime? fechaPrimerPago,
                    [FromQuery] string? formaPago,
                    [FromQuery] decimal? gastosAdm,
                    [FromQuery] string? ga,
                    [FromQuery] string? cliente,
                    [FromQuery] string? tipoDocumento,
                    [FromQuery] string? nroDocumento,
                    [FromQuery] string? direccionCliente,
                    [FromQuery] string? direccionNegocio,
                    [FromQuery] string? prendaDescripcion,
                    [FromQuery] string? asesor,
                    [FromQuery] string? telefonoCliente,
                    IRptSimuladorPlanPagosReadService simuladorPlan,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (error, query) = ParseSimuladorQuery(
                        productoId,
                        monto,
                        nroCuotas,
                        interesMensual,
                        fechaPrimerPago,
                        formaPago,
                        gastosAdm,
                        ga,
                        cliente,
                        tipoDocumento,
                        nroDocumento,
                        direccionCliente,
                        direccionNegocio,
                        prendaDescripcion,
                        asesor,
                        telefonoCliente);
                    if (error is not null)
                    {
                        return error;
                    }

                    var log = loggerFactory.CreateLogger("RptSimuladorPlanPagosCsv");
                    try
                    {
                        var dto = await simuladorPlan.GenerarAsync(query!, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "Sin datos",
                                detail: "No se generó plan.");
                        }

                        var bytes = RptSimuladorPlanPagosCsvFormatter.ToUtf8BomCsv(dto);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", "simulador-plan-pagos.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "simulador plan pagos CSV");
                    }
                })
            .WithName("CreditoRptSimuladorPlanPagosCsv")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/credito/rpt-simulador-plan-pagos-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int productoId,
                    [FromQuery] decimal monto,
                    [FromQuery] int nroCuotas,
                    [FromQuery] decimal interesMensual,
                    [FromQuery] DateTime? fechaPrimerPago,
                    [FromQuery] string? formaPago,
                    [FromQuery] decimal? gastosAdm,
                    [FromQuery] string? ga,
                    [FromQuery] string? cliente,
                    [FromQuery] string? tipoDocumento,
                    [FromQuery] string? nroDocumento,
                    [FromQuery] string? direccionCliente,
                    [FromQuery] string? direccionNegocio,
                    [FromQuery] string? prendaDescripcion,
                    [FromQuery] string? asesor,
                    [FromQuery] string? telefonoCliente,
                    IRptSimuladorPlanPagosReadService simuladorPlan,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (error, query) = ParseSimuladorQuery(
                        productoId,
                        monto,
                        nroCuotas,
                        interesMensual,
                        fechaPrimerPago,
                        formaPago,
                        gastosAdm,
                        ga,
                        cliente,
                        tipoDocumento,
                        nroDocumento,
                        direccionCliente,
                        direccionNegocio,
                        prendaDescripcion,
                        asesor,
                        telefonoCliente);
                    if (error is not null)
                    {
                        return error;
                    }

                    var log = loggerFactory.CreateLogger("RptSimuladorPlanPagosPdf");
                    try
                    {
                        var dto = await simuladorPlan.GenerarAsync(query!, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "Sin datos",
                                detail: "No se generó plan.");
                        }

                        var pdfBytes = RptSimuladorPlanPagosPdfDocument.Build(dto);
                        return TypedResults.File(pdfBytes, "application/pdf", "simulador-plan-pagos.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "simulador plan pagos PDF");
                    }
                })
            .WithName("CreditoRptSimuladorPlanPagosPdf")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static (ProblemHttpResult? Error, RptSimuladorPlanPagosQuery? Query) ParseSimuladorQuery(
        int productoId,
        decimal monto,
        int nroCuotas,
        decimal interesMensual,
        DateTime? fechaPrimerPago,
        string? formaPago,
        decimal? gastosAdm,
        string? ga,
        string? cliente,
        string? tipoDocumento,
        string? nroDocumento,
        string? direccionCliente,
        string? direccionNegocio,
        string? prendaDescripcion,
        string? asesor,
        string? telefonoCliente)
    {
        if (productoId < 1)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "productoId debe ser >= 1."), null);
        }

        if (monto <= 0)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "monto debe ser > 0."), null);
        }

        if (nroCuotas < 1)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "nroCuotas debe ser >= 1."), null);
        }

        if (!FormaPagoCredito.TryNormalizar(formaPago, out _, out var fpError))
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: fpError), null);
        }

        if (fechaPrimerPago is null)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "fechaPrimerPago es obligatoria."), null);
        }

        var y = fechaPrimerPago.Value.Year;
        if (y < 1900 || y > 2100)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "fechaPrimerPago debe tener año entre 1900 y 2100."), null);
        }

        var gaNorm = string.IsNullOrWhiteSpace(ga) ? "CAP" : ga.Trim().ToUpperInvariant();
        if (gaNorm is not ("CAP" or "CUO" or "ADE"))
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "ga debe ser CAP, CUO o ADE (paridad pGA legacy)."), null);
        }

        // ADE: gastos fuera de cuotas (SP=0) y desembolso = monto (no restar gastos como CAP).
        return (null, new RptSimuladorPlanPagosQuery(
            productoId,
            monto,
            nroCuotas,
            interesMensual,
            fechaPrimerPago.Value.Date,
            formaPago!,
            gastosAdm ?? 0m,
            gaNorm,
            cliente,
            tipoDocumento,
            nroDocumento,
            direccionCliente,
            direccionNegocio,
            prendaDescripcion,
            asesor,
            telefonoCliente));
    }
    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex, string operacion)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "{Operacion}: configuración", operacion);
            return TypedResults.Problem(
                detail: ioe.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "{Operacion}", operacion);
        var detail = $"No se pudo obtener {operacion}.";
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }
}
