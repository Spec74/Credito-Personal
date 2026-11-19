using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Validation;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.Auth;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.CreditoCartera;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.CreditoTareas;
using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Application.Dashboard;
using Credito.Modern.Application.Documentos;
using Credito.Modern.Application.Integraciones;
using Credito.Modern.Application.Inventario;
using Credito.Modern.Application.ListaPrecios;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Productos;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Validation;
using Credito.Modern.Application.Ventas;
using Credito.Modern.Infrastructure.CreditoPlanes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

/// <summary>Caja diario — sesión y lecturas de cabecera.</summary>
internal static partial class CreditoCajaDiarioEndpoints
{
    private static void MapCajaDiarioSesionCore(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/usuarios-no-asignados-caja",
                async Task<Results<Ok<List<UsuariosNoAsignadosCajaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IUsuariosNoAsignadosCajaReadService usuariosNoAsignados,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina v?lida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("UsuariosNoAsignadosCaja");
                    try
                    {
                        var items = await usuariosNoAsignados.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "oficinaId inv?lido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_UsuariosNoAsignadosCaja");
                        var detail = "No se pudo listar usuarios sin caja asignada.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoUsuariosNoAsignadosCaja")
            .WithSummary(
                "Solo lectura: CREDITO.usp_UsuariosNoAsignadosCaja(OficinaId). oficinaId obligatorio y debe coincidir con vendix:oficina_id del JWT. CreditoUser. Equivale a CajaBL.ListaUsuariosNoAsignado.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<UsuariosNoAsignadosCajaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/obtener-saldo-cuenta-caja-diario",
                async Task<Results<Ok<ObtenerSaldoCuentaCajaDiarioResponse>, ProblemHttpResult>> (
                    int? cajaDiarioId,
                    int? tipoPagoId,
                    IObtenerSaldoCuentaCajaDiarioReadService saldoCuenta,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "cajaDiarioId es obligatorio y debe ser un entero >= 1.");
                    }

                    var tipoPago = tipoPagoId ?? 1;
                    if (tipoPago < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "tipoPagoId debe ser un entero >= 1 cuando se indica.");
                    }

                    var log = loggerFactory.CreateLogger("ObtenerSaldoCuentaCajaDiario");
                    try
                    {
                        var valor = await saldoCuenta.ObtenerAsync(cajaDiarioId.Value, tipoPago, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new ObtenerSaldoCuentaCajaDiarioResponse(valor));
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ObtenerSaldoCuentaCajadiario");
                        var detail = "No se pudo obtener el saldo de la cuenta de caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoObtenerSaldoCuentaCajaDiario")
            .WithSummary(
                "Solo lectura: CREDITO.usp_ObtenerSaldoCuentaCajadiario(CajaDiarioId, TipoPagoId). cajaDiarioId obligatorio (>=1). tipoPagoId opcional (default 1, como CajaDiarioBL). CreditoUser. saldo puede ser null si el proc no devuelve filas.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ObtenerSaldoCuentaCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/caja-diario-sesion",
                async Task<Results<Ok<CajaDiarioSesionDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ICajaDiarioSesionReadService cajaSesion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene un usuario v?lido (vendix:usuario_id).");
                    }

                    var log = loggerFactory.CreateLogger("CajaDiarioSesion");
                    try
                    {
                        var sesion = await cajaSesion
                            .ObtenerSesionAbiertaAsync(usuarioId, oficinaId, ct)
                            .ConfigureAwait(false);
                        if (sesion is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No hay caja diario abierta asignada al usuario en esta oficina.");
                        }

                        return TypedResults.Ok(sesion);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al leer sesi?n de caja diario");
                        var detail = "No se pudo obtener la sesi?n de caja diario.";
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
            .WithName("CreditoCajaDiarioSesion")
            .WithSummary(
                "Lectura: caja diario abierta con saldos (paridad cabecera CajaDiario.cshtml). usuarioId y oficinaId = JWT.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioSesionDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}