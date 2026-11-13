using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Validation;
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
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Productos;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Validation;
using Credito.Modern.Application.Ventas;
using Credito.Modern.Infrastructure.CreditoPlanes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

/// <summary>Ficha crédito: contexto, evidencias, prendas, avales y gestiones.</summary>
internal static class CreditoFichaGestionEndpoints
{
    public static void MapCreditoFichaGestionEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/credito-contexto",
                async Task<Results<Ok<CreditoContextoDto>, ProblemHttpResult>> (
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoContexto");
                    try
                    {
                        var ctx = await gestionRead.ObtenerContextoAsync(creditoId, ct).ConfigureAwait(false);
                        if (ctx is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Cr?dito no encontrado.");
                        }

                        return TypedResults.Ok(ctx);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al obtener contexto de cr?dito");
                        var detail = "No se pudo obtener el contexto.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoContexto")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditoContextoDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/solicitud-credito",
                async Task<Results<Ok<SolicitudCreditoDetalleDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int solicitudCreditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, solicitudCreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, solicitudCreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("SolicitudCreditoDetalle");
                    try
                    {
                        var solicitud = await gestionRead
                            .ObtenerSolicitudCreditoAsync(solicitudCreditoId, ct)
                            .ConfigureAwait(false);
                        if (solicitud is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Solicitud de cr?dito CRE no encontrada.");
                        }

                        return TypedResults.Ok(solicitud);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al obtener solicitud de cr?dito");
                        var detail = "No se pudo obtener la solicitud de cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSolicitudDetalle")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .WithSummary("Solo lectura: solicitud CRE para continuar originaci?n desde Cr?dito > Cr?ditos.")
            .Produces<SolicitudCreditoDetalleDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/prendas",
                async Task<Results<Ok<IReadOnlyList<PrendaDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("Prenda");
                    try
                    {
                        var prendas = await gestionRead.ListarPrendasAsync(creditoId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(prendas);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar prendas del cr?dito");
                        var detail = "No se pudieron obtener los bienes del cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendasListar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<IReadOnlyList<PrendaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cargos-credito",
                async Task<Results<Ok<List<CargoCreditoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("CargosCredito");
                    try
                    {
                        var items = await gestionRead.ListarCargosAsync(creditoId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar cargos");
                        var detail = "No se pudo listar cargos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCargosListar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CargoCreditoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/evidencias-credito",
                async Task<Results<Ok<List<CreditoEvidenciaDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("EvidenciasCredito");
                    try
                    {
                        var items = await gestionRead.ListarEvidenciasAsync(creditoId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar evidencias");
                        var detail = "No se pudo listar evidencias.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEvidenciasListar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CreditoEvidenciaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/evidencia-archivo/{creditoImagenId:int}",
                async Task<Results<PhysicalFileHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int creditoImagenId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    IOptions<CreditoStorageOptions> storageOptions,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoImagenId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "creditoImagenId inv?lido.");
                    }

                    var log = loggerFactory.CreateLogger("EvidenciaArchivo");
                    try
                    {
                        var meta = await gestionRead
                            .ObtenerEvidenciaArchivoAsync(creditoImagenId, ct)
                            .ConfigureAwait(false);
                        if (meta is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Evidencia no encontrada.");
                        }

                        if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "El token no contiene una oficina v?lida.");
                        }

                        var scopeError = await CajaCreditoWriteGuards
                            .ValidateCreditoOficinaAsync(jwtOficinaId, meta.Value.CreditoId, creditoOficina, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                            return scopeError;

                        var root = CreditoGestionWriteService.ResolveStorageRoot(storageOptions.Value);
                        var path = Path.Combine(root, meta.Value.CreditoId.ToString(), meta.Value.FileName);
                        if (!File.Exists(path))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Archivo no existe en almacenamiento.");
                        }

                        var ext = Path.GetExtension(path).ToLowerInvariant();
                        var contentType = ext switch
                        {
                            ".png" => "image/png",
                            ".gif" => "image/gif",
                            ".webp" => "image/webp",
                            ".bmp" => "image/bmp",
                            _ => "image/jpeg",
                        };
                        return TypedResults.PhysicalFile(path, contentType);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al obtener evidencia");
                        var detail = "No se pudo obtener la evidencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEvidenciaArchivo")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/persona-credito-ficha",
                async Task<Results<Ok<PersonaCreditoFichaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("PersonaCreditoFicha");
                    try
                    {
                        var ficha = await gestionRead
                            .ObtenerPersonaCreditoFichaAsync(oficinaId, personaId, ct)
                            .ConfigureAwait(false);
                        if (ficha is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Cliente o persona no encontrados.");
                        }

                        return TypedResults.Ok(ficha);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al obtener ficha persona cr?dito");
                        var detail = "No se pudo obtener la ficha del cliente.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PersonaCreditoFicha")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .WithSummary("Cabecera cliente en consulta cr?dito (paridad Creditos.cshtml con pPersonaId).")
            .Produces<PersonaCreditoFichaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/creditos-grilla-persona",
                async Task<Results<Ok<CreditoGrillaPersonaPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    bool grupoActivo,
                    int page,
                    int pageSize,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("CreditosGrillaPersona");
                    try
                    {
                        var response = await gestionRead
                            .ListarCreditosGrillaPersonaAsync(
                                oficinaId,
                                personaId,
                                grupoActivo,
                                page < 1 ? 1 : page,
                                pageSize < 1 ? 25 : pageSize,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar cr?ditos por persona");
                        var detail = "No se pudo listar cr?ditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoGrillaPersona")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditoGrillaPersonaPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/avales-persona",
                async Task<Results<Ok<IReadOnlyList<CreditoAvalRelacionDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("AvalesPersona");
                    try
                    {
                        var response = await gestionRead
                            .ListarAvalesPersonaAsync(oficinaId, personaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar avales por persona");
                        var detail = "No se pudo listar avales y avalados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAvalesPersona")
            .WithSummary("Solo lectura: relaciones reales de aval y avalado para la ficha moderna de cr?dito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<IReadOnlyList<CreditoAvalRelacionDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/condonar-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CondonarCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("CondonarCredito");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .CondonarAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "No condonado",
                                detail: response.Mensaje ?? "Error al condonar.");
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al condonar cr?dito");
                        var detail = "No se pudo condonar el cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCondonar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolSoloAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/observar-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ObservarCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(CreditoProductoValidacion.ValidarObservacion(body.Observacion));
                    if (edgeError is not null)
                        return edgeError;

                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ObservarCredito");
                    try
                    {
                        var response = await gestionWrite.ObservarAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al observar cr?dito");
                        var detail = "No se pudo guardar la observaci?n.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoObservar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/guardar-cargo-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarCargoCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(CreditoProductoValidacion.ValidarGuardarCargo(body));
                    if (edgeError is not null)
                        return edgeError;

                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("GuardarCargoCredito");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .GuardarCargoAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Cargo no guardado",
                                detail: response.Mensaje ?? "Error al guardar cargo.");
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al guardar cargo");
                        var detail = "No se pudo guardar el cargo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoGuardarCargo")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/subir-evidencia-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    HttpRequest httpRequest,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!httpRequest.HasFormContentType)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "Se requiere multipart/form-data.");
                    }

                    var form = await httpRequest.ReadFormAsync(ct).ConfigureAwait(false);
                    if (!int.TryParse(form["oficinaId"], out var oficinaId) || oficinaId < 1
                        || !int.TryParse(form["creditoId"], out var creditoId) || creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y creditoId son obligatorios.");
                    }

                    var file = form.Files.GetFile("imagen");
                    if (file is null || file.Length < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "Seleccione una imagen (campo imagen).");
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp"))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "Formato de imagen no permitido.");
                    }

                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("SubirEvidenciaCredito");
                    try
                    {
                        await using var stream = file.OpenReadStream();
                        var response = await gestionWrite
                            .SubirEvidenciaAsync(oficinaId, creditoId, ext, stream, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "No subida",
                                detail: response.Mensaje ?? "Error al subir evidencia.");
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al subir evidencia");
                        var detail = "No se pudo subir la evidencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSubirEvidencia")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .DisableAntiforgery()
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/eliminar-evidencia-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarEvidenciaRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.CreditoImagenId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y creditoImagenId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("EliminarEvidenciaCredito");
                    try
                    {
                        var response = await gestionWrite
                            .EliminarEvidenciaAsync(body.OficinaId, body.CreditoImagenId, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al eliminar evidencia");
                        var detail = "No se pudo eliminar la evidencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEliminarEvidencia")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cambiar-analista-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CambiarAnalistaCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("CambiarAnalistaCredito");
                    try
                    {
                        var response = await gestionWrite.CambiarAnalistaAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al cambiar analista");
                        var detail = "No se pudo cambiar el analista.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCambiarAnalista")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-tope-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarTopeCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y personaId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("ActualizarTopeCredito");
                    try
                    {
                        var response = await gestionWrite.ActualizarTopeAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al actualizar tope");
                        var detail = "No se pudo actualizar el tope.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarTope")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/depurar-persona-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DepurarPersonaCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y personaId son obligatorios.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Observacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "observacion (motivo de depuración) es obligatoria.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;

                    var log = loggerFactory.CreateLogger("DepurarPersonaCredito");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .DepurarPersonaAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo depurar",
                                detail: response.Mensaje ?? "No se pudo depurar la persona.");
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al depurar persona");
                        var detail = "No se pudo depurar la persona.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoDepurarPersona")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-irrecuperable-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarIrrecuperableRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ActualizarIrrecuperable");
                    try
                    {
                        var response = await gestionWrite.ActualizarIrrecuperableAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al actualizar irrecuperable");
                        var detail = "No se pudo actualizar el indicador.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarIrrecuperable")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/modificar-tramite-adm-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ModificarTramiteAdmCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ModificarTramiteAdmCredito");
                    try
                    {
                        var response = await gestionWrite.ModificarTramiteAdmAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al modificar tr?mite administrativo");
                        var detail = "No se pudo modificar el tr?mite administrativo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoModificarTramiteAdm")
            .WithSummary("Paridad CreditoController.ModificarTramiteAdmCredito (MontoGastosAdm).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/modificar-central-riesgo-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ModificarCentralRiesgoCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ModificarCentralRiesgoCredito");
                    try
                    {
                        var response = await gestionWrite.ModificarCentralRiesgoAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al modificar central de riesgo");
                        var detail = "No se pudo modificar la central de riesgo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoModificarCentralRiesgo")
            .WithSummary("Paridad CreditoController.ModificarCentralRieagoCredito (CentralRiesgo).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-descuento-plan-pago",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarDescuentoPlanPagoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.PlanPagoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "planPagoId es obligatorio.");
                    }

                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ActualizarDescuentoPlanPago");
                    try
                    {
                        var response = await gestionWrite
                            .ActualizarDescuentoPlanPagoAsync(body, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al actualizar descuento del plan de pagos");
                        var detail = "No se pudo actualizar el descuento.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarDescuentoPlanPago")
            .WithSummary("Paridad CreditoController.ActualizarDescuentoPlanPago.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolSoloAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/guardar-prendas",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarPrendasRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("GuardarPrendas");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .GuardarPrendasAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Bienes no guardados",
                                detail: response.Mensaje ?? "Error al guardar los bienes.");
                        }

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
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Validación de bienes prendarios");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Datos inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "Los bienes enviados no son válidos."
                                : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al guardar los bienes del crédito prendario");
                        var detail = "No se pudieron guardar los bienes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendasGuardar")
            .WithSummary("Modern: primera carga de bienes en custodia de un crédito prendario.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-aval-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarAvalCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ActualizarAvalCredito");
                    try
                    {
                        var response = await gestionWrite.ActualizarAvalAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al actualizar aval del cr?dito");
                        var detail = "No se pudo actualizar el aval.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarAval")
            .WithSummary("Paridad CreditoController.ActualizarAvalCredito (PersonaAvalId).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/creditos-por-persona",
                async Task<Results<Ok<List<CreditoPorPersonaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? personaId,
                    bool? esCajaCentral,
                    ICreditosPorPersonaReadService creditosPersona,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "personaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene un usuario v?lido (vendix:usuario_id).");
                    }

                    var log = loggerFactory.CreateLogger("CreditosPorPersona");
                    try
                    {
                        var items = await creditosPersona
                            .ListarDesembolsadosPorPersonaAsync(
                                personaId.Value,
                                usuarioId,
                                esCajaCentral == true,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar cr?ditos por persona");
                        var detail = "No se pudo listar los cr?ditos.";
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
            .WithName("CreditoCreditosPorPersona")
            .WithSummary(
                "Cr?ditos estado DES por persona (paridad ListarCreditosPendientesCombo). esCajaCentral omite filtro UsuarioRegId.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CreditoPorPersonaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
