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
using Credito.Modern.Application.MediosPago;
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

/// <summary>Bóveda — cierres, transferencias y asignación de caja.</summary>
internal static partial class CreditoBovedaOperacionEndpoints
{
    private static void MapBovedaCierresYTransferencias(WebApplication app)
    {
        app.MapPost(
                "/api/v1/credito/cerrar-boveda",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CerrarBovedaRequest body,
                    IBovedaWriteService bovedaWrite,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

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

                    var log = loggerFactory.CreateLogger("CerrarBoveda");
                    try
                    {
                        var validacion = await saldosCierre
                            .ValidarCierreBovedaAsync(body.OficinaId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeCerrar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Cierre bloqueado",
                                detail: validacion.Mensaje);
                        }

                        var response = await bovedaWrite
                            .CerrarBovedaAsync(body.OficinaId, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CerrarBoveda");
                        var detail = "No se pudo cerrar la b?veda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarBoveda")
            .WithSummary("Escritura: CREDITO.usp_CerrarBoveda. Paridad BovedaBL.Cerrar + BovedaController.ValidarCierre.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-boveda-temporal",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CerrarBovedaRequest body,
                    IBovedaWriteService bovedaWrite,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

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

                    var log = loggerFactory.CreateLogger("CerrarBovedaTemporal");
                    try
                    {
                        // Solo temporal: no bloquea por descuadre de la principal.
                        var validacion = await saldosCierre
                            .ValidarCierreBovedaTemporalAsync(body.OficinaId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeCerrar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Cierre bloqueado",
                                detail: validacion.Mensaje);
                        }

                        var response = await bovedaWrite
                            .CerrarBovedaTemporalAsync(body.OficinaId, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CerrarBovedaTemporal");
                        var detail = "No se pudo cerrar la b?veda temporal.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarBovedaTemporal")
            .WithSummary("Escritura: CREDITO.usp_CerrarBovedaTemporal. Paridad BovedaBL.CerrarBovedaTemporal + BovedaController.ValidarCierre.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-boveda",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaRequest body,
                    IBovedaWriteService bovedaWrite,
                    IBovedaOficinaReadService bovedaOficina,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var (sesionTipo, sesionTipoErr) = await SesionOficina
                        .ResolverAsync(httpContext, oficinas, null, ct)
                        .ConfigureAwait(false);
                    if (sesionTipoErr is not null)
                        return sesionTipoErr;

                    // Fondeo y remesa tipan con el catálogo Central (billeteras + bancos).
                    // Sucursal: el Id elegido es la billetera destino en Central; el USP debita el genérico.
                    var tipoPagoFondeo = body.TipoPagoId < 1 ? (short)1 : body.TipoPagoId;
                    var tipoPagoError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireCobroPermitido(tipoPagoFondeo, esCentral: true));
                    if (tipoPagoError is not null)
                        return tipoPagoError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var aceptacionTemporal = body.BovedaMovTempId > 0;
                    if (aceptacionTemporal && body.FlagAceptar != 2)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "usp_TransferirBoveda solo anula pendientes con flag 2. El fondeo es inmediato (flag 0).");
                    }

                    if (aceptacionTemporal)
                    {
                        var oficinaDestinoTemp = await bovedaOficina
                            .GetOficinaIdByBovedaMovTempDestinoAsync(body.BovedaMovTempId, ct)
                            .ConfigureAwait(false);
                        if (oficinaDestinoTemp is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe la transferencia pendiente indicada.");
                        }

                        if (oficinaDestinoTemp.Value != body.OficinaId)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "La pendiente no pertenece a la oficina del token.");
                        }
                    }

                    if (!aceptacionTemporal)
                    {
                        var edgeError = ProblemResults.IfInvalid(
                            TesoreriaValidacion.ValidarTransferenciaInterBoveda(
                                body.OficinaId,
                                body.BovedaInicioId,
                                body.BovedaDestinoId,
                                body.Monto,
                                body.Glosa));
                        if (edgeError is not null)
                            return edgeError;
                    }

                    var log = loggerFactory.CreateLogger("TransferirBoveda");
                    try
                    {
                        if (!aceptacionTemporal)
                        {
                            var oficinaInicio = await bovedaOficina
                                .GetOficinaIdByBovedaIdAsync(body.BovedaInicioId, ct)
                                .ConfigureAwait(false);
                            if (oficinaInicio is null)
                            {
                                return TypedResults.Problem(
                                    statusCode: StatusCodes.Status404NotFound,
                                    title: "No encontrado",
                                    detail: "No existe la b?veda de origen indicada.");
                            }

                            if (oficinaInicio.Value != body.OficinaId)
                            {
                                return TypedResults.Problem(
                                    statusCode: StatusCodes.Status403Forbidden,
                                    title: "Prohibido",
                                    detail: "La b?veda de origen no pertenece a la oficina del token.");
                            }

                            var oficinaDestino = await bovedaOficina
                                .GetOficinaIdByBovedaIdAsync(body.BovedaDestinoId, ct)
                                .ConfigureAwait(false);
                            if (oficinaDestino is null)
                            {
                                return TypedResults.Problem(
                                    statusCode: StatusCodes.Status404NotFound,
                                    title: "No encontrado",
                                    detail: "No existe la b?veda de destino indicada.");
                            }

                            // Red: Central → sucursal; sucursal → Central. Sin peer entre sucursales.
                            var activas = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
                            var destinoEsCentral = activas
                                .FirstOrDefault(o => o.OficinaId == oficinaDestino.Value)
                                ?.IndPrincipal == true;
                            if (sesionTipo!.EsCentral && destinoEsCentral)
                            {
                                return TypedResults.Problem(
                                    statusCode: StatusCodes.Status400BadRequest,
                                    title: "Destino inválido",
                                    detail: "Desde Central fondee una sucursal (no otra bóveda Central).");
                            }

                            if (!sesionTipo.EsCentral && !destinoEsCentral)
                            {
                                return TypedResults.Problem(
                                    statusCode: StatusCodes.Status400BadRequest,
                                    title: "Destino inválido",
                                    detail: "Desde sucursal solo puede enviar saldo a la bóveda Central.");
                            }

                            // Dual-oficina: medio tipado activo en origen y destino (mapeo familia).
                            short tipoEnOrigen;
                            short tipoEnDestino;
                            if (sesionTipo.EsCentral)
                            {
                                tipoEnOrigen = tipoPagoFondeo;
                                tipoEnDestino = TipoPagoRules.MapFondeoDestino(tipoPagoFondeo);
                            }
                            else
                            {
                                tipoEnOrigen = TipoPagoRules.MapRemesaOrigen(tipoPagoFondeo);
                                tipoEnDestino = tipoPagoFondeo;
                            }

                            var medioOrigenErr = ProblemResults.IfInvalid(
                                await mediosPago
                                    .ValidateMedioActivoAsync(
                                        oficinaInicio.Value,
                                        tipoEnOrigen,
                                        ct)
                                    .ConfigureAwait(false));
                            if (medioOrigenErr is not null)
                                return medioOrigenErr;

                            var medioDestinoErr = ProblemResults.IfInvalid(
                                await mediosPago
                                    .ValidateMedioActivoAsync(
                                        oficinaDestino.Value,
                                        tipoEnDestino,
                                        ct)
                                    .ConfigureAwait(false));
                            if (medioDestinoErr is not null)
                                return medioDestinoErr;
                        }

                        var response = await bovedaWrite
                            .TransferirBovedaAsync(
                                body.BovedaInicioId,
                                body.BovedaDestinoId,
                                body.Glosa,
                                body.Monto,
                                usuarioId,
                                body.FlagAceptar,
                                body.BovedaMovTempId,
                                body.TipoPagoId < 1 ? (short)1 : body.TipoPagoId,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_TransferirBoveda");
                        var detail = "No se pudo transferir entre b?vedas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirBoveda")
            .WithSummary(
                "Escritura: CREDITO.usp_TransferirBoveda. Fondeo inmediato (flag 0) con TipoPagoId, o anulación de BovedaMovTemp (flag 2). El USP no implementa aceptación.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cajas-para-asignar",
                async Task<Results<Ok<List<CajaParaAsignarRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ICajaAsignacionReadService cajaAsignacion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("CajasParaAsignar");
                    try
                    {
                        var items = await cajaAsignacion
                            .ListarCajasParaAsignarAsync(oficinaId, ct)
                            .ConfigureAwait(false);
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
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar cajas para asignar");
                        var detail = "No se pudo listar cajas disponibles.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCajasParaAsignar")
            .WithSummary(
                "Solo lectura: paridad SaldosController.ObtnerListaCajasCombo (cajas activas, cerradas, de la oficina).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CajaParaAsignarRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/monto-boveda-asignacion",
                async Task<Results<Ok<MontoBovedaAsignacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ICajaAsignacionReadService cajaAsignacion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("MontoBovedaAsignacion");
                    try
                    {
                        var (monto, esTemporal) = await cajaAsignacion
                            .ObtenerMontoBovedaAsignacionAsync(oficinaId, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new MontoBovedaAsignacionResponse(monto, esTemporal));
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
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al obtener monto de b?veda");
                        var detail = "No se pudo obtener el monto de b?veda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMontoBovedaAsignacion")
            .WithSummary(
                "Solo lectura: paridad SaldosController.MostrarMontoBoveda (b?veda temporal si rol ENCARGADO).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MontoBovedaAsignacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/asignar-caja",
                async Task<Results<Ok<AsignarCajaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AsignarCajaRequest body,
                    ICajaAsignacionWriteService cajaAsignacionWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.CajaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y cajaId deben ser enteros >= 1.");
                    }

                    if (body.SaldoInicial < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "saldoInicial debe ser >= 0.");
                    }

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

                    var log = loggerFactory.CreateLogger("AsignarCaja");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var (error, result) = await cajaAsignacionWrite
                            .AsignarAsync(
                                body.OficinaId,
                                body.CajaId,
                                body.SaldoInicial,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo asignar la caja",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al asignar caja");
                        var detail = "No se pudo asignar la caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAsignarCaja")
            .WithSummary(
                "Escritura: paridad CajaDiarioBL.AsignarUsuarioCaja / SaldosController.AsignarCaja (EF + usp_CalcularMontoPorCobrar). Cajero desde Caja.CajeroId. Bloquea LECTURA_SALDO.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoNoLecturaSaldo)
            .Produces<AsignarCajaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



    }
}