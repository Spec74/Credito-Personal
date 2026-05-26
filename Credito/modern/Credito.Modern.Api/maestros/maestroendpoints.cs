using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.ListaPrecios;
using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.Marcas;
using Credito.Modern.Application.Modelos;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.TipoArticulos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Maestros;

internal static class MaestroEndpoints
{
    public static void MapMaestroCrudEndpoints(this WebApplication app)
    {
        MapMarca(app);
        MapModelo(app);
        MapTipoArticulo(app);
        MapOficina(app);
        MapAlmacen(app);
        MapListaPrecio(app);
    }

    private static void MapMarca(WebApplication app)
    {
        app.MapGet(
                "/api/v1/marcas/gestion",
                async Task<Results<Ok<List<MarcaListItemDto>>, ProblemHttpResult>> (
                    bool? incluirInactivos,
                    IMarcaReadService marcas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("MarcasGestion");
                    try
                    {
                        var items = await marcas
                            .ListAsync(incluirInactivos == true, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return MaestroReadError(log, env, ex, "marcas");
                    }
                })
            .WithName("MarcasGestion")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<MarcaListItemDto>>();

        app.MapPost(
                "/api/v1/marcas/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarMarcaRequest body,
                    IMarcaWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Denominacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion es obligatoria.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "GuardarMarca",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarMarca")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/marcas/{marcaId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int marcaId,
                    IMarcaWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (marcaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "marcaId debe ser >= 1.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "ActivarMarca",
                        ct,
                        token => write.ActivarAsync(marcaId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarMarca")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();
    }

    private static void MapModelo(WebApplication app)
    {
        app.MapGet(
                "/api/v1/modelos/gestion",
                async Task<Results<Ok<List<ModeloAdminListItemDto>>, ProblemHttpResult>> (
                    int? marcaId,
                    bool? incluirInactivos,
                    IModeloReadService modelos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ModelosGestion");
                    try
                    {
                        var items = await modelos
                            .ListAsync(marcaId, incluirInactivos == true, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return MaestroReadError(log, env, ex, "modelos");
                    }
                })
            .WithName("ModelosGestion")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ModeloAdminListItemDto>>();

        app.MapPost(
                "/api/v1/modelos/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarModeloRequest body,
                    IModeloWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Denominacion) || body.MarcaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion y marcaId (>=1) son obligatorios.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "GuardarModelo",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarModelo")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/modelos/{modeloId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int modeloId,
                    IModeloWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (modeloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "modeloId debe ser >= 1.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "ActivarModelo",
                        ct,
                        token => write.ActivarAsync(modeloId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarModelo")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();
    }

    private static void MapTipoArticulo(WebApplication app)
    {
        app.MapGet(
                "/api/v1/tipos-articulo/gestion",
                async Task<Results<Ok<List<TipoArticuloListItemDto>>, ProblemHttpResult>> (
                    bool? incluirInactivos,
                    ITipoArticuloReadService tipos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("TiposArticuloGestion");
                    try
                    {
                        var items = await tipos.ListAsync(incluirInactivos == true, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return MaestroReadError(log, env, ex, "tipos de artículo");
                    }
                })
            .WithName("TiposArticuloGestion")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<TipoArticuloListItemDto>>();

        app.MapPost(
                "/api/v1/tipos-articulo/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarTipoArticuloRequest body,
                    ITipoArticuloWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Denominacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion es obligatoria.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "GuardarTipoArticulo",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarTipoArticulo")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/tipos-articulo/{tipoArticuloId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int tipoArticuloId,
                    ITipoArticuloWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (tipoArticuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "tipoArticuloId debe ser >= 1.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "ActivarTipoArticulo",
                        ct,
                        token => write.ActivarAsync(tipoArticuloId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarTipoArticulo")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();
    }

    private static void MapOficina(WebApplication app)
    {
        app.MapGet(
                "/api/v1/oficinas/gestion",
                async Task<Results<Ok<List<OficinaAdminListItemDto>>, ProblemHttpResult>> (
                    bool? incluirInactivos,
                    string? buscar,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("OficinasGestion");
                    try
                    {
                        var items = await oficinas
                            .ListGestionAsync(incluirInactivos == true, buscar, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return MaestroReadError(log, env, ex, "oficinas");
                    }
                })
            .WithName("OficinasGestion")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<OficinaAdminListItemDto>>();

        app.MapPost(
                "/api/v1/oficinas/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarOficinaRequest body,
                    IOficinaWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Denominacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion es obligatoria.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "GuardarOficina",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarOficina")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/oficinas/{oficinaId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int oficinaId,
                    IOficinaWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "ActivarOficina",
                        ct,
                        token => write.ActivarAsync(oficinaId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarOficina")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();
    }

    private static void MapAlmacen(WebApplication app)
    {
        app.MapGet(
                "/api/v1/almacenes/gestion",
                async Task<Results<Ok<List<AlmacenAdminListItemDto>>, ProblemHttpResult>> (
                    int? oficinaId,
                    bool? incluirInactivos,
                    IAlmacenReadService almacenes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("AlmacenesGestion");
                    try
                    {
                        var items = await almacenes
                            .ListGestionAsync(oficinaId, incluirInactivos == true, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return MaestroReadError(log, env, ex, "almacenes");
                    }
                })
            .WithName("AlmacenesGestion")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<AlmacenAdminListItemDto>>();

        app.MapPost(
                "/api/v1/almacenes/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarAlmacenRequest body,
                    IAlmacenWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Denominacion) || body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion y oficinaId (>=1) son obligatorios.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "GuardarAlmacen",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarAlmacen")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/almacenes/{almacenId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int almacenId,
                    IAlmacenWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (almacenId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "almacenId debe ser >= 1.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "ActivarAlmacen",
                        ct,
                        token => write.ActivarAsync(almacenId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarAlmacen")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();
    }

    private static void MapListaPrecio(WebApplication app)
    {
        app.MapGet(
                "/api/v1/lista-precios/gestion",
                async Task<Results<Ok<List<ListaPrecioListItemDto>>, ProblemHttpResult>> (
                    int? articuloId,
                    bool? incluirInactivos,
                    IListaPrecioReadService lista,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ListaPreciosGestion");
                    try
                    {
                        var items = await lista.ListAsync(articuloId, incluirInactivos == true, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return MaestroReadError(log, env, ex, "lista de precios");
                    }
                })
            .WithName("ListaPreciosGestion")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ListaPrecioListItemDto>>();

        app.MapPost(
                "/api/v1/lista-precios/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarListaPrecioRequest body,
                    IListaPrecioWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.ArticuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "articuloId debe ser >= 1.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "GuardarListaPrecio",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarListaPrecio")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/lista-precios/{listaPrecioId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int listaPrecioId,
                    IListaPrecioWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (listaPrecioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "listaPrecioId debe ser >= 1.");
                    }

                    return await MaestroWriteAsync(
                        loggerFactory,
                        env,
                        "ActivarListaPrecio",
                        ct,
                        token => write.ActivarAsync(listaPrecioId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarListaPrecio")
            .WithTags("maestros")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();
    }

    private static ProblemHttpResult MaestroReadError(
        ILogger log,
        IHostEnvironment env,
        Exception ex,
        string catalogo)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Cadena de conexión no configurada");
            return TypedResults.Problem(
                detail: ioe.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Error al listar {Catalogo}", catalogo);
        var detail = $"No se pudo leer el catálogo de {catalogo}.";
        if (env.IsDevelopment())
            detail += $" Detalle: {ex.Message}";
        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }

    private static async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> MaestroWriteAsync(
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
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }
        catch (DbException ex)
        {
            log.LogError(ex, "Error en operación de maestro {Operation}", operation);
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
