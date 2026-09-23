using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

/// <summary>Paridad MVC <c>CreditoController.CobrarPlanillaBloque</c> / pantalla CobroBloque.</summary>
internal static class CobroPlanillaBloqueEndpoints
{
    public static void MapCobroPlanillaBloqueEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/api/v1/credito/cobrar-planilla-bloque",
                async Task<Results<Ok<CobrarPlanillaBloqueResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CobrarPlanillaBloqueRequest body,
                    ICobroPlanillaBloqueWriteService planillaWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.CajaDiarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y cajaDiarioId deben ser >= 1.");
                    }

                    if (body.Planilla is null || body.Planilla.Count == 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La planilla enviada está vacía.");
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

                    var log = loggerFactory.CreateLogger("CobrarPlanillaBloque");
                    try
                    {
                        var cajaOficina = await cajaDiarioOficina
                            .GetOficinaIdByCajaDiarioIdAsync(body.CajaDiarioId, ct)
                            .ConfigureAwait(false);
                        if (cajaOficina is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Caja no disponible",
                                detail: "La caja diario no existe.");
                        }

                        if (cajaOficina.Value != body.OficinaId)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Oficina no autorizada",
                                detail: "La caja diario no pertenece a la oficina del token.");
                        }

                        var result = await planillaWrite
                            .EjecutarAsync(body.CajaDiarioId, usuarioId, body.Planilla, ct)
                            .ConfigureAwait(false);

                        if (!result.Exito)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Planilla no procesada",
                                detail: result.Mensaje);
                        }

                        return TypedResults.Ok(result);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos en planilla");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros de la planilla no son válidos.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al cobrar planilla en bloque");
                        var detail = "Proceso abortado por seguridad: error de base de datos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCobrarPlanillaBloque")
            .WithSummary(
                "Escritura atómica: paridad CreditoController.CobrarPlanillaBloque (usp_PagarCuotaPagoLibre × N + usp_CompletarImpagos).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CobrarPlanillaBloqueResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
