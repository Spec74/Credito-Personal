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

/// <summary>Simulador, TEM/gastos y lectura de plan/mora.</summary>
internal static class CreditoSimuladorCarteraEndpoints
{
    public static void MapCreditoSimuladorCarteraEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/api/v1/credito/calcular-tem",
                async Task<Results<Ok<CalcularTemResponse>, ProblemHttpResult>> (
                    CalcularTemRequest body,
                    ICalcularTemService calcularTem,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("CalcularTem");
                    if (!FormaPagoCredito.TryNormalizar(body.FormaPago, out var formaNormalizada, out var fpError))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: fpError);
                    }

                    try
                    {
                        var tem = await calcularTem.CalcularTemAsync(body.Tea, formaNormalizada, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new CalcularTemResponse(tem));
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CalcularTEM");
                        var detail = "No se pudo ejecutar el procedimiento almacenado.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CalcularTem")
            .WithSummary("Calcula TEM v?a CREDITO.usp_CalcularTEM (formaPago = D|M|Q|S, como el MVC).")
            .WithSummary("Calcula TEM v?a CREDITO.usp_CalcularTEM (formaPago = D|M|Q|S, como el MVC).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CalcularTemResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/calcular-gastos-adm",
                async Task<Results<Ok<CalcularGastosAdmResponse>, ProblemHttpResult>> (
                    decimal monto,
                    bool incluyeCentralRiesgo,
                    IGastosAdmReadService gastosAdm,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (monto <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "monto debe ser mayor a cero.");
                    }

                    var log = loggerFactory.CreateLogger("CalcularGastosAdm");
                    try
                    {
                        var valor = await gastosAdm
                            .CalcularAsync(monto, incluyeCentralRiesgo, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new CalcularGastosAdmResponse(valor));
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
                        log.LogError(ex, "Error al calcular gastos administrativos");
                        var detail = "No se pudo calcular el trámite administrativo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CalcularGastosAdm")
            .WithSummary(
                "Paridad CreditoController.CalcularGastosAdm / GastosAdmBL: tabla CREDITO.GastosAdm. "
                + "incluyeCentralRiesgo=false omite filas Ind=1. No define el monto a desembolsar "
                + "(con ADE el desembolso es el monto del crédito completo).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CalcularGastosAdmResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/tiene-cxc-pendiente",
                async Task<Results<Ok<TieneCxcPendienteResponse>, ProblemHttpResult>> (
                    int creditoId,
                    ICuentaPorCobrarPagoReadService cuentaPorCobrarPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "creditoId debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("TieneCxcPendiente");
                    try
                    {
                        var tiene = await cuentaPorCobrarPago
                            .TienePendientesPorCreditoAsync(creditoId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new TieneCxcPendienteResponse(tiene));
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
                        log.LogError(ex, "Error al consultar CxC pendiente por cr?dito");
                        var detail = "No se pudo consultar cuentas por cobrar pendientes.";
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
            .WithName("CreditoTieneCxcPendiente")
            .WithSummary("Solo lectura: paridad CreditoController.TieneCxcPendiente (CxC PEN del cr?dito).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TieneCxcPendienteResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/creditos-gestor-desembolsados",
                async Task<Results<Ok<List<CreditoGestorPendienteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IDesembolsoReadService desembolsoRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CreditosGestorDesembolsados");
                    try
                    {
                        var items = await desembolsoRead
                            .ListarCreditosGestorDesembolsadosAsync(usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogError(ex, "Error al listar cr?ditos DES del gestor");
                        var detail = "No se pudo listar cr?ditos del gestor.";
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
            .WithName("CreditoCreditosGestorDesembolsados")
            .WithSummary(
                "Solo lectura: paridad CajaDiarioBL.LstCreditoPendienteJGrid (cr?ditos DES registrados por el gestor).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CreditoGestorPendienteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/simulador-credito",
                async Task<Results<Ok<List<SimuladorCreditoCuotaDto>>, ProblemHttpResult>> (
                    SimuladorCreditoRequest body,
                    ISimuladorCreditoReadService simuladorCredito,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("SimuladorCredito");
                    var montoError = ProblemResults.IfInvalid(
                        MoneyRules.RequirePositive(body.Monto, "monto"));
                    if (montoError is not null)
                    {
                        return montoError;
                    }

                    if (!FormaPagoCredito.TryNormalizar(body.FormaPago, out var formaNormalizada, out var fpError))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: fpError);
                    }

                    if (body.NroCuotas < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "nroCuotas debe ser un entero >= 1.");
                    }

                    if (body.InteresMensual < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "interesMensual no puede ser negativo.");
                    }

                    var y = body.FechaPrimerPago.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "fechaPrimerPago debe tener un a?o entre 1900 y 2100.");
                    }

                    var gastosAdm = body.GastosAdm ?? 0m;
                    var gastosError = ProblemResults.IfInvalid(
                        MoneyRules.RequireNonNegative(gastosAdm, "gastosAdm"));
                    if (gastosError is not null)
                    {
                        return gastosError;
                    }

                    try
                    {
                        var saltarDomingo = body.SaltarDomingo ?? true;
                        var cuotas = await simuladorCredito.SimularAsync(
                                body.Monto,
                                formaNormalizada,
                                body.NroCuotas,
                                body.InteresMensual,
                                body.FechaPrimerPago,
                                gastosAdm,
                                saltarDomingo,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(cuotas.ToList());
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
                            title: "Solicitud inv?lida",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "La solicitud enviada no es válida." : ex.Message);
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Argumento inv?lido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "La solicitud enviada no es válida." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_SimuladorCredito");
                        var detail = "No se pudo ejecutar el simulador de cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSimuladorCredito")
            .WithSummary(
                "Solo lectura: CREDITO.usp_SimuladorCredito (plan simulado). formaPago = D|M|Q|S. saltarDomingo default true (cuota en domingo pasa a lunes). Prendario envía false. gastosAdm opcional (default 0). CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<SimuladorCreditoCuotaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/estado-plan-pago",
                async Task<Results<Ok<List<EstadoPlanPagoCuotaDto>>, ProblemHttpResult>> (
                    int? creditoId,
                    IEstadoPlanPagoReadService estadoPlan,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "creditoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("EstadoPlanPago");
                    try
                    {
                        var items = await estadoPlan.ListarPorCreditoAsync(creditoId.Value, ct).ConfigureAwait(false);
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
                        log.LogWarning(ex, "creditoId inv?lido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_EstadoPlanPago");
                        var detail = "No se pudo obtener el estado del plan de pagos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEstadoPlanPago")
            .WithSummary("Solo lectura: CREDITO.usp_EstadoPlanPago(CreditoId). creditoId obligatorio (>=1). Requiere JWT (CreditoUser).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<EstadoPlanPagoCuotaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cuotas-pendientes",
                async Task<Results<Ok<List<CuotasPendientesRowDto>>, ProblemHttpResult>> (
                    int? creditoId,
                    DateTime? fechaCalculo,
                    bool? indCancelacion,
                    ICuotasPendientesReadService cuotas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "creditoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var fecha = fechaCalculo?.Date ?? DateTime.Today;
                    var indCan = indCancelacion == true;
                    var log = loggerFactory.CreateLogger("CuotasPendientes");
                    try
                    {
                        var items = await cuotas.ListarAsync(creditoId.Value, fecha, indCan, ct).ConfigureAwait(false);
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
                        log.LogWarning(ex, "creditoId inv?lido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CuotasPendientes");
                        var detail = "No se pudo obtener el listado de cuotas pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCuotasPendientes")
            .WithSummary(
                "Solo lectura: CREDITO.usp_CuotasPendientes(CreditoId, FechaCalculo, IndCancelacion). creditoId obligatorio. fechaCalculo opcional (default hoy local). indCancelacion opcional (default false). CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CuotasPendientesRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/credito-mora",
                async Task<Results<Ok<List<CreditoMoraRowDto>>, ProblemHttpResult>> (
                    int creditoId,
                    ICreditoMoraReadService moraRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "creditoId debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoMoraListar");
                    try
                    {
                        var items = await moraRead.ListarPorCreditoAsync(creditoId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogError(ex, "Error al listar CreditoMora");
                        var detail = "No se pudo listar el historial de moras del cr?dito.";
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
            .WithName("CreditoMoraListar")
            .WithSummary("Solo lectura: historial CREDITO.CreditoMora por cr?dito (paridad ListarCreditoMoraGrd).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CreditoMoraRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/credito-mora-resumen",
                async Task<Results<Ok<CreditoMoraIndicadorResponse>, ProblemHttpResult>> (
                    int creditoId,
                    ICreditoMoraReadService moraRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "creditoId debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoMoraResumen");
                    try
                    {
                        var indMora = await moraRead
                            .CreditoTieneMoraPostergadaHabilitadaAsync(creditoId, ct)
                            .ConfigureAwait(false);
                        var saldo = await moraRead.ObtenerSaldoPostergadoAsync(creditoId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new CreditoMoraIndicadorResponse(indMora, saldo));
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
                        log.LogError(ex, "Error al consultar resumen CreditoMora");
                        var detail = "No se pudo consultar mora postergada del cr?dito.";
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
            .WithName("CreditoMoraResumen")
            .WithSummary(
                "Solo lectura: IndMora del producto + saldo postergado (paridad CreditoBL.ObtenerMoraPostergadaAcumulada).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditoMoraIndicadorResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/obtener-monto-pendiente-plan-pago",
                async Task<Results<Ok<ObtenerMontoPendientePlanPagoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IObtenerMontoPendientePlanPagoReadService montoPendiente,
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

                    var log = loggerFactory.CreateLogger("ObtenerMontoPendientePlanPago");
                    try
                    {
                        var valor = await montoPendiente.ObtenerAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new ObtenerMontoPendientePlanPagoResponse(valor));
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ObtenerMontoPendientePlanPago");
                        var detail = "No se pudo obtener el monto pendiente del plan de pago.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoObtenerMontoPendientePlanPago")
            .WithSummary(
                "Solo lectura: CREDITO.usp_ObtenerMontoPendientePlanPago(OficinaId). oficinaId obligatorio y debe coincidir con vendix:oficina_id del JWT. CreditoUser. montoPendiente puede ser null si el proc no devuelve filas.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ObtenerMontoPendientePlanPagoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
