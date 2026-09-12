using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.Auth;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.CreditoCartera;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.CreditoTareas;
using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Application.Dashboard;
using Credito.Modern.Application.Documentos;
using Credito.Modern.Application.Integraciones;
using Credito.Modern.Application.Inventario;
using Credito.Modern.Application.ListaPrecios;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Ventas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Prendario;

internal static class PrendarioEndpoints
{
    public static void MapPrendarioEndpoints(this WebApplication app)
    {

        app.MapGet(
                "/api/v1/prendario/resumen",
                async Task<Results<Ok<PrendarioResumenDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IPrendarioReadService prendarioRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("PrendarioResumen");
                    try
                    {
                        var resumen = await prendarioRead.ObtenerResumenAsync(oficinaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(resumen);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al obtener el resumen prendario");
                        var detail = "No se pudo obtener el resumen prendario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioResumen")
            .WithSummary("Tarjetas del listado prendario sobre cartera desembolsada de la oficina.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<PrendarioResumenDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/prendario/creditos",
                async Task<Results<Ok<PrendarioListaPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IPrendarioReadService prendarioRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct,
                    string? buscar = null,
                    int page = 1,
                    int pageSize = 20) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("PrendarioListar");
                    try
                    {
                        var pagina = await prendarioRead
                            .ListarAsync(oficinaId, buscar, page, pageSize, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(pagina);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar créditos prendarios");
                        var detail = "No se pudo obtener el listado prendario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioListar")
            .WithSummary("Listado paginado de créditos prendarios de la oficina, con categoría y días para vencer.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<PrendarioListaPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/prendario/crear-solicitud",
                async Task<Results<Ok<CrearSolicitudCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearSolicitudPrendariaRequest body,
                    ICreditoSolicitudWriteService creditoSolicitud,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;

                    var log = loggerFactory.CreateLogger("PrendarioCrearSolicitud");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await creditoSolicitud
                            .CrearSolicitudPrendariaAsync(body.OficinaId, body.PersonaId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al crear la solicitud prendaria");
                        var detail = "No se pudo crear la solicitud prendaria.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioCrearSolicitud")
            .WithSummary("Paridad CreditoBL.CrearSolicitudCreditoPrendario: crédito en estado CRE con ProductoId 2 y EsPrendario.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<CrearSolicitudCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/prendario/contrato-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    IPrendarioReadService prendarioRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var consulta = await ConsultarDocumentoPrendarioAsync(
                            httpContext, oficinaId, creditoId, prendarioRead.ObtenerContratoAsync, loggerFactory, env, "contrato", ct)
                        .ConfigureAwait(false);
                    if (consulta.Error is not null)
                    {
                        return consulta.Error;
                    }

                    var pdf = RptContratoPrendarioPdfDocument.Build(consulta.Documento!);
                    return TypedResults.File(
                        pdf,
                        "application/pdf",
                        $"ContratoPrendario_{consulta.Documento!.NumeroContrato}.pdf");
                })
            .WithName("PrendarioContratoPdf")
            .WithSummary("Contrato prendario en PDF (QuestPDF). Exige al menos un bien en custodia.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/prendario/acta-entrega-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    IPrendarioReadService prendarioRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var consulta = await ConsultarDocumentoPrendarioAsync(
                            httpContext, oficinaId, creditoId, prendarioRead.ObtenerActaAsync, loggerFactory, env, "acta", ct)
                        .ConfigureAwait(false);
                    if (consulta.Error is not null)
                    {
                        return consulta.Error;
                    }

                    var pdf = RptActaEntregaPrendarioPdfDocument.Build(consulta.Documento!);
                    return TypedResults.File(
                        pdf,
                        "application/pdf",
                        $"ActaEntregaPrendario_{consulta.Documento!.NumeroContrato}.pdf");
                })
            .WithName("PrendarioActaEntregaPdf")
            .WithSummary("Acta de entrega voluntaria en PDF (QuestPDF). Exige al menos un bien en custodia.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/prendario/avisos-vencimiento/estado",
                Results<Ok<PrendarioWhatsAppEstadoDto>, ProblemHttpResult> (
                    IPrendarioWhatsAppEstadoService estado) =>
                    TypedResults.Ok(estado.Obtener()))
            .WithName("PrendarioAvisosVencimientoEstado")
            .WithSummary("Estado del aviso WhatsApp de vencimiento prendario: canal configurado, próxima corrida 08:00 Lima y última pasada. No expone el token.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<PrendarioWhatsAppEstadoDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);



        app.MapGet(
                "/api/v1/prendario/avisos-vencimiento",
                async Task<Results<Ok<IReadOnlyList<PrendarioAvisoVencimientoDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IPrendarioReadService prendarioRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    int diasAntes = 3,
                    CancellationToken ct = default) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    if (diasAntes < 1 || diasAntes > 30)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "diasAntes debe estar entre 1 y 30.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("PrendarioAvisosVencimiento");
                    try
                    {
                        var avisos = await prendarioRead
                            .ListarAvisosVencimientoAsync(oficinaId, diasAntes, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(avisos);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar avisos de vencimiento prendario");
                        var detail = "No se pudieron listar los avisos de vencimiento.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioAvisosVencimiento")
            .WithSummary("Créditos prendarios desembolsados que vencen en N días y aún no fueron avisados hoy. Paridad CreditoBL.ObtenerCreditosPrendariosPorVencer, acotado a la oficina.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<IReadOnlyList<PrendarioAvisoVencimientoDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/prendario/avisos-vencimiento/marcar",
                async Task<Results<Ok, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    IPrendarioReadService prendarioRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y creditoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("PrendarioMarcarWhatsApp");
                    try
                    {
                        var ok = await prendarioRead
                            .MarcarNotificadoWhatsAppAsync(oficinaId, creditoId, ct)
                            .ConfigureAwait(false);
                        if (!ok)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "El crédito prendario no existe en esta oficina.");
                        }

                        return TypedResults.Ok();
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al marcar el aviso WhatsApp prendario");
                        var detail = "No se pudo registrar el aviso.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioMarcarAvisoVencimiento")
            .WithSummary("Paridad CreditoBL.MarcarNotificadoWhatsapp: deja constancia de que el aviso de hoy ya se envió.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/prendario/avisos-vencimiento/enviar",
                async Task<Results<Ok<PrendarioAvisoEnvioResumenDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EnviarAvisosPrendarioRequest body,
                    IPrendarioAvisoEnvioService envio,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    var diasAntes = body.DiasAntes < 1 ? 3 : body.DiasAntes;
                    if (diasAntes > 30)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "diasAntes debe estar entre 1 y 30.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("PrendarioEnviarAvisos");
                    try
                    {
                        var resumen = await envio
                            .EnviarPendientesAsync(body.OficinaId, diasAntes, body.CreditoId, "manual", ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(resumen);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al enviar avisos WhatsApp prendarios");
                        var detail = "No se pudieron enviar los avisos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioEnviarAvisosVencimiento")
            .WithSummary("Envía la plantilla aviso_vencimiento_prendario por WhatsApp Cloud API a quienes vencen en N días.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<PrendarioAvisoEnvioResumenDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    }

    static async Task<(T? Documento, ProblemHttpResult? Error)> ConsultarDocumentoPrendarioAsync<T>(
        HttpContext httpContext,
        int oficinaId,
        int creditoId,
        Func<int, int, CancellationToken, Task<PrendarioDocumentoConsulta<T>>> obtener,
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        string documento,
        CancellationToken ct)
        where T : class
    {
        if (oficinaId < 1 || creditoId < 1)
        {
            return (null, TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "oficinaId y creditoId deben ser >= 1."));
        }

        var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
        if (oficinaError is not null)
        {
            return (null, oficinaError);
        }

        var log = loggerFactory.CreateLogger("PrendarioDocumento");
        try
        {
            var resultado = await obtener(oficinaId, creditoId, ct).ConfigureAwait(false);
            return resultado.Estado switch
            {
                PrendarioDocumentoEstado.NoEncontrado => (null, TypedResults.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "No encontrado",
                    detail: "El crédito prendario no existe en esta oficina.")),
                PrendarioDocumentoEstado.SinBienes => (null, TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Bienes no registrados",
                    detail: "Guarde al menos un bien en custodia antes de imprimir.")),
                _ => (resultado.Documento, null),
            };
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Cadena de conexión no configurada");
            return (null, TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta"));
        }
        catch (DbException ex)
        {
            log.LogError(ex, "Error al generar el {Documento} prendario", documento);
            var detail = "No se pudo generar el documento.";
            if (env.IsDevelopment())
            {
                detail += $" Detalle: {ex.Message}";
            }

            return (null, TypedResults.Problem(
                detail: detail,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Error de base de datos"));
        }
    }

}
