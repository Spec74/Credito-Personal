using System.Data.Common;
using System.Net.Http.Headers;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Api.Reportes;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.Documentos;
using Credito.Modern.Application.Inventario;
using Credito.Modern.Application.ListaPrecios;
using Credito.Modern.Application.Marcas;
using Credito.Modern.Application.Menus;
using Credito.Modern.Application.Modelos;
using Credito.Modern.Application.Ocupaciones;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Productos;
using Credito.Modern.Application.SerieArticulos;
using Credito.Modern.Application.TipoArticulos;
using Credito.Modern.Application.TipoOperaciones;
using Credito.Modern.Application.Ubigeo;
using Credito.Modern.Application.ValorTablas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Maestros;

internal static class MaestroReadEndpoints
{
    public static void MapMaestroReadEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/menu", async Task<Results<Ok<List<MenuItemDto>>, ProblemHttpResult>> (
            HttpContext httpContext,
            int? oficinaId,
            int? usuarioId,
            IMenuReadService menuService,
            IOptions<MenuNavigationOptions> menuNavOpts,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Menu");
            if (AuthenticationHeaderValue.TryParse(httpContext.Request.Headers.Authorization.ToString(), out var authHeader)
                && string.Equals(authHeader.Scheme, JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(authHeader.Parameter)
                && httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "No autorizado",
                    detail: "Bearer JWT inválido o expirado.");
            }

            var allowQuery = menuNavOpts.Value.PermiteParametrosQuery;
            if (!MenuIdentity.TryResolve(httpContext.User, oficinaId, usuarioId, allowQuery, out var oid, out var uid))
            {
                var detail = allowQuery
                    ? "Indica oficinaId y usuarioId en la query (enteros > 0), o envía un Bearer JWT con claims vendix:oficina_id y vendix:usuario_id. En local (Development, sin CI, con Hosting:AllowDevToken) puedes obtener un token con POST /api/v1/dev/token."
                    : "Envía un Bearer JWT con claims vendix:oficina_id y vendix:usuario_id. Los parámetros oficinaId/usuarioId en query están deshabilitados (Menu:PermiteParametrosQuery=false).";
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                    detail: detail);
            }

            try
            {
                var items = await menuService.GetMenuAsync(oid, uid, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al ejecutar MAESTRO.usp_MenuLst");
                var detail =
                    "No se pudo obtener el menú desde SQL Server. Revisa conexión y que exista el procedimiento MAESTRO.usp_MenuLst.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("Menu")
        .WithTags("read-only")
        .Produces<List<MenuItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/oficinas", async Task<Results<Ok<List<OficinaListItemDto>>, ProblemHttpResult>> (
            IOficinaReadService oficinas,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Oficinas");
            try
            {
                var items = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar MAESTRO.Oficina");
                var detail = "No se pudo leer el catálogo de oficinas desde SQL Server.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("OficinasActivas")
        .WithTags("read-only")
        .Produces<List<OficinaListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/productos", async Task<Results<Ok<List<ProductoListItemDto>>, ProblemHttpResult>> (
            IProductoReadService productos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Productos");
            try
            {
                var items = await productos.GetActivosAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar CREDITO.Producto");
                var detail = "No se pudo leer el catálogo de productos de crédito.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("ProductosActivos")
        .WithTags("read-only")
        .Produces<List<ProductoListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/marcas", async Task<Results<Ok<List<MarcaListItemDto>>, ProblemHttpResult>> (
            IMarcaReadService marcas,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Marcas");
            try
            {
                var items = await marcas.GetActivasAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar ALMACEN.Marca");
                var detail = "No se pudo leer el catálogo de marcas.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("MarcasActivas")
        .WithTags("read-only")
        .Produces<List<MarcaListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/modelos", async Task<Results<Ok<List<ModeloListItemDto>>, ProblemHttpResult>> (
            int? marcaId,
            IModeloReadService modelos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Modelos");
            try
            {
                var items = await modelos.GetActivosAsync(marcaId, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar ALMACEN.Modelo");
                var detail = "No se pudo leer el catálogo de modelos.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("ModelosActivos")
        .WithTags("read-only")
        .WithSummary("Modelos activos; query opcional marcaId (>=1) filtra por ALMACEN.Modelo.MarcaId.")
        .Produces<List<ModeloListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/tipos-articulo", async Task<Results<Ok<List<TipoArticuloListItemDto>>, ProblemHttpResult>> (
            ITipoArticuloReadService tipos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("TipoArticulo");
            try
            {
                var items = await tipos.GetActivosAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar ALMACEN.TipoArticulo");
                var detail = "No se pudo leer el catálogo de tipos de artículo.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("TiposArticuloActivos")
        .WithTags("read-only")
        .Produces<List<TipoArticuloListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/tipo-operaciones", async Task<Results<Ok<List<TipoOperacionListItemDto>>, ProblemHttpResult>> (
            ITipoOperacionReadService tipos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("TipoOperacion");
            try
            {
                var items = await tipos.GetAllAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar MAESTRO.TipoOperacion");
                var detail = "No se pudo leer el catálogo de tipos de operación.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("TipoOperaciones")
        .WithTags("read-only")
        .Produces<List<TipoOperacionListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/tipos-documento", async Task<Results<Ok<List<TipoDocumentoListItemDto>>, ProblemHttpResult>> (
            bool? paraVenta,
            ITipoDocumentoReadService tipos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("TipoDocumento");
            var soloVenta = paraVenta == true;
            try
            {
                var items = await tipos.GetActivosAsync(soloVenta, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar MAESTRO.TipoDocumento");
                var detail = "No se pudo leer el catálogo de tipos de documento.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("TiposDocumento")
        .WithTags("read-only")
        .Produces<List<TipoDocumentoListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/tipos-documento-almacen-mov", async Task<Results<Ok<List<TipoDocumentoListItemDto>>, ProblemHttpResult>> (
            ITipoDocumentoReadService tipos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("TipoDocumentoAlmacenMov");
            try
            {
                var items = await tipos.GetActivosAlmacenMovAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar tipos documento almacén movimiento");
                var detail = "No se pudo leer tipos de documento para movimiento de almacén.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("TiposDocumentoAlmacenMov")
        .WithSummary("Paridad EntradaController ViewBag.cboTipoDocumento (IndAlmacen, sin IndAlmacenMov).")
        .WithTags("read-only", "almacen")
        .Produces<List<TipoDocumentoListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/departamentos", async Task<Results<Ok<List<DepartamentoListItemDto>>, ProblemHttpResult>> (
            IDepartamentoReadService departamentos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("UbigeoDepartamentos");
            try
            {
                var items = await departamentos.GetAllAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar MAESTRO.Departamento");
                var detail = "No se pudo leer el catálogo de departamentos.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("UbigeoDepartamentos")
        .WithTags("read-only")
        .Produces<List<DepartamentoListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/provincias", async Task<Results<Ok<List<ProvinciaListItemDto>>, ProblemHttpResult>> (
            int? departamentoId,
            IProvinciaReadService provincias,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("UbigeoProvincias");
            try
            {
                var items = await provincias.GetAsync(departamentoId, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar MAESTRO.Provincia");
                var detail = "No se pudo leer el catálogo de provincias.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("UbigeoProvincias")
        .WithTags("read-only")
        .WithSummary("Provincias; query opcional departamentoId (>=1) filtra por MAESTRO.Provincia.idDepa.")
        .Produces<List<ProvinciaListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/distritos", async Task<Results<Ok<List<DistritoListItemDto>>, ProblemHttpResult>> (
            int? provinciaId,
            IDistritoReadService distritos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("UbigeoDistritos");
            try
            {
                var items = await distritos.GetAsync(provinciaId, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar MAESTRO.Distrito");
                var detail = "No se pudo leer el catálogo de distritos.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("UbigeoDistritos")
        .WithTags("read-only")
        .WithSummary("Distritos; query opcional provinciaId (>=1) filtra por MAESTRO.Distrito.idProv.")
        .Produces<List<DistritoListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/tipos-movimiento-almacen", async Task<Results<Ok<List<TipoMovimientoAlmacenListItemDto>>, ProblemHttpResult>> (
            ITipoMovimientoAlmacenReadService tipos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("TipoMovimientoAlmacen");
            try
            {
                var items = await tipos.GetActivosAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar ALMACEN.TipoMovimiento");
                var detail = "No se pudo leer el catálogo de tipos de movimiento de almacén.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("TiposMovimientoAlmacenActivos")
        .WithTags("read-only")
        .WithSummary("Tipos de movimiento de almacén activos (ALMACEN.TipoMovimiento, Estado=1). Distinto de /api/v1/tipo-operaciones (crédito).")
        .Produces<List<TipoMovimientoAlmacenListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/almacenes", async Task<Results<Ok<List<AlmacenListItemDto>>, ProblemHttpResult>> (
            int? oficinaId,
            IAlmacenReadService almacenes,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Almacenes");
            try
            {
                var items = await almacenes.GetActivosAsync(oficinaId, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar ALMACEN.Almacen");
                var detail = "No se pudo leer el catálogo de almacenes.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("AlmacenesActivos")
        .WithTags("read-only")
        .WithSummary("Almacenes activos (esquema ALMACEN); query opcional oficinaId (>=1).")
        .Produces<List<AlmacenListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/reporte-stock",
                async Task<Results<Ok<List<ReporteStockRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IReporteStockReadService reportes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ReporteStock");
                    try
                    {
                        var items = await reportes.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "oficinaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_ReporteStock");
                        var detail = "No se pudo generar el reporte de stock.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenReporteStock")
            .WithSummary("Solo lectura: ALMACEN.usp_ReporteStock(OficinaId). oficinaId obligatorio y debe coincidir con vendix:oficina_id del JWT. CreditoUser.")
            .WithTags("read-only")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ReporteStockRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/reporte-stock-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IReporteStockReadService reportes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ReporteStockCsv");
                    try
                    {
                        var items = await reportes.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        var bytes = ReporteStockCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "reporte-stock.csv");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "oficinaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_ReporteStock (CSV)");
                        var detail = "No se pudo generar el CSV de reporte de stock.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenReporteStockCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET reporte-stock en CSV UTF-8 (BOM). ALMACEN.usp_ReporteStock sin motor RDLC. CreditoUser.")
            .WithTags("almacen", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/reporte-stock-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IReporteStockReadService reportes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ReporteStockPdf");
                    try
                    {
                        var items = await reportes.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        var csvBytes = ReporteStockCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf
                            .ResolveAsync(httpContext, oficinaId.Value, cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv(
                            "Reporte stock",
                            csvBytes,
                            context: pdfContext with { Titulo = "REPORTE DE STOCK" });
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "reporte-stock.pdf");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "oficinaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_ReporteStock (PDF)");
                        var detail = "No se pudo generar el PDF de reporte de stock.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenReporteStockPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET reporte-stock en PDF tabular (QuestPDF, columnas = CSV). ALMACEN.usp_ReporteStock sin layout RDLC. CreditoUser.")
            .WithTags("almacen", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/generar-kardex",
                async Task<Results<Ok<List<GenerarKardexRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? articuloId,
                    int? almacenId,
                    IGenerarKardexReadService generarKardex,
                    IAlmacenOficinaReadService almacenOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || articuloId is null or < 1 || almacenId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId, articuloId y almacenId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var almacenOid = await almacenOficina
                        .GetOficinaIdByAlmacenIdAsync(almacenId.Value, ct)
                        .ConfigureAwait(false);
                    if (almacenOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe un almacén con el almacenId indicado.");
                    }

                    if (almacenOid.Value != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El almacén no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("GenerarKardex");
                    try
                    {
                        var items = await generarKardex
                            .ListarAsync(articuloId.Value, almacenId.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_GenerarKardex");
                        var detail = "No se pudo generar el kardex.";
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
            .WithName("AlmacenGenerarKardex")
            .WithSummary(
                "Solo lectura: ALMACEN.usp_GenerarKardex(ArticuloId, AlmacenId). oficinaId/articuloId/almacenId obligatorios; oficinaId = vendix:oficina_id y el almacén debe pertenecer a esa oficina. CreditoUser. Equivale a AlmacenBL.GenerarKardex / KardexController.ListarKardex.")
            .WithTags("read-only")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<GenerarKardexRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/generar-kardex-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? articuloId,
                    int? almacenId,
                    IGenerarKardexReadService generarKardex,
                    IAlmacenOficinaReadService almacenOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || articuloId is null or < 1 || almacenId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId, articuloId y almacenId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var almacenOid = await almacenOficina
                        .GetOficinaIdByAlmacenIdAsync(almacenId.Value, ct)
                        .ConfigureAwait(false);
                    if (almacenOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe un almacén con el almacenId indicado.");
                    }

                    if (almacenOid.Value != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El almacén no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("GenerarKardexCsv");
                    try
                    {
                        var items = await generarKardex
                            .ListarAsync(articuloId.Value, almacenId.Value, ct)
                            .ConfigureAwait(false);
                        var bytes = GenerarKardexCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "generar-kardex.csv");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_GenerarKardex (CSV)");
                        var detail = "No se pudo generar el CSV de kardex.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenGenerarKardexCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET generar-kardex en CSV UTF-8 (BOM). ALMACEN.usp_GenerarKardex sin motor RDLC. CreditoUser.")
            .WithTags("almacen", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/generar-kardex-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? articuloId,
                    int? almacenId,
                    IGenerarKardexReadService generarKardex,
                    IAlmacenOficinaReadService almacenOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || articuloId is null or < 1 || almacenId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId, articuloId y almacenId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var almacenOid = await almacenOficina
                        .GetOficinaIdByAlmacenIdAsync(almacenId.Value, ct)
                        .ConfigureAwait(false);
                    if (almacenOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe un almacén con el almacenId indicado.");
                    }

                    if (almacenOid.Value != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El almacén no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("GenerarKardexPdf");
                    try
                    {
                        var items = await generarKardex
                            .ListarAsync(articuloId.Value, almacenId.Value, ct)
                            .ConfigureAwait(false);
                        var csvBytes = GenerarKardexCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf
                            .ResolveAsync(httpContext, oficinaId.Value, cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv(
                            "Kardex",
                            csvBytes,
                            context: pdfContext with
                            {
                                Titulo = "KARDEX",
                                Referencia = $"Artículo #{articuloId.Value} · Almacén #{almacenId.Value}",
                            });
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "generar-kardex.pdf");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_GenerarKardex (PDF)");
                        var detail = "No se pudo generar el PDF de kardex.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenGenerarKardexPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET generar-kardex en PDF tabular (QuestPDF, columnas = CSV). ALMACEN.usp_GenerarKardex sin layout RDLC. CreditoUser.")
            .WithTags("almacen", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/serie-kardex",
                async Task<Results<Ok<ListarSerieKardexResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? movimientoDetalleId,
                    bool? indStock,
                    IListarSerieKardexReadService listarSerieKardex,
                    IMovimientoDetOficinaReadService movimientoDetOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || movimientoDetalleId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId y movimientoDetalleId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ListarSerieKardex");
                    try
                    {
                        var movimientoOficinaId = await movimientoDetOficina
                            .GetOficinaIdByMovimientoDetIdAsync(movimientoDetalleId.Value, ct)
                            .ConfigureAwait(false);
                        if (movimientoOficinaId != oficinaId.Value)
                        {
                            return TypedResults.Problem(
                                statusCode: movimientoOficinaId is null
                                    ? StatusCodes.Status404NotFound
                                    : StatusCodes.Status403Forbidden,
                                title: movimientoOficinaId is null ? "No encontrado" : "Prohibido",
                                detail: movimientoOficinaId is null
                                    ? "No existe el detalle de movimiento solicitado."
                                    : "El detalle de movimiento no pertenece a la oficina del token JWT.");
                        }

                        var response = await listarSerieKardex
                            .ObtenerPrimeraFilaAsync(movimientoDetalleId.Value, indStock == true, ct)
                            .ConfigureAwait(false);
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
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "movimientoDetalleId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_ListarSerieKardex");
                        var detail = "No se pudo listar la serie del kardex.";
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
            .WithName("AlmacenSerieKardex")
            .WithSummary(
                "Solo lectura: ALMACEN.usp_ListarSerieKardex(MovimientoDetalleId, IndStock) con validación previa de oficina por MovimientoDetId.")
            .WithTags("read-only")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ListarSerieKardexResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/almacen/existe-serie-articulo",
                async Task<Results<Ok<ExisteSerieArticuloResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    string? listaSerie,
                    int? cantidad,
                    bool? indCorrelativo,
                    IExisteSerieArticuloReadService existeSerieArticulo,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (string.IsNullOrWhiteSpace(listaSerie))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "listaSerie es obligatoria.");
                    }

                    if (cantidad is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "cantidad, si se indica, debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ExisteSerieArticulo");
                    try
                    {
                        var response = await existeSerieArticulo
                            .ValidarAsync(listaSerie, cantidad, indCorrelativo == true, ct)
                            .ConfigureAwait(false);
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
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "listaSerie o cantidad inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_ExisteSerieArticulo");
                        var detail = "No se pudo validar la serie del artículo.";
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
            .WithName("AlmacenExisteSerieArticulo")
            .WithSummary(
                "Solo lectura: ALMACEN.usp_ExisteSerieArticulo(ListaSerie, Cantidad, IndCorrelativo). listaSerie obligatoria; cantidad opcional; indCorrelativo opcional (default false). oficinaId = vendix:oficina_id (el proc no filtra por oficina). CreditoUser. Equivale a SerieArticuloBL.ValidarExisteSerie / EntradaController.ValidarExisteSerie.")
            .WithTags("read-only")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ExisteSerieArticuloResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/ocupaciones", async Task<Results<Ok<List<OcupacionListItemDto>>, ProblemHttpResult>> (
            IOcupacionReadService ocupaciones,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Ocupaciones");
            try
            {
                var items = await ocupaciones.GetActivasAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar MAESTRO.Ocupacion");
                var detail = "No se pudo leer el catálogo de ocupaciones / actividad económica.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("OcupacionesActivas")
        .WithTags("read-only")
        .Produces<List<OcupacionListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/articulos", async Task<Results<Ok<List<ArticuloListItemDto>>, ProblemHttpResult>> (
            int? modeloId,
            int? tipoArticuloId,
            IArticuloReadService articulos,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("Articulos");
            try
            {
                var items = await articulos.GetActivosAsync(modeloId, tipoArticuloId, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar ALMACEN.Articulo");
                var detail = "No se pudo leer el catálogo de artículos.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("ArticulosActivos")
        .WithTags("read-only")
        .WithSummary("Artículos activos (ALMACEN.Articulo); queries opcionales modeloId y tipoArticuloId (>=1). Sin columna Imagen.")
        .Produces<List<ArticuloListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/lista-precios", async Task<Results<Ok<List<ListaPrecioListItemDto>>, ProblemHttpResult>> (
            int? articuloId,
            IListaPrecioReadService listaPrecios,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("ListaPrecio");
            try
            {
                var items = await listaPrecios.GetActivosAsync(articuloId, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
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
                log.LogError(ex, "Error al listar VENTAS.ListaPrecio");
                var detail = "No se pudo leer la lista de precios.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("ListaPreciosActivos")
        .WithTags("read-only")
        .WithSummary("Lista de precios activa (VENTAS.ListaPrecio); query opcional articuloId (>=1).")
        .Produces<List<ListaPrecioListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/valores-tabla", async Task<Results<Ok<List<ValorTablaListItemDto>>, ProblemHttpResult>> (
            int? tablaId,
            bool? soloItemIdPositivo,
            IValorTablaReadService valores,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (tablaId is null or < 1)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                    detail: "tablaId es obligatorio y debe ser un entero >= 1.");
            }

            var soloPositivo = soloItemIdPositivo != false;
            var log = loggerFactory.CreateLogger("ValorTabla");
            try
            {
                var items = await valores.GetByTablaIdAsync(tablaId.Value, soloPositivo, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
            }
            catch (InvalidOperationException ex)
            {
                log.LogWarning(ex, "Cadena de conexión no configurada");
                return TypedResults.Problem(
                    detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Configuración incompleta");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                log.LogWarning(ex, "tablaId inválido");
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
            }
            catch (DbException ex)
            {
                log.LogError(ex, "Error al listar MAESTRO.ValorTabla");
                var detail = "No se pudo leer la tabla genérica de valores.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("ValoresTablaPorTablaId")
        .WithTags("read-only")
        .WithSummary("Valores MAESTRO.ValorTabla; tablaId obligatorio (>=1). Query soloItemIdPositivo (default true) excluye ItemId <= 0.")
        .Produces<List<ValorTablaListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/series-articulo", async Task<Results<Ok<List<SerieArticuloListItemDto>>, ProblemHttpResult>> (
            int? almacenId,
            int? articuloId,
            int? estadoId,
            int? limite,
            ISerieArticuloReadService series,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (almacenId is null or < 1 || articuloId is null or < 1)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                    detail: "almacenId y articuloId son obligatorios y deben ser enteros >= 1.");
            }

            var estado = estadoId ?? 2;
            if (estado < 1)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                    detail: "estadoId, si se envía, debe ser >= 1 (por defecto 2 = EN_ALMACEN).");
            }

            var take = limite switch
            {
                null => 200,
                < 1 => 1,
                > 500 => 500,
                int v => v,
            };

            var log = loggerFactory.CreateLogger("SerieArticulo");
            try
            {
                var items = await series.ListarPorAlmacenArticuloAsync(almacenId.Value, articuloId.Value, estado, take, ct).ConfigureAwait(false);
                return TypedResults.Ok(items);
            }
            catch (InvalidOperationException ex)
            {
                log.LogWarning(ex, "Cadena de conexión no configurada");
                return TypedResults.Problem(
                    detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Configuración incompleta");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                log.LogWarning(ex, "Parámetros de series inválidos");
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
            }
            catch (DbException ex)
            {
                log.LogError(ex, "Error al listar ALMACEN.SerieArticulo");
                var detail = "No se pudo leer el listado de series.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("SeriesArticuloPorAlmacenArticulo")
        .WithTags("read-only")
        .WithSummary("Series en almacén (ALMACEN.SerieArticulo). almacenId y articuloId obligatorios. estadoId por defecto 2 (EN_ALMACEN). limite 1-500 (default 200).")
        .Produces<List<SerieArticuloListItemDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    }
}
