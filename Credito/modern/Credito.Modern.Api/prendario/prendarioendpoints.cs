using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Api.Validation;
using Credito.Modern.Application.Validation;
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
using Credito.Modern.Infrastructure.CreditoPlanes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
                    string? categoria = null,
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
                        PrendarioCategoria? filtro = null;
                        if (!string.IsNullOrWhiteSpace(categoria)
                            && Enum.TryParse<PrendarioCategoria>(categoria, ignoreCase: true, out var parsed))
                        {
                            filtro = parsed;
                        }

                        var pagina = await prendarioRead
                            .ListarAsync(oficinaId, buscar, page, pageSize, filtro, ct)
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos al crear solicitud prendaria");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Datos inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "Los parámetros enviados no son válidos."
                                : ex.Message);
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



        app.MapPost(
                "/api/v1/prendario/devolver-prenda",
                async Task<Results<Ok<DevolverPrendaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DevolverPrendaRequest body,
                    IPrendarioWriteService prendarioWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PrendaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y prendaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("PrendarioDevolver");
                    try
                    {
                        var response = await prendarioWrite
                            .DevolverAsync(body, usuarioId, ct)
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
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        var negocio = MensajeNegocioDevolucion(ex);
                        if (negocio is { } n)
                        {
                            log.LogWarning(ex, "Devolución rechazada por contrato");
                            return TypedResults.Problem(
                                statusCode: n.NotFound ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict,
                                title: n.NotFound ? "No encontrado" : "No se puede devolver",
                                detail: n.Detail);
                        }

                        log.LogError(ex, "Error al ejecutar CREDITO.usp_DevolverPrenda");
                        var detail = "No se pudo devolver el bien.";
                        if (env.IsDevelopment())
                        {
                            detail += $" Detalle: {ex.Message}";
                        }

                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioDevolverPrenda")
            .WithSummary("Escritura: CREDITO.usp_DevolverPrenda. EN CUSTODIA → DEVUELTO (no DES).")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<DevolverPrendaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/prendario/devolucion-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    long prendaId,
                    IPrendarioReadService prendarioRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || prendaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y prendaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("PrendarioDevolucionPdf");
                    try
                    {
                        var consulta = await prendarioRead
                            .ObtenerDevolucionAsync(oficinaId, prendaId, ct)
                            .ConfigureAwait(false);
                        return consulta.Estado switch
                        {
                            PrendarioDocumentoEstado.NoEncontrado => TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "El bien no existe en esta oficina."),
                            PrendarioDocumentoEstado.SinBienes => TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Aún en custodia",
                                detail: "Devuelva el bien antes de imprimir el comprobante."),
                            _ => TypedResults.File(
                                RptDevolucionPrendarioPdfDocument.Build(consulta.Documento!),
                                "application/pdf",
                                $"DevolucionPrendario_{consulta.Documento!.PrendaId}.pdf"),
                        };
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
                        log.LogError(ex, "Error al generar el comprobante de devolución");
                        var detail = "No se pudo generar el documento.";
                        if (env.IsDevelopment())
                        {
                            detail += $" Detalle: {ex.Message}";
                        }

                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioDevolucionPdf")
            .WithSummary("Comprobante de devolución (QuestPDF). Exige Prenda.Estado = DEVUELTO.")
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
            .WithSummary("Créditos prendarios desembolsados que vencen en N días. Acotado a la oficina.")
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

        app.MapGet(
                "/api/v1/prendario/cobro-preview",
                async Task<Results<Ok<PrendarioCobroPreviewDto>, ProblemHttpResult>> (
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

                    try
                    {
                        var preview = await prendarioRead
                            .ObtenerCobroPreviewAsync(oficinaId, creditoId, ct)
                            .ConfigureAwait(false);
                        if (preview is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Crédito prendario no encontrado en esta sede.");
                        }

                        return TypedResults.Ok(preview);
                    }
                    catch (InvalidOperationException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioCobroPreview").LogWarning(ex, "Configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioCobroPreview").LogError(ex, "Error al leer preview prendario");
                        var detail = "No se pudo obtener el preview de cobro.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioCobroPreview")
            .WithSummary("Importes de PlanPago (interés/capital) para renovar, abonar o liberar. No inventa tasa.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario);

        app.MapPost(
                "/api/v1/prendario/actualizar-ficha",
                async Task<Results<Ok<PrendarioFichaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PrendarioFichaRequest body,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var r = await EjecutarEscrituraPrendarioAsync(
                        httpContext,
                        body.OficinaId,
                        (svc, usuarioId) => svc.ActualizarFichaAsync(body, usuarioId, ct),
                        loggerFactory,
                        env,
                        "PrendarioFicha",
                        "No se pudo guardar la ficha.",
                        ct).ConfigureAwait(false);
                    return r;
                })
            .WithName("PrendarioActualizarFicha")
            .WithSummary("Actualiza Observaciones (cómo llegó) sin tocar tasación.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario);

        app.MapPost(
                "/api/v1/prendario/prenda-foto",
                async Task<Results<Ok<PrendarioFichaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IPrendarioWriteService prendarioWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!int.TryParse(httpContext.Request.Form["oficinaId"], out var oficinaId)
                        || !long.TryParse(httpContext.Request.Form["prendaId"], out var prendaId)
                        || oficinaId < 1
                        || prendaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y prendaId son obligatorios.");
                    }

                    var archivo = httpContext.Request.Form.Files.GetFile("archivo")
                        ?? httpContext.Request.Form.Files.FirstOrDefault();
                    if (archivo is null || archivo.Length < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Adjunte una foto.");
                    }

                    if (archivo.Length > 5 * 1024 * 1024)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Archivo demasiado grande",
                            detail: "La foto no puede superar 5 MB.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;

                    var ext = Path.GetExtension(archivo.FileName);
                    if (string.IsNullOrWhiteSpace(ext))
                        ext = ".jpg";
                    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ".jpg", ".jpeg", ".png", ".webp",
                    };
                    if (!allowed.Contains(ext))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Formato no permitido",
                            detail: "Use JPG, PNG o WEBP.");
                    }

                    try
                    {
                        await using var stream = archivo.OpenReadStream();
                        var response = await prendarioWrite
                            .SubirFotoAsync(oficinaId, prendaId, usuarioId, ext, stream, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("no encontrado", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioFoto").LogWarning(ex, "Configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioFoto").LogError(ex, "Error al subir foto de prenda");
                        var detail = "No se pudo guardar la foto.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioSubirFoto")
            .WithSummary("Sube FotoPath de CREDITO.Prenda al storage de evidencias.")
            .WithTags("prendario")
            .DisableAntiforgery()
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Accepts<IFormFile>("multipart/form-data");

        app.MapGet(
                "/api/v1/prendario/prenda-foto/{prendaId:long}",
                async Task<Results<PhysicalFileHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    long prendaId,
                    int oficinaId,
                    IPrendarioReadService prendarioRead,
                    IOptions<CreditoStorageOptions> storageOptions,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || prendaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y prendaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    try
                    {
                        var meta = await prendarioRead
                            .ObtenerFotoArchivoAsync(oficinaId, prendaId, ct)
                            .ConfigureAwait(false);
                        if (meta is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Esta prenda no tiene foto.");
                        }

                        var root = CreditoGestionWriteService.ResolveStorageRoot(storageOptions.Value);
                        var path = Path.GetFullPath(Path.Combine(root, meta.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                        if (!path.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)
                            || !File.Exists(path))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "El archivo de la foto no está en el servidor.");
                        }

                        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
                        {
                            ".png" => "image/png",
                            ".webp" => "image/webp",
                            _ => "image/jpeg",
                        };
                        return TypedResults.PhysicalFile(path, contentType);
                    }
                    catch (InvalidOperationException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioFoto").LogWarning(ex, "Configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioFoto").LogError(ex, "Error al leer foto de prenda");
                        var detail = "No se pudo leer la foto.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioObtenerFoto")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario);

        app.MapPost(
                "/api/v1/prendario/renovar",
                async Task<Results<Ok<PrendarioRenovarResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PrendarioRenovarRequest body,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                    await EjecutarEscrituraPrendarioAsync(
                        httpContext,
                        body.OficinaId,
                        (svc, usuarioId) => svc.RenovarAsync(body, usuarioId, ct),
                        loggerFactory,
                        env,
                        "PrendarioRenovar",
                        "No se pudo renovar el préstamo.",
                        ct).ConfigureAwait(false))
            .WithName("PrendarioRenovar")
            .WithSummary("Cobra interés de PlanPago + usp_ProrrogarCredito. No copia el 5 % del demo.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario);

        app.MapPost(
                "/api/v1/prendario/abonar",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotaPagoLibreRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                        return idError;

                    var moneyError = ProblemResults.IfInvalid(
                        MoneyRules.RequirePositive(body.ImporteRecibido, "importeRecibido"));
                    if (moneyError is not null)
                        return moneyError;

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario está cerrada.");
                    }

                    try
                    {
                        var response = await cajaPago
                            .PagarCuotaPagoLibreAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                body.ImporteRecibido,
                                usuarioId,
                                body.TipoPagoId < 1 ? 1 : body.TipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                            return pagoError;
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioAbonar").LogWarning(ex, "Configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        loggerFactory.CreateLogger("PrendarioAbonar").LogError(ex, "Error usp_PagarCuotaPagoLibre");
                        var detail = "No se pudo registrar el abono.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendarioAbonar")
            .WithSummary("Abono a capital: CREDITO.usp_PagarCuotaPagoLibre (mismo motor de caja).")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario);

        app.MapPost(
                "/api/v1/prendario/liberar",
                async Task<Results<Ok<PrendarioLiberarResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PrendarioLiberarRequest body,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                    await EjecutarEscrituraPrendarioAsync(
                        httpContext,
                        body.OficinaId,
                        (svc, usuarioId) => svc.LiberarAsync(body, usuarioId, ct),
                        loggerFactory,
                        env,
                        "PrendarioLiberar",
                        "No se pudo liberar la prenda.",
                        ct).ConfigureAwait(false))
            .WithName("PrendarioLiberar")
            .WithSummary("usp_PagarCuotasCancelacion + prendas DEVUELTO.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario);

        app.MapPost(
                "/api/v1/prendario/rematar",
                async Task<Results<Ok<RematarPrendaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RematarPrendaRequest body,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                    await EjecutarEscrituraPrendarioAsync(
                        httpContext,
                        body.OficinaId,
                        (svc, usuarioId) => svc.RematarAsync(body, usuarioId, ct),
                        loggerFactory,
                        env,
                        "PrendarioRematar",
                        "No se pudo rematar el bien.",
                        ct).ConfigureAwait(false))
            .WithName("PrendarioRematar")
            .WithSummary("Pasa la prenda a stock (artículo + lista precio + serie EN_ALMACEN) y cierra como rematado.")
            .WithTags("prendario")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario);

    static async Task<Results<Ok<T>, ProblemHttpResult>> EjecutarEscrituraPrendarioAsync<T>(
        HttpContext httpContext,
        int oficinaId,
        Func<IPrendarioWriteService, int, Task<T>> exec,
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        string logName,
        string failDetail,
        CancellationToken ct)
    {
        _ = ct;
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

        var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
        if (usuarioError is not null)
            return usuarioError;

        var log = loggerFactory.CreateLogger(logName);
        try
        {
            var write = httpContext.RequestServices.GetRequiredService<IPrendarioWriteService>();
            var response = await exec(write, usuarioId).ConfigureAwait(false);
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Configuración incompleta");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
        }
        catch (DbException ex)
        {
            var negocio = MensajeNegocioCiclo(ex);
            if (negocio is { } n)
            {
                log.LogWarning(ex, "Operación prendaria rechazada por contrato");
                return TypedResults.Problem(
                    statusCode: n.NotFound ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict,
                    title: n.NotFound ? "No encontrado" : "No se puede completar",
                    detail: n.Detail);
            }

            log.LogError(ex, "Error en {Operacion}", logName);
            var detail = failDetail;
            if (env.IsDevelopment())
                detail += $" Detalle: {ex.Message}";
            return TypedResults.Problem(
                detail: detail,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Error de base de datos");
        }
    }

    static (string Detail, bool NotFound)? MensajeNegocioCiclo(DbException ex)
    {
        var msg = (ex.Message ?? string.Empty).Split('\n')[0].Trim();
        if (msg.Contains("no encontrado", StringComparison.OrdinalIgnoreCase))
            return (msg, true);
        if (msg.Contains("caja", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("custodia", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("remat", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("almacén", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("almacen", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("cuotas pendientes", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("incompletos", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("ficha", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("liber", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("renov", StringComparison.OrdinalIgnoreCase))
        {
            return (msg, false);
        }

        return MensajeNegocioDevolucion(ex);
    }

    static (string Detail, bool NotFound)? MensajeNegocioDevolucion(DbException ex)
    {
        var msg = (ex.Message ?? string.Empty).Split('\n')[0].Trim();
        if (msg.Contains("no encontrado", StringComparison.OrdinalIgnoreCase))
        {
            return (msg, true);
        }

        if (msg.Contains("devuelto", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("custodia", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("desembolsado", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("incompletos", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("actualizó", StringComparison.OrdinalIgnoreCase))
        {
            return (msg, false);
        }

        return null;
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

}
