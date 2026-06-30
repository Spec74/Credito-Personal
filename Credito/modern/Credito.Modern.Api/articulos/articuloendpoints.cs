using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.Maestros;
using Credito.Modern.Infrastructure.Articulos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Articulos;

internal static class ArticuloEndpoints
{
    public static void MapArticuloCrudEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/articulos/gestion",
                async Task<Results<Ok<List<ArticuloGestionListItemDto>>, ProblemHttpResult>> (
                    int? modeloId,
                    int? tipoArticuloId,
                    bool? incluirInactivos,
                    IArticuloReadService articulos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ArticulosGestion");
                    try
                    {
                        var items = await articulos
                            .ListGestionAsync(modeloId, tipoArticuloId, incluirInactivos == true, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "artículos");
                    }
                })
            .WithName("ArticulosGestion")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ArticuloGestionListItemDto>>();

        app.MapGet(
                "/api/v1/articulos/{articuloId:int}/detalle",
                async Task<Results<Ok<ArticuloDetalleDto>, ProblemHttpResult>> (
                    int articuloId,
                    IArticuloReadService articulos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (articuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "articuloId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("ArticuloDetalle");
                    try
                    {
                        var detalle = await articulos.GetDetalleAsync(articuloId, ct).ConfigureAwait(false);
                        if (detalle is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Artículo no encontrado.");
                        }

                        return TypedResults.Ok(detalle);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "artículo");
                    }
                })
            .WithName("ArticuloDetalle")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ArticuloDetalleDto>();

        app.MapGet(
                "/api/v1/articulos/buscar",
                async Task<Results<Ok<List<ArticuloBuscarItemDto>>, ProblemHttpResult>> (
                    string? term,
                    bool? soloActivos,
                    IArticuloReadService articulos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ArticulosBuscar");
                    try
                    {
                        var items = await articulos
                            .BuscarSelectAsync(term ?? string.Empty, soloActivos != false, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex, "búsqueda de artículos");
                    }
                })
            .WithName("ArticulosBuscar")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ArticuloBuscarItemDto>>();

        app.MapPost(
                "/api/v1/articulos/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarArticuloRequest body,
                    IArticuloWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.ModeloId < 1 || body.TipoArticuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "modeloId y tipoArticuloId deben ser >= 1.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Denominacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion es obligatoria.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "GuardarArticulo",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarArticulo")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();

        app.MapGet(
                "/api/v1/articulos/{articuloId:int}/imagenes",
                async Task<Results<Ok<ArticuloImagenesResponse>, ProblemHttpResult>> (
                    int articuloId,
                    IArticuloReadService articulos,
                    CancellationToken ct) =>
                {
                    if (articuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "articuloId debe ser >= 1.");
                    }

                    var csv = await articulos.ObtenerImagenCsvAsync(articuloId, ct).ConfigureAwait(false);
                    if (csv is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "Artículo no encontrado.");
                    }

                    var archivos = string.IsNullOrWhiteSpace(csv)
                        ? Array.Empty<string>()
                        : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    return TypedResults.Ok(new ArticuloImagenesResponse(archivos));
                })
            .WithName("ArticuloImagenes")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ArticuloImagenesResponse>();

        app.MapGet(
                "/api/v1/articulos/{articuloId:int}/imagen/{nombreArchivo}",
                async Task<Results<PhysicalFileHttpResult, ProblemHttpResult>> (
                    int articuloId,
                    string nombreArchivo,
                    IArticuloReadService articulos,
                    IOptions<ArticuloStorageOptions> storageOptions,
                    CancellationToken ct) =>
                {
                    if (articuloId < 1
                        || string.IsNullOrWhiteSpace(nombreArchivo)
                        || nombreArchivo.Contains('/', StringComparison.Ordinal)
                        || nombreArchivo.Contains('\\', StringComparison.Ordinal)
                        || nombreArchivo.Contains("..", StringComparison.Ordinal))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Parámetros de imagen inválidos.");
                    }

                    var csv = await articulos.ObtenerImagenCsvAsync(articuloId, ct).ConfigureAwait(false);
                    if (csv is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "Artículo no encontrado.");
                    }

                    var permitidos = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (!permitidos.Contains(nombreArchivo, StringComparer.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "Imagen no asociada al artículo.");
                    }

                    var root = ArticuloWriteService.ResolveImgRoot(storageOptions.Value);
                    var path = Path.Combine(root, nombreArchivo);
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
                })
            .WithName("ArticuloImagenArchivo")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost(
                "/api/v1/articulos/{articuloId:int}/imagen",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int articuloId,
                    HttpRequest httpRequest,
                    IArticuloWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (articuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "articuloId debe ser >= 1.");
                    }

                    if (!httpRequest.HasFormContentType)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Se requiere multipart/form-data.");
                    }

                    var form = await httpRequest.ReadFormAsync(ct).ConfigureAwait(false);
                    var file = form.Files.GetFile("archivo") ?? form.Files.GetFile("imagen");
                    if (file is null || file.Length < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Seleccione un archivo (campo archivo o imagen).");
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp"))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Formato de imagen no permitido.");
                    }

                    await using var stream = file.OpenReadStream();
                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "SubirImagenArticulo",
                        ct,
                        token => write.SubirImagenAsync(articuloId, stream, file.FileName, token))
                        .ConfigureAwait(false);
                })
            .WithName("SubirImagenArticulo")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/articulos/{articuloId:int}/eliminar-imagen",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int articuloId,
                    string nombreArchivo,
                    IArticuloWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (articuloId < 1 || string.IsNullOrWhiteSpace(nombreArchivo))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "articuloId y nombreArchivo son obligatorios.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "EliminarImagenArticulo",
                        ct,
                        token => write.EliminarImagenAsync(articuloId, nombreArchivo, token)).ConfigureAwait(false);
                })
            .WithName("EliminarImagenArticulo")
            .WithTags("articulos")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();
    }

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex, string catalogo)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Cadena de conexión no configurada");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Error al leer {Catalogo}", catalogo);
        var detail = $"No se pudo leer {catalogo}.";
        if (env.IsDevelopment())
            detail += $" Detalle: {ex.Message}";
        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }

    private static async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> WriteAsync(
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        string operation,
        CancellationToken ct,
        Func<CancellationToken, Task<MaestroOperacionResponse>> action)
    {
        var log = loggerFactory.CreateLogger(operation);
        try
        {
            var response = await action(ct).ConfigureAwait(false);
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
        catch (DbException ex)
        {
            log.LogError(ex, "Error en {Operation}", operation);
            var detail = "No se pudo completar la operación.";
            if (env.IsDevelopment())
                detail += $" Detalle: {ex.Message}";
            return TypedResults.Problem(
                detail: detail,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Error de base de datos");
        }
    }
}
