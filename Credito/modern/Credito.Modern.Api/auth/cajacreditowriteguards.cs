using Credito.Modern.Application.CreditoPlanes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Auth;

internal static class CajaCreditoWriteGuards
{
    internal static ProblemHttpResult? ValidateBodyCajaIds(int oficinaId, int cajaDiarioId)
    {
        if (oficinaId < 1 || cajaDiarioId < 1)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "oficinaId y cajaDiarioId deben ser enteros >= 1.");
        }

        return null;
    }

    internal static ProblemHttpResult? ValidateBodyMovimientoIds(int oficinaId, int movimientoCajaId)
    {
        if (oficinaId < 1 || movimientoCajaId < 1)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "oficinaId y movimientoCajaId deben ser enteros >= 1.");
        }

        return null;
    }

    internal static async Task<(ProblemHttpResult? Error, MovimientoCajaScopeDto? Scope)> ValidateMovimientoOficinaAsync(
        int oficinaId,
        int movimientoCajaId,
        IMovimientoCajaScopeReadService movimientoScope,
        CancellationToken cancellationToken)
    {
        var scope = await movimientoScope
            .GetScopeAsync(movimientoCajaId, cancellationToken)
            .ConfigureAwait(false);
        if (scope is null)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "No existe un movimiento de caja con el movimientoCajaId indicado."),
                null);
        }

        if (scope.OficinaId != oficinaId)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "El movimiento de caja no pertenece a la oficina del token JWT."),
                null);
        }

        return (null, scope);
    }

    internal static ProblemHttpResult? ValidateMovimientoAnulable(MovimientoCajaScopeDto scope)
    {
        if (!scope.EstadoActivo)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto",
                detail: "El movimiento ya está anulado.");
        }

        if (scope.CajaDiarioCerrada)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto",
                detail: "La caja diario está cerrada; no se puede anular el movimiento.");
        }

        return null;
    }
    internal static async Task<ProblemHttpResult?> ValidateCajaOficinaAsync(
        int oficinaId,
        int cajaDiarioId,
        ICajaDiarioOficinaReadService cajaDiarioOficina,
        CancellationToken cancellationToken)
    {
        var cajaOid = await cajaDiarioOficina
            .GetOficinaIdByCajaDiarioIdAsync(cajaDiarioId, cancellationToken)
            .ConfigureAwait(false);
        if (cajaOid is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "No existe una caja diario con el cajaDiarioId indicado.");
        }

        if (cajaOid.Value != oficinaId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "La caja diario no pertenece a la oficina del token JWT.");
        }

        return null;
    }
    internal static ProblemHttpResult? ValidateBodyIds(int oficinaId, int cajaDiarioId, int creditoId)
    {
        if (oficinaId < 1 || cajaDiarioId < 1 || creditoId < 1)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "oficinaId, cajaDiarioId y creditoId deben ser enteros >= 1.");
        }

        return null;
    }

    internal static ProblemHttpResult? ValidateBodyCreditoIds(int oficinaId, int creditoId)
    {
        if (oficinaId < 1 || creditoId < 1)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "oficinaId y creditoId deben ser enteros >= 1.");
        }

        return null;
    }

    internal static async Task<ProblemHttpResult?> ValidateCreditoOficinaAsync(
        int oficinaId,
        int creditoId,
        ICreditoOficinaReadService creditoOficina,
        CancellationToken cancellationToken)
    {
        var creditoOid = await creditoOficina
            .GetOficinaIdByCreditoIdAsync(creditoId, cancellationToken)
            .ConfigureAwait(false);
        if (creditoOid is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "No existe un crédito con el creditoId indicado.");
        }

        if (creditoOid.Value != oficinaId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "El crédito no pertenece a la oficina del token JWT.");
        }

        return null;
    }
    internal static ProblemHttpResult? ValidateJwtOficina(HttpContext httpContext, int oficinaId)
    {
        if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "El token no contiene una oficina válida (vendix:oficina_id).");
        }

        if (jwtOficinaId != oficinaId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "oficinaId debe coincidir con la oficina del token JWT.");
        }

        return null;
    }

    internal static ProblemHttpResult? ValidateJwtUsuario(HttpContext httpContext, out int usuarioId)
    {
        if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out usuarioId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "El token no contiene un usuario válido (vendix:usuario_id).");
        }

        return null;
    }

    internal static async Task<ProblemHttpResult?> ValidateCajaYCreditoOficinaAsync(
        int oficinaId,
        int cajaDiarioId,
        int creditoId,
        ICajaDiarioOficinaReadService cajaDiarioOficina,
        ICreditoOficinaReadService creditoOficina,
        CancellationToken cancellationToken)
    {
        var cajaOid = await cajaDiarioOficina
            .GetOficinaIdByCajaDiarioIdAsync(cajaDiarioId, cancellationToken)
            .ConfigureAwait(false);
        if (cajaOid is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "No existe una caja diario con el cajaDiarioId indicado.");
        }

        if (cajaOid.Value != oficinaId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "La caja diario no pertenece a la oficina del token JWT.");
        }

        var creditoOid = await creditoOficina
            .GetOficinaIdByCreditoIdAsync(creditoId, cancellationToken)
            .ConfigureAwait(false);
        if (creditoOid is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "No existe un crédito con el creditoId indicado.");
        }

        if (creditoOid.Value != oficinaId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "El crédito no pertenece a la oficina del token JWT.");
        }

        return null;
    }

    internal static async Task<ProblemHttpResult?> ValidateEntradaSalidaPrecondicionesAsync(
        int cajaDiarioId,
        int tipoOperacionId,
        decimal importe,
        string? descripcion,
        int tipoPagoId,
        IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "descripcion es obligatoria.");
        }

        if (importe <= 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "importe debe ser > 0.");
        }

        if (tipoPagoId < 1)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "tipoPagoId debe ser >= 1.");
        }

        if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(cajaDiarioId, cancellationToken).ConfigureAwait(false))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto",
                detail: "La caja diario está cerrada.");
        }

        var esEntrada = await entradaSalidaRead
            .GetTipoOperacionEsEntradaAsync(tipoOperacionId, cancellationToken)
            .ConfigureAwait(false);
        if (esEntrada is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No encontrado",
                detail: "No existe el tipoOperacionId indicado.");
        }

        if (esEntrada == false)
        {
            var saldo = await entradaSalidaRead
                .GetSaldoFinalCajaDiarioAsync(cajaDiarioId, cancellationToken)
                .ConfigureAwait(false);
            if (saldo is null)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "No encontrado",
                    detail: "No existe la caja diario indicada.");
            }

            if (importe > saldo.Value)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflicto",
                    detail: "Saldo Insuficiente!");
            }
        }

        return null;
    }

    internal static ProblemHttpResult? MapEntradaSalidaResult(EntradaSalidaCajaDiarioResponse response)
    {
        if (response.ResultCode < 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Operación rechazada",
                detail: "El procedimiento de entrada/salida no completó la operación (resultado negativo).");
        }

        return null;
    }
    internal static ProblemHttpResult? MapPagoResult(PagoCajaResultResponse response)
    {
        if (response.ResultId is null or < 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Pago rechazado",
                detail: "El procedimiento de pago no completó la operación (resultado negativo o nulo).");
        }

        return null;
    }
}
