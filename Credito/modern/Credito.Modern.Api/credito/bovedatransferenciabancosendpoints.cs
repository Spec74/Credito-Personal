using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Validation;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Validation;
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
                    var edgeError = ProblemResults.IfInvalid(
                        TesoreriaValidacion.ValidarTransferenciaBancos(
                            body.OficinaId,
                            body.TipoPagoOrigenId,
                            body.TipoPagoDestinoId,
                            body.Importe,
                            body.Glosa));
                    if (edgeError is not null)
                        return edgeError;

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
                                body.Glosa.Trim(),
                                usuarioId,
                                ct)
                            .ConfigureAwait(false);

                        return TypedResults.Ok(result);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parametros invalidos en transferencia entre bancos");
                        return ProblemResults.FromArgument(ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Transferencia entre bancos rechazada");
                        return ProblemResults.Conflict(ex.Message, "Operacion no permitida");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error en transferencia entre bancos de boveda");
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
                "Paridad BovedaController.RegistrarTransferenciaBancos - usp_RegistrarTransferenciaBancos. Recalcula saldos tras el SP.")
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
