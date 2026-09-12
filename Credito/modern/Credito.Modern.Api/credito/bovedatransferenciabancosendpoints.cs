using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

internal static class BovedaTransferenciaBancosEndpoints
{
    public static void MapBovedaTransferenciaBancosEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/api/v1/credito/transferir-boveda-bancos",
                async Task<Results<Ok<TransferirBovedaBancosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaBancosRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.TipoPagoOrigenId < 1 || body.TipoPagoDestinoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y tipos de pago deben ser >= 1.");
                    }

                    if (body.TipoPagoOrigenId == body.TipoPagoDestinoId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "El banco de origen y el de destino no pueden ser iguales.");
                    }

                    if (body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "El importe debe ser mayor a cero.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Glosa))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "glosa es obligatoria.");
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

                    var log = loggerFactory.CreateLogger("TransferirBovedaBancos");
                    try
                    {
                        var result = await bovedaMovWrite
                            .TransferirEntreBancosAsync(
                                body.OficinaId,
                                body.TipoPagoOrigenId,
                                body.TipoPagoDestinoId,
                                body.Importe,
                                body.Glosa,
                                usuarioId,
                                ct)
                            .ConfigureAwait(false);

                        return TypedResults.Ok(result);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos en transferencia entre bancos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "Los parámetros enviados no son válidos."
                                : ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Transferencia entre bancos rechazada");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Operación no permitida",
                            detail: ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error en transferencia entre bancos de bóveda");
                        var detail = "No se pudo registrar la transferencia entre bancos.";
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
            .WithName("CreditoTransferirBovedaBancos")
            .WithSummary(
                "Paridad BovedaController.RegistrarTransferenciaBancos → usp_RegistrarTransferenciaBancos. Recalcula saldos tras el SP.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TransferirBovedaBancosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
