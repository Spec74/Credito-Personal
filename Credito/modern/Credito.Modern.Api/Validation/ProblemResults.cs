using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Validation;

/// <summary>
/// ProblemDetails RFC 7807 para errores de validación/negocio en Minimal APIs.
/// Convención: reenviar siempre el mensaje de ArgumentException al cliente.
/// </summary>
internal static class ProblemResults
{
    public static ProblemHttpResult BadRequest(string detail, string title = "Datos inválidos") =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: title,
            detail: detail);

    public static ProblemHttpResult Conflict(string detail, string title = "Conflicto") =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: title,
            detail: detail);

    public static ProblemHttpResult NotFound(string detail, string title = "No encontrado") =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: title,
            detail: detail);

    /// <summary>Si <paramref name="error"/> no es null, devuelve 400; si no, null (continuar).</summary>
    public static ProblemHttpResult? IfInvalid(string? error, string title = "Datos inválidos") =>
        string.IsNullOrWhiteSpace(error) ? null : BadRequest(error, title);

    public static ProblemHttpResult FromArgument(
        Exception ex,
        string fallback = "Los parámetros enviados no son válidos.",
        string title = "Datos inválidos")
    {
        var detail = string.IsNullOrWhiteSpace(ex.Message) ? fallback : ex.Message.Trim();
        // ArgumentException a veces incluye "Parameter name: x" — limpiar ruido de framework.
        var paramMarker = " (Parameter '";
        var idx = detail.IndexOf(paramMarker, StringComparison.Ordinal);
        if (idx > 0)
        {
            detail = detail[..idx].Trim();
        }

        return BadRequest(string.IsNullOrWhiteSpace(detail) ? fallback : detail, title);
    }
}
