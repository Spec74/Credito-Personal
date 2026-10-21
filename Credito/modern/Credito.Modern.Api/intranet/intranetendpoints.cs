using System.Data.Common;
using System.Security.Claims;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Intranet;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;

namespace Credito.Modern.Api.Intranet;

internal static class IntranetEndpoints
{
    public static void MapIntranetEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/comunicados", async (
            HttpContext http, bool vencidos, IIntranetService svc, ILoggerFactory log, IHostEnvironment env, CancellationToken ct) =>
        {
            if (!Ids(http, out var o, out var u, out var err)) return err!;
            try
            {
                return Results.Ok(await svc.ListarComunicadosAsync(o, u, vencidos, ct));
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbException)
            {
                return Fallo(log, env, ex, "No se pudo listar comunicados.");
            }
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet("/api/v1/comunicados/{id:int}", async (
            int id, HttpContext http, IIntranetService svc, ILoggerFactory log, IHostEnvironment env, CancellationToken ct) =>
        {
            if (!Ids(http, out var o, out var u, out var err)) return err!;
            try
            {
                var (cab, adj) = await svc.ObtenerComunicadoAsync(id, o, u, ct);
                if (cab is null) return Results.Problem(statusCode: 404, title: "No encontrado", detail: "Aviso no disponible.");
                return Results.Ok(new { cabecera = cab, adjuntos = adj });
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbException)
            {
                return Fallo(log, env, ex, "No se pudo abrir el aviso.");
            }
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost("/api/v1/comunicados", async (
            GuardarComunicadoRequest body, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out var o, out var u, out var err)) return err!;
            if (!PuedePublicar(http, out var admin))
                return Results.Problem(statusCode: 403, title: "Prohibido", detail: "Solo admin o encargado publica avisos.");
            var alcance = (body.Alcance ?? "").Trim().ToUpperInvariant();
            if (alcance == "RED" && !admin)
                return Results.Problem(statusCode: 403, title: "Prohibido", detail: "Solo el administrador publica a toda la red.");
            try
            {
                var id = await svc.GuardarComunicadoAsync(body with { Alcance = alcance }, o, u, ct);
                return Results.Ok(new IdResponse(id));
            }
            catch (SqlException ex)
            {
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: ex.Message);
            }
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost("/api/v1/comunicados/{id:int}/baja", async (
            int id, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out _, out var u, out var err)) return err!;
            if (!PuedePublicar(http, out _))
                return Results.Problem(statusCode: 403, title: "Prohibido", detail: "Solo admin o encargado retira avisos.");
            await svc.BajaComunicadoAsync(id, u, ct);
            return Results.Ok(new IdResponse(id));
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost("/api/v1/comunicados/{id:int}/leido", async (
            int id, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out _, out var u, out var err)) return err!;
            await svc.MarcarLeidoAsync(id, u, ct);
            return Results.Ok(new IdResponse(id));
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost("/api/v1/comunicados/{id:int}/adjunto", async (
            int id, HttpRequest req, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out _, out _, out var err)) return err!;
            if (!PuedePublicar(http, out _))
                return Results.Problem(statusCode: 403, title: "Prohibido", detail: "Solo admin o encargado adjunta.");
            if (!req.HasFormContentType)
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: "Use multipart/form-data.");
            var form = await req.ReadFormAsync(ct);
            var file = form.Files.GetFile("archivo");
            if (file is null || file.Length < 1)
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: "Seleccione un archivo.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var okImg = ext is ".jpg" or ".jpeg" or ".png" or ".webp";
            var okPdf = ext == ".pdf";
            if (!okImg && !okPdf)
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: "Solo imagen (jpg/png/webp) o PDF.");
            if (okImg && file.Length > 2 * 1024 * 1024)
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: "La imagen no puede superar 2 MB.");
            if (okPdf && file.Length > 8 * 1024 * 1024)
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: "El PDF no puede superar 8 MB.");
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var adjuntoId = await svc.SubirAdjuntoAsync(
                id,
                file.FileName,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                ms.ToArray(),
                ct);
            return Results.Ok(new IdResponse(adjuntoId));
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser).DisableAntiforgery();

        app.MapGet("/api/v1/comunicados/adjunto/{adjuntoId:int}", async (
            int adjuntoId, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out var o, out _, out var err)) return err!;
            var file = await svc.DescargarAdjuntoAsync(adjuntoId, o, ct);
            if (file is null)
                return Results.Problem(statusCode: 404, title: "No encontrado", detail: "Archivo no disponible.");
            return Results.File(file.Contenido, file.ContentType, file.NombreOriginal);
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet("/api/v1/agenda", async (
            DateTime desde, DateTime hasta, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out var o, out var u, out var err)) return err!;
            if (hasta <= desde)
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: "Rango de fechas inválido.");
            return Results.Ok(await svc.ListarAgendaAsync(o, u, desde, hasta, ct));
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost("/api/v1/agenda", async (
            GuardarAgendaRequest body, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out var o, out var u, out var err)) return err!;
            var alcance = (body.Alcance ?? "PERSONAL").Trim().ToUpperInvariant();
            if (alcance == "OFICINA" && !PuedePublicar(http, out _))
                return Results.Problem(statusCode: 403, title: "Prohibido", detail: "Solo admin o encargado crea agenda de sede.");
            try
            {
                var id = await svc.GuardarAgendaAsync(body with { Alcance = alcance }, o, u, ct);
                return Results.Ok(new IdResponse(id));
            }
            catch (SqlException ex)
            {
                return Results.Problem(statusCode: 400, title: "Solicitud inválida", detail: ex.Message);
            }
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost("/api/v1/agenda/{id:int}/baja", async (
            int id, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out _, out var u, out var err)) return err!;
            await svc.BajaAgendaAsync(id, u, ct);
            return Results.Ok(new IdResponse(id));
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost("/api/v1/agenda/{id:int}/descartar", async (
            int id, HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out _, out var u, out var err)) return err!;
            await svc.DescartarAgendaAsync(id, u, ct);
            return Results.Ok(new IdResponse(id));
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet("/api/v1/bandeja", async (
            HttpContext http, IIntranetService svc, CancellationToken ct) =>
        {
            if (!Ids(http, out var o, out var u, out var err)) return err!;
            return Results.Ok(await svc.BandejaAsync(o, u, ct));
        }).RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);
    }

    private static bool Ids(HttpContext http, out int oficinaId, out int usuarioId, out IResult? error)
    {
        oficinaId = 0;
        usuarioId = 0;
        if (!MenuIdentity.TryGetOficinaIdFromJwt(http.User, out oficinaId)
            || !MenuIdentity.TryGetUsuarioIdFromJwt(http.User, out usuarioId))
        {
            error = Results.Problem(statusCode: 403, title: "Prohibido", detail: "Sesión sin oficina o usuario.");
            return false;
        }

        error = null;
        return true;
    }

    private static bool PuedePublicar(HttpContext http, out bool admin)
    {
        var roles = http.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value.Trim().ToUpperInvariant())
            .ToList();
        admin = roles.Any(r => r is "ADMIN" or "ADMINISTRADOR" || r.Contains("ADMINISTRADOR"));
        var encargado = roles.Any(r => r.Contains("ENCARGADO"));
        return admin || encargado;
    }

    private static IResult Fallo(ILoggerFactory log, IHostEnvironment env, Exception ex, string titulo)
    {
        log.CreateLogger("Intranet").LogError(ex, "{Titulo}", titulo);
        return Results.Problem(
            statusCode: 503,
            title: "No disponible",
            detail: env.IsDevelopment() ? $"{titulo} {ex.Message}" : titulo);
    }
}
