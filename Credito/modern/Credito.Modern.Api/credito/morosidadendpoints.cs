using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;

namespace Credito.Modern.Api.Credito;

internal static class MorosidadEndpoints
{
    public static void MapMorosidadEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/morosidad/permisos",
                Results<Ok<MorosidadPermisosResponse>, ProblemHttpResult> (
                    HttpContext httpContext,
                    IMorosidadAccessService access) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    return TypedResults.Ok(new MorosidadPermisosResponse(
                        access.PuedeConsultar(usuarioId, roles)));
                })
            .WithName("MorosidadPermisos")
            .WithTags("morosidad")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MorosidadPermisosResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapGet(
                "/api/v1/morosidad/empresa",
                async Task<Results<Ok<MorosidadEmpresaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? tipo,
                    int? oficinaId,
                    int? gestorId,
                    DateTime? fechaCorte,
                    IMorosidadAccessService access,
                    IMorosidadReadService read,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    if (!access.PuedeConsultar(usuarioId, roles))
                    {
                        return ForbiddenConsulta();
                    }

                    var log = loggerFactory.CreateLogger("MorosidadEmpresa");
                    try
                    {
                        var filas = await read.ObtenerMorososEmpresaAsync(
                                tipo ?? "TODOS",
                                oficinaId,
                                gestorId,
                                fechaCorte,
                                ct)
                            .ConfigureAwait(false);

                        var fechaIso = filas.Count > 0
                            ? filas[0].FechaCorte.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : (fechaCorte ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                        var resumen = new MorosidadResumenDto(
                            filas.Count,
                            filas.Sum(x => x.CreditosMora),
                            filas.Sum(x => x.SaldoMora),
                            filas.Count(x => x.CodigoClasificacion == "NUNCA_PAGO"),
                            filas.Count(x => x.CodigoClasificacion == "DEJO_PAGAR"),
                            filas.Count(x => x.CodigoClasificacion == "PAGA_CON_ATRASO"));

                        return TypedResults.Ok(new MorosidadEmpresaResponse(
                            true,
                            fechaIso,
                            resumen,
                            filas));
                    }
                    catch (SqlException ex)
                    {
                        log.LogError(ex, "Error SQL al obtener morosidad empresarial");
                        return SqlConflict(ex, env);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error DB al obtener morosidad empresarial");
                        return DbUnavailable(ex, env);
                    }
                })
            .WithName("MorosidadEmpresa")
            .WithSummary("Paridad MorosidadController.Obtener → CREDITO.usp_MorosidadEmpresa.")
            .WithTags("morosidad")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MorosidadEmpresaResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/morosidad/excel",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? fechaInicio,
                    string? fechaFin,
                    IMorosidadAccessService access,
                    IMorosidadReadService read,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    if (!access.PuedeConsultar(usuarioId, roles))
                    {
                        return ForbiddenConsulta();
                    }

                    if (!TryParseFecha(fechaInicio, out var inicio) ||
                        !TryParseFecha(fechaFin, out var fin))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Debe indicar fechas válidas para el inicio y el final.");
                    }

                    inicio = inicio.Date;
                    fin = fin.Date;
                    if (inicio > fin)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La fecha de inicio no puede ser posterior a la fecha final.");
                    }

                    var log = loggerFactory.CreateLogger("MorosidadExcel");
                    try
                    {
                        // El SP conserva la clasificación oficial; el rango solo acota
                        // la primera cuota vencida mostrada en el archivo (paridad legado).
                        var filas = (await read.ObtenerMorososEmpresaAsync(
                                    "TODOS",
                                    null,
                                    null,
                                    fin,
                                    ct)
                                .ConfigureAwait(false))
                            .Where(x =>
                                x.PrimeraCuotaVencida.HasValue &&
                                x.PrimeraCuotaVencida.Value.Date >= inicio &&
                                x.PrimeraCuotaVencida.Value.Date <= fin)
                            .OrderBy(x => OrdenSemaforo(x.CodigoClasificacion))
                            .ThenByDescending(x => x.DiasAtraso)
                            .ThenByDescending(x => x.SaldoMora)
                            .ThenBy(x => x.NombreCompleto, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var bytes = MorosidadXlsxFormatter.ToXlsx(filas, inicio, fin);
                        var nombre = string.Create(
                            CultureInfo.InvariantCulture,
                            $"MOROSIDAD_EMPRESA_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.xlsx");

                        return TypedResults.File(
                            bytes,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            nombre);
                    }
                    catch (SqlException ex)
                    {
                        log.LogError(ex, "Error SQL al exportar Excel de morosidad");
                        return SqlConflict(ex, env);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error DB al exportar Excel de morosidad");
                        return DbUnavailable(ex, env);
                    }
                })
            .WithName("MorosidadExcel")
            .WithSummary("Paridad MorosidadController.ExportarExcel (ClosedXML).")
            .WithTags("morosidad")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static bool TryUsuarioYRoles(
        HttpContext httpContext,
        out int usuarioId,
        out List<string> roles,
        out ProblemHttpResult? problem)
    {
        roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out usuarioId))
        {
            problem = TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sesión inválida",
                detail: "La sesión del usuario ha expirado.");
            return false;
        }

        problem = null;
        return true;
    }

    private static ProblemHttpResult ForbiddenConsulta() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Sin permiso",
            detail: "Su usuario no está autorizado para consultar la morosidad empresarial.");

    private static ProblemHttpResult SqlConflict(SqlException ex, IHostEnvironment env)
    {
        // Paridad legado: mensajes de negocio 50001–50099.
        var detail = ex.Number is >= 50001 and <= 50099
            ? ex.Message
            : "La base de datos no pudo procesar la consulta de morosidad.";
        if (env.IsDevelopment() && ex.Number is < 50001 or > 50099)
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflicto de base de datos",
            detail: detail);
    }

    private static ProblemHttpResult DbUnavailable(DbException ex, IHostEnvironment env)
    {
        var detail = "No se pudo cargar la morosidad empresarial.";
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos",
            detail: detail);
    }

    private static int OrdenSemaforo(string codigo) =>
        codigo switch
        {
            "NUNCA_PAGO" => 1,
            "DEJO_PAGAR" => 2,
            _ => 3,
        };

    private static bool TryParseFecha(string? valor, out DateTime fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var formatos = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" };
        return DateTime.TryParseExact(
            valor.Trim(),
            formatos,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out fecha);
    }
}
