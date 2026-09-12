using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Reportes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Reportes;

internal static class ReportesCatalogoEndpoints
{
    public static void MapReportesCatalogoEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/reportes/catalogo",
                async Task<Results<Ok<IReadOnlyList<ReporteCatalogoItemDto>>, ProblemHttpResult>> (
                    IReportesCatalogoReadService catalogo,
                    CancellationToken ct) =>
                {
                    var items = await catalogo.ListarAsync(ct).ConfigureAwait(false);
                    return TypedResults.Ok(items);
                })
            .WithName("ReportesCatalogo")
            .WithSummary(
                "Solo lectura: catálogo de acciones/pantallas de informes del MVC ReporteController (sin ejecutar RDLC). JWT CreditoUser.")
            .WithTags("reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<IReadOnlyList<ReporteCatalogoItemDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.MapGet(
                "/api/v1/reportes/politica-exportacion",
                async Task<Results<Ok<ReporteExportPoliticaDto>, ProblemHttpResult>> (
                    IReportesExportPoliticaReadService politica,
                    CancellationToken ct) =>
                {
                    var dto = await politica.ObtenerAsync(ct).ConfigureAwait(false);
                    return TypedResults.Ok(dto);
                })
            .WithName("ReportesExportPolitica")
            .WithSummary(
                "Solo lectura: política de sustitución CSV/JSON/RDLC (sin render ReportViewer). JWT CreditoUser.")
            .WithTags("reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ReporteExportPoliticaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.MapGet(
                "/api/v1/reportes/catalogo-cobertura",
                async Task<Results<Ok<ReporteCatalogoCoberturaDto>, ProblemHttpResult>> (
                    IReportesCatalogoCoberturaReadService cobertura,
                    CancellationToken ct) =>
                {
                    var dto = await cobertura.ObtenerAsync(ct).ConfigureAwait(false);
                    return TypedResults.Ok(dto);
                })
            .WithName("ReportesCatalogoCobertura")
            .WithSummary(
                "Solo lectura: matriz catálogo MVC (52) vs sustitutos API (JSON/CSV/PDF). JWT CreditoUser.")
            .WithTags("reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ReporteCatalogoCoberturaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
