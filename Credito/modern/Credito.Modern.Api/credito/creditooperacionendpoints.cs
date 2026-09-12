using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
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
using Credito.Modern.Application.Ventas;
using Credito.Modern.Infrastructure.CreditoPlanes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

internal static class CreditoOperacionEndpoints
{
    public static void MapCreditoOperacionEndpoints(this WebApplication app)
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
                            title: "Solicitud inválida",
                            detail: fpError);
                    }

                    try
                    {
                        var tem = await calcularTem.CalcularTemAsync(body.Tea, formaNormalizada, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new CalcularTemResponse(tem));
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
            .WithSummary("Calcula TEM vía CREDITO.usp_CalcularTEM (formaPago = D|M|Q|S, como el MVC).")
            .WithSummary("Calcula TEM vía CREDITO.usp_CalcularTEM (formaPago = D|M|Q|S, como el MVC).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CalcularTemResponse>(StatusCodes.Status200OK, "application/json")
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
                            title: "Solicitud inválida",
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
                        log.LogError(ex, "Error al consultar CxC pendiente por crédito");
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
            .WithSummary("Solo lectura: paridad CreditoController.TieneCxcPendiente (CxC PEN del crédito).")
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
                        log.LogError(ex, "Error al listar créditos DES del gestor");
                        var detail = "No se pudo listar créditos del gestor.";
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
                "Solo lectura: paridad CajaDiarioBL.LstCreditoPendienteJGrid (créditos DES registrados por el gestor).")
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
                    if (body.Monto <= 0)
                    {
                        return TypedResults.Ok(new List<SimuladorCreditoCuotaDto>());
                    }

                    if (!FormaPagoCredito.TryNormalizar(body.FormaPago, out var formaNormalizada, out var fpError))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: fpError);
                    }

                    if (body.NroCuotas < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "nroCuotas debe ser un entero >= 1 cuando monto > 0.");
                    }

                    if (body.InteresMensual < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "interesMensual no puede ser negativo.");
                    }

                    var y = body.FechaPrimerPago.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "fechaPrimerPago debe tener un año entre 1900 y 2100.");
                    }

                    var gastosAdm = body.GastosAdm ?? 0m;

                    try
                    {
                        var cuotas = await simuladorCredito.SimularAsync(
                                body.Monto,
                                formaNormalizada,
                                body.NroCuotas,
                                body.InteresMensual,
                                body.FechaPrimerPago,
                                gastosAdm,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(cuotas.ToList());
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Argumento inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_SimuladorCredito");
                        var detail = "No se pudo ejecutar el simulador de crédito.";
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
                "Solo lectura: CREDITO.usp_SimuladorCredito (plan simulado). formaPago = D|M|Q|S como calcular-tem / MVC. monto <= 0 devuelve lista vacía sin llamar a SQL (como CreditoController.Simulador). gastosAdm opcional (default 0). CreditoUser.")
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
                            title: "Parámetros inválidos",
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
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "creditoId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
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
                            title: "Parámetros inválidos",
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
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "creditoId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
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
                "/api/v1/credito/calcular-mora-pendiente",
                async Task<Results<Ok<CalcularMoraPendienteResponse>, ProblemHttpResult>> (
                    int? creditoId,
                    ICalcularMoraPendienteReadService moraPendiente,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CalcularMoraPendiente");
                    try
                    {
                        var valor = await moraPendiente.ObtenerAsync(creditoId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new CalcularMoraPendienteResponse(valor));
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
                        log.LogWarning(ex, "creditoId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CalcularMoraPendiente");
                        var detail = "No se pudo calcular la mora pendiente.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCalcularMoraPendiente")
            .WithSummary(
                "Solo lectura: CREDITO.usp_CalcularMoraPendiente(CreditoId). creditoId obligatorio (>=1). CreditoUser. moraPendiente puede ser null si el proc no devuelve filas.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CalcularMoraPendienteResponse>(StatusCodes.Status200OK, "application/json")
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
                            title: "Solicitud inválida",
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
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar CreditoMora");
                        var detail = "No se pudo listar el historial de moras del crédito.";
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
            .WithSummary("Solo lectura: historial CREDITO.CreditoMora por crédito (paridad ListarCreditoMoraGrd).")
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
                            title: "Solicitud inválida",
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
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al consultar resumen CreditoMora");
                        var detail = "No se pudo consultar mora postergada del crédito.";
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

                    var log = loggerFactory.CreateLogger("ObtenerMontoPendientePlanPago");
                    try
                    {
                        var valor = await montoPendiente.ObtenerAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new ObtenerMontoPendientePlanPagoResponse(valor));
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

                    var log = loggerFactory.CreateLogger("UsuariosNoAsignadosCaja");
                    try
                    {
                        var items = await usuariosNoAsignados.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
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
                            title: "Parámetros inválidos",
                            detail: "cajaDiarioId es obligatorio y debe ser un entero >= 1.");
                    }

                    var tipoPago = tipoPagoId ?? 1;
                    if (tipoPago < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
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
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
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
                            title: "Parámetros inválidos",
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
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
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
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al leer sesión de caja diario");
                        var detail = "No se pudo obtener la sesión de caja diario.";
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



        app.MapGet(
                "/api/v1/credito/credito-contexto",
                async Task<Results<Ok<CreditoContextoDto>, ProblemHttpResult>> (
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoContexto");
                    try
                    {
                        var ctx = await gestionRead.ObtenerContextoAsync(creditoId, ct).ConfigureAwait(false);
                        if (ctx is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Crédito no encontrado.");
                        }

                        return TypedResults.Ok(ctx);
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
                        log.LogError(ex, "Error al obtener contexto de crédito");
                        var detail = "No se pudo obtener el contexto.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoContexto")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditoContextoDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/solicitud-credito",
                async Task<Results<Ok<SolicitudCreditoDetalleDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int solicitudCreditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, solicitudCreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, solicitudCreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("SolicitudCreditoDetalle");
                    try
                    {
                        var solicitud = await gestionRead
                            .ObtenerSolicitudCreditoAsync(solicitudCreditoId, ct)
                            .ConfigureAwait(false);
                        if (solicitud is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Solicitud de crédito CRE no encontrada.");
                        }

                        return TypedResults.Ok(solicitud);
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
                        log.LogError(ex, "Error al obtener solicitud de crédito");
                        var detail = "No se pudo obtener la solicitud de crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSolicitudDetalle")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .WithSummary("Solo lectura: solicitud CRE para continuar originación desde Crédito > Créditos.")
            .Produces<SolicitudCreditoDetalleDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/prendas",
                async Task<Results<Ok<IReadOnlyList<PrendaDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("Prenda");
                    try
                    {
                        var prendas = await gestionRead.ListarPrendasAsync(creditoId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(prendas);
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
                        log.LogError(ex, "Error al listar prendas del crédito");
                        var detail = "No se pudieron obtener los bienes del crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendasListar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<IReadOnlyList<PrendaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cargos-credito",
                async Task<Results<Ok<List<CargoCreditoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("CargosCredito");
                    try
                    {
                        var items = await gestionRead.ListarCargosAsync(creditoId, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar cargos");
                        var detail = "No se pudo listar cargos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCargosListar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CargoCreditoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/evidencias-credito",
                async Task<Results<Ok<List<CreditoEvidenciaDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("EvidenciasCredito");
                    try
                    {
                        var items = await gestionRead.ListarEvidenciasAsync(creditoId, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar evidencias");
                        var detail = "No se pudo listar evidencias.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEvidenciasListar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CreditoEvidenciaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/evidencia-archivo/{creditoImagenId:int}",
                async Task<Results<PhysicalFileHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int creditoImagenId,
                    ICreditoGestionReadService gestionRead,
                    ICreditoOficinaReadService creditoOficina,
                    IOptions<CreditoStorageOptions> storageOptions,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoImagenId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "creditoImagenId inválido.");
                    }

                    var log = loggerFactory.CreateLogger("EvidenciaArchivo");
                    try
                    {
                        var meta = await gestionRead
                            .ObtenerEvidenciaArchivoAsync(creditoImagenId, ct)
                            .ConfigureAwait(false);
                        if (meta is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Evidencia no encontrada.");
                        }

                        if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "El token no contiene una oficina válida.");
                        }

                        var scopeError = await CajaCreditoWriteGuards
                            .ValidateCreditoOficinaAsync(jwtOficinaId, meta.Value.CreditoId, creditoOficina, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                            return scopeError;

                        var root = CreditoGestionWriteService.ResolveStorageRoot(storageOptions.Value);
                        var path = Path.Combine(root, meta.Value.CreditoId.ToString(), meta.Value.FileName);
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
                        log.LogError(ex, "Error al obtener evidencia");
                        var detail = "No se pudo obtener la evidencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEvidenciaArchivo")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/persona-credito-ficha",
                async Task<Results<Ok<PersonaCreditoFichaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("PersonaCreditoFicha");
                    try
                    {
                        var ficha = await gestionRead
                            .ObtenerPersonaCreditoFichaAsync(oficinaId, personaId, ct)
                            .ConfigureAwait(false);
                        if (ficha is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Cliente o persona no encontrados.");
                        }

                        return TypedResults.Ok(ficha);
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
                        log.LogError(ex, "Error al obtener ficha persona crédito");
                        var detail = "No se pudo obtener la ficha del cliente.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PersonaCreditoFicha")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .WithSummary("Cabecera cliente en consulta crédito (paridad Creditos.cshtml con pPersonaId).")
            .Produces<PersonaCreditoFichaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/creditos-grilla-persona",
                async Task<Results<Ok<CreditoGrillaPersonaPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    bool grupoActivo,
                    int page,
                    int pageSize,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("CreditosGrillaPersona");
                    try
                    {
                        var response = await gestionRead
                            .ListarCreditosGrillaPersonaAsync(
                                oficinaId,
                                personaId,
                                grupoActivo,
                                page < 1 ? 1 : page,
                                pageSize < 1 ? 25 : pageSize,
                                ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar créditos por persona");
                        var detail = "No se pudo listar créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoGrillaPersona")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditoGrillaPersonaPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/avales-persona",
                async Task<Results<Ok<IReadOnlyList<CreditoAvalRelacionDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    ICreditoGestionReadService gestionRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("AvalesPersona");
                    try
                    {
                        var response = await gestionRead
                            .ListarAvalesPersonaAsync(oficinaId, personaId, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar avales por persona");
                        var detail = "No se pudo listar avales y avalados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAvalesPersona")
            .WithSummary("Solo lectura: relaciones reales de aval y avalado para la ficha moderna de crédito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<IReadOnlyList<CreditoAvalRelacionDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/condonar-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CondonarCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("CondonarCredito");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .CondonarAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "No condonado",
                                detail: response.Mensaje ?? "Error al condonar.");
                        }

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
                        log.LogError(ex, "Error al condonar crédito");
                        var detail = "No se pudo condonar el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCondonar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolSoloAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/observar-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ObservarCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ObservarCredito");
                    try
                    {
                        var response = await gestionWrite.ObservarAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al observar crédito");
                        var detail = "No se pudo guardar la observación.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoObservar")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/guardar-cargo-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarCargoCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("GuardarCargoCredito");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .GuardarCargoAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Cargo no guardado",
                                detail: response.Mensaje ?? "Error al guardar cargo.");
                        }

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
                        log.LogError(ex, "Error al guardar cargo");
                        var detail = "No se pudo guardar el cargo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoGuardarCargo")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/subir-evidencia-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    HttpRequest httpRequest,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!httpRequest.HasFormContentType)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Se requiere multipart/form-data.");
                    }

                    var form = await httpRequest.ReadFormAsync(ct).ConfigureAwait(false);
                    if (!int.TryParse(form["oficinaId"], out var oficinaId) || oficinaId < 1
                        || !int.TryParse(form["creditoId"], out var creditoId) || creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y creditoId son obligatorios.");
                    }

                    var file = form.Files.GetFile("imagen");
                    if (file is null || file.Length < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Seleccione una imagen (campo imagen).");
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp"))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Formato de imagen no permitido.");
                    }

                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("SubirEvidenciaCredito");
                    try
                    {
                        await using var stream = file.OpenReadStream();
                        var response = await gestionWrite
                            .SubirEvidenciaAsync(oficinaId, creditoId, ext, stream, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "No subida",
                                detail: response.Mensaje ?? "Error al subir evidencia.");
                        }

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
                        log.LogError(ex, "Error al subir evidencia");
                        var detail = "No se pudo subir la evidencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSubirEvidencia")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .DisableAntiforgery()
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/eliminar-evidencia-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarEvidenciaRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.CreditoImagenId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y creditoImagenId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("EliminarEvidenciaCredito");
                    try
                    {
                        var response = await gestionWrite
                            .EliminarEvidenciaAsync(body.OficinaId, body.CreditoImagenId, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al eliminar evidencia");
                        var detail = "No se pudo eliminar la evidencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEliminarEvidencia")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cambiar-analista-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CambiarAnalistaCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("CambiarAnalistaCredito");
                    try
                    {
                        var response = await gestionWrite.CambiarAnalistaAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al cambiar analista");
                        var detail = "No se pudo cambiar el analista.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCambiarAnalista")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-tope-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarTopeCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var log = loggerFactory.CreateLogger("ActualizarTopeCredito");
                    try
                    {
                        var response = await gestionWrite.ActualizarTopeAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al actualizar tope");
                        var detail = "No se pudo actualizar el tope.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarTope")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/depurar-persona-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DepurarPersonaCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;

                    var log = loggerFactory.CreateLogger("DepurarPersonaCredito");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .DepurarPersonaAsync(body, usuarioId, serverTime.Value, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al depurar persona");
                        var detail = "No se pudo depurar la persona.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoDepurarPersona")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-irrecuperable-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarIrrecuperableRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ActualizarIrrecuperable");
                    try
                    {
                        var response = await gestionWrite.ActualizarIrrecuperableAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al actualizar irrecuperable");
                        var detail = "No se pudo actualizar el indicador.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarIrrecuperable")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/modificar-tramite-adm-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ModificarTramiteAdmCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ModificarTramiteAdmCredito");
                    try
                    {
                        var response = await gestionWrite.ModificarTramiteAdmAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al modificar trámite administrativo");
                        var detail = "No se pudo modificar el trámite administrativo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoModificarTramiteAdm")
            .WithSummary("Paridad CreditoController.ModificarTramiteAdmCredito (MontoGastosAdm).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/modificar-central-riesgo-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ModificarCentralRiesgoCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ModificarCentralRiesgoCredito");
                    try
                    {
                        var response = await gestionWrite.ModificarCentralRiesgoAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al modificar central de riesgo");
                        var detail = "No se pudo modificar la central de riesgo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoModificarCentralRiesgo")
            .WithSummary("Paridad CreditoController.ModificarCentralRieagoCredito (CentralRiesgo).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-descuento-plan-pago",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarDescuentoPlanPagoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.PlanPagoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "planPagoId es obligatorio.");
                    }

                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ActualizarDescuentoPlanPago");
                    try
                    {
                        var response = await gestionWrite
                            .ActualizarDescuentoPlanPagoAsync(body, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al actualizar descuento del plan de pagos");
                        var detail = "No se pudo actualizar el descuento.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarDescuentoPlanPago")
            .WithSummary("Paridad CreditoController.ActualizarDescuentoPlanPago.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolSoloAdministrador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/guardar-prendas",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarPrendasRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("GuardarPrendas");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor.");
                        }

                        var response = await gestionWrite
                            .GuardarPrendasAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Bienes no guardados",
                                detail: response.Mensaje ?? "Error al guardar los bienes.");
                        }

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
                        log.LogError(ex, "Error al guardar los bienes del crédito prendario");
                        var detail = "No se pudieron guardar los bienes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("PrendasGuardar")
            .WithSummary("Modern: reemplaza los bienes en custodia de un crédito prendario.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-aval-credito",
                async Task<Results<Ok<CreditoGestionOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarAvalCreditoRequest body,
                    ICreditoGestionWriteService gestionWrite,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                        return idError;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;
                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                        return scopeError;

                    var log = loggerFactory.CreateLogger("ActualizarAvalCredito");
                    try
                    {
                        var response = await gestionWrite.ActualizarAvalAsync(body, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

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
                        log.LogError(ex, "Error al actualizar aval del crédito");
                        var detail = "No se pudo actualizar el aval.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarAval")
            .WithSummary("Paridad CreditoController.ActualizarAvalCredito (PersonaAvalId).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CreditoGestionOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/creditos-por-persona",
                async Task<Results<Ok<List<CreditoPorPersonaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? personaId,
                    bool? esCajaCentral,
                    ICreditosPorPersonaReadService creditosPersona,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
                    }

                    var log = loggerFactory.CreateLogger("CreditosPorPersona");
                    try
                    {
                        var items = await creditosPersona
                            .ListarDesembolsadosPorPersonaAsync(
                                personaId.Value,
                                usuarioId,
                                esCajaCentral == true,
                                ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar créditos por persona");
                        var detail = "No se pudo listar los créditos.";
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
            .WithName("CreditoCreditosPorPersona")
            .WithSummary(
                "Créditos estado DES por persona (paridad ListarCreditosPendientesCombo). esCajaCentral omite filtro UsuarioRegId.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CreditoPorPersonaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/pagos-no-verificados",
                async Task<Results<Ok<List<PagosNoVerificadosRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IPagosNoVerificadosReadService pagosNoVerificados,
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

                    var log = loggerFactory.CreateLogger("PagosNoVerificados");
                    try
                    {
                        var items = await pagosNoVerificados.ListarAsync(oficinaId.Value, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagosNoVerificados");
                        var detail = "No se pudo obtener el listado de pagos no verificados.";
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
            .WithName("CreditoPagosNoVerificados")
            .WithSummary(
                "Solo lectura: CREDITO.usp_PagosNoVerificados() con post-filtro por oficina JWT.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<PagosNoVerificadosRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/verificar-pago-transferencia",
                async Task<Results<Ok<VerificarPagoTransferenciaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    VerificarPagoTransferenciaRequest body,
                    IMovimientoCajaScopeReadService movimientoScope,
                    IVerificarPagoTransferenciaWriteService verificarPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(body.OficinaId, body.MovimientoCajaId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("VerificarPagoTransferencia");
                    try
                    {
                        var (scopeError, scope) = await CajaCreditoWriteGuards
                            .ValidateMovimientoOficinaAsync(body.OficinaId, body.MovimientoCajaId, movimientoScope, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                        {
                            return scopeError;
                        }

                        if (scope!.CajaDiarioCerrada)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Caja cerrada",
                                detail: "No se puede verificar un pago en una caja diario cerrada.");
                        }

                        var response = await verificarPago
                            .VerificarAsync(body.MovimientoCajaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Verificar pago transferencia");
                        var status = ex.Message.Contains("No existe extensión", StringComparison.OrdinalIgnoreCase)
                            ? StatusCodes.Status404NotFound
                            : StatusCodes.Status503ServiceUnavailable;
                        return TypedResults.Problem(
                            statusCode: status,
                            title: status == StatusCodes.Status404NotFound ? "No encontrado" : "Error de operación",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
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
                        log.LogError(ex, "Error al verificar pago por transferencia");
                        var detail = "No se pudo verificar el pago.";
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
            .WithName("CreditoVerificarPagoTransferencia")
            .WithSummary(
                "Escritura: marca IndTransferenciaVerificada en MovimientoCajaExtension (paridad VerificarPagosController.Verificar). oficinaId = JWT.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<VerificarPagoTransferenciaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/completar-impagos-validacion",
                async Task<Results<Ok<CompletarImpagosValidacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? cajaDiarioId,
                    ICompletarImpagosValidacionReadService completarImpagosValidacion,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId y cajaDiarioId son obligatorios y deben ser enteros >= 1.");
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

                    var cajaOid = await cajaDiarioOficina
                        .GetOficinaIdByCajaDiarioIdAsync(cajaDiarioId.Value, ct)
                        .ConfigureAwait(false);
                    if (cajaOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una caja diario con el cajaDiarioId indicado.");
                    }

                    if (cajaOid.Value != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La caja diario no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("CompletarImpagosValidacion");
                    try
                    {
                        var response = await completarImpagosValidacion
                            .ValidarAsync(cajaDiarioId.Value, ct)
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
                        log.LogWarning(ex, "cajaDiarioId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CompletarImpagosValidacion");
                        var detail = "No se pudo validar impagos pendientes de la caja diario.";
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
            .WithName("CreditoCompletarImpagosValidacion")
            .WithSummary(
                "Solo lectura: CREDITO.usp_CompletarImpagosValidacion(CajaDiarioId). cajaDiarioId y oficinaId obligatorios; oficinaId = vendix:oficina_id y la caja diario debe pertenecer a esa oficina. cantidadImpagosPendientes > 0 bloquea flujos en MVC (CreditoBL.CompletarImpagosValidar / CreditoController.ValidarCredito). CreditoUser. Distinto de POST completar-impagos (usp_CompletarImpagos, escritura).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CompletarImpagosValidacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/completar-impagos",
                async Task<Results<Ok<CompletarImpagosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CompletarImpagosRequest body,
                    ICompletarImpagosWriteService completarImpagos,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.CajaDiarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y cajaDiarioId deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var cajaOid = await cajaDiarioOficina
                        .GetOficinaIdByCajaDiarioIdAsync(body.CajaDiarioId, ct)
                        .ConfigureAwait(false);
                    if (cajaOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una caja diario con el cajaDiarioId indicado.");
                    }

                    if (cajaOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La caja diario no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("CompletarImpagos");
                    try
                    {
                        var response = await completarImpagos.EjecutarAsync(body.CajaDiarioId, ct).ConfigureAwait(false);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CompletarImpagos");
                        var detail = "No se pudo completar impagos de la caja diario.";
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
            .WithName("CreditoCompletarImpagos")
            .WithSummary(
                "Escritura: CREDITO.usp_CompletarImpagos(CajaDiarioId). Body { oficinaId, cajaDiarioId }; oficinaId = vendix:oficina_id. Paridad CreditoBL.CompletarImpagos (el SP registra CUO 0 en créditos sin cobro del día). GET completar-impagos-validacion se usa en cierre, no como rechazo de esta escritura.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CompletarImpagosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuotas",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotasRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaPagoMoraOrchestrator moraOrchestrator,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (string.IsNullOrWhiteSpace(body.ListaPlanPagoId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "listaPlanPagoId es obligatorio.");
                    }

                    if (body.ImporteRecibido <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importeRecibido debe ser > 0.");
                    }

                    if (body.TipoPagoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "tipoPagoId debe ser >= 1.");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario está cerrada.");
                    }

                    var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                    if (serverTime is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos",
                            detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotas");
                    try
                    {
                        var response = await cajaPago
                            .PagarCuotasAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                body.ListaPlanPagoId,
                                body.ImporteRecibido,
                                usuarioId,
                                serverTime.Value,
                                body.TipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

                        if (body.AplicarMoraPostergada && response.ResultId is > 0)
                        {
                            var planIds = body.ListaPlanPagoId
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(s => int.TryParse(s, out var id) ? id : 0)
                                .Where(id => id > 0)
                                .ToList();
                            await moraOrchestrator
                                .AplicarTrasPagoCuotasAsync(
                                    body.CreditoId,
                                    body.CajaDiarioId,
                                    usuarioId,
                                    body.TipoPagoId,
                                    planIds,
                                    body.EsUltimaCuota,
                                    ct)
                                .ConfigureAwait(false);
                        }

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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuotas");
                        var detail = "No se pudo registrar el pago de cuotas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuotas")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuotas. usuarioId y fechaPago desde JWT y usp_FechaBD. Paridad CajaDiarioBL.PagarCuotas / CreditoController.PagarCuotas.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuota-importe-libre",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotaPagoLibreRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.ImporteRecibido <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importeRecibido debe ser > 0.");
                    }

                    if (body.TipoPagoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "tipoPagoId debe ser >= 1.");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario está cerrada.");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotaPagoLibre");
                    try
                    {
                        var response = await cajaPago
                            .PagarCuotaPagoLibreAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                body.ImporteRecibido,
                                usuarioId,
                                body.TipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuotaPagoLibre");
                        var detail = "No se pudo registrar el pago con importe libre.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuotaImporteLibre")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuotaPagoLibre. Paridad CajaDiarioBL.PagarCuotaPagoLibre / CreditoController.PagarCuotasImporteLibre.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuota-con-mora",
                async Task<Results<Ok<PagarCuotaConMoraResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotaConMoraRequest body,
                    ICajaPagoMoraOrchestrator moraOrchestrator,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.ImporteRecibido <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importeRecibido debe ser > 0.");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario está cerrada.");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotaConMora");
                    try
                    {
                        var response = await moraOrchestrator
                            .ProcesarPagoCuotaConMoraAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                body.ImporteRecibido,
                                usuarioId,
                                body.TipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                body.EsUltimaCuota,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

                        var mensaje = body.EsUltimaCuota
                            ? "Cuota final procesada y moras acumuladas liquidadas con éxito."
                            : $"Pago registrado (mov. {response.ResultId}).";
                        return TypedResults.Ok(
                            new PagarCuotaConMoraResponse(response.ResultId, mensaje, body.EsUltimaCuota));
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
                        log.LogError(ex, "Error en pago con mora postergada");
                        var detail = "No se pudo procesar el pago con mora postergada.";
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
            .WithName("CreditoPagarCuotaConMora")
            .WithSummary(
                "Escritura: paridad CajaDiarioBL.ProcesarPagoCompleto / ProcesarPagoCuotaConMora (pago libre + liquidación mora).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagarCuotaConMoraResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuotas-cancelacion",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotasCancelacionRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario está cerrada.");
                    }

                    var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                    if (serverTime is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos",
                            detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotasCancelacion");
                    try
                    {
                        var response = await cajaPago
                            .PagarCuotasCancelacionAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuotasCancelacion");
                        var detail = "No se pudo registrar el pago por cancelación.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuotasCancelacion")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuotasCancelacion. Paridad CajaDiarioBL.PagarCuotasCancelacion / CreditoController.PagarCuotasCancelacion.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-cierre-caja-diario",
                async Task<Results<Ok<ValidarCierreCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? cajaDiarioId,
                    ICajaDiarioCierreService cajaDiarioCierre,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId y cajaDiarioId son obligatorios y deben ser enteros >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId.Value);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(oficinaId.Value, cajaDiarioId.Value, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreCajaDiario");
                    try
                    {
                        var response = await cajaDiarioCierre
                            .ValidarCierreAsync(cajaDiarioId.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        log.LogWarning(ex, "Caja diario no encontrada");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre de caja diario");
                        var detail = "No se pudo validar el cierre de la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreCajaDiario")
            .WithSummary(
                "Solo lectura: validaciones previas a cerrar caja (paridad CreditoController.ValidarCierreCajaDiario / CajaDiarioBL). oficinaId = vendix:oficina_id. CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-caja-diario",
                async Task<Results<Ok<CerrarCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CerrarCajaDiarioRequest body,
                    ICajaDiarioCierreService cajaDiarioCierre,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("CerrarCajaDiario");
                    try
                    {
                        var validacion = await cajaDiarioCierre
                            .ValidarCierreAsync(body.CajaDiarioId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeCerrar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "No se puede cerrar la caja",
                                detail: string.Join("; ", validacion.Blockers),
                                extensions: new Dictionary<string, object?> { ["blockers"] = validacion.Blockers });
                        }

                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await cajaDiarioCierre
                            .CerrarAsync(body.CajaDiarioId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        log.LogWarning(ex, "Caja diario no encontrada");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cierre de caja rechazado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "No se puede cerrar la caja",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
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
                        log.LogError(ex, "Error al cerrar caja diario");
                        var detail = "No se pudo cerrar la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarCajaDiario")
            .WithSummary(
                "Escritura: paridad CajaDiarioBL.CerrarCajaDiario (UPDATE CajaDiario/Caja + CREDITO.ActualizarClientesNuevos). Rechaza 409 si validar-cierre falla. usuarioModId = vendix:usuario_id; fecha = usp_FechaBD. Distinto de cerrar-cajas-diarios (transferencia bóveda).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CerrarCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/reconciliar-caja-diario",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ReconciliarCajaDiarioRequest body,
                    ICajaDiarioOperacionWriteService cajaDiarioOperacion,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ReconciliarCajaDiario");
                    try
                    {
                        var response = await cajaDiarioOperacion
                            .ReconciliarCajaDiarioAsync(body.CajaDiarioId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ReconciliarCajaDiario");
                        var detail = "No se pudo reconciliar la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoReconciliarCajaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_ReconciliarCajaDiario(CajaDiarioId). Paridad CajaDiarioBL.ConciliarCajaDiario / CreditoController.ConciliarCajaDiario.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/recalcular-caja-diario",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RecalcularCajaDiarioRequest body,
                    ICajaDiarioOperacionWriteService cajaDiarioOperacion,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("RecalcularCajaDiario");
                    try
                    {
                        var response = await cajaDiarioOperacion
                            .RecalcularCajaDiarioAsync(body.CajaDiarioId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RecalcularCajaDiario");
                        var detail = "No se pudo recalcular la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRecalcularCajaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_RecalcularCajaDiario(CajaDiarioId). Mantenimiento de saldos de caja diario (edmx / inventario Fase 0).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-saldos-caja-diario",
                async Task<Results<Ok<object>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirSaldosCajaDiarioRequest body,
                    ICajaDiarioTransferWriteService transferWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importe debe ser > 0.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Descripcion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "descripcion es obligatoria.");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("TransferirSaldosCajaDiario");
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

                        var (error, success) = await transferWrite
                            .TransferirSaldosAsync(
                                body.OficinaId,
                                body.CajaDiarioId,
                                usuarioId,
                                body.Importe,
                                body.Descripcion,
                                body.CajaIdDestino,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (!success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error ?? "Error al grabar.");
                        }

                        return TypedResults.Ok((object)new { success = true });
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
                        log.LogError(ex, "Error al transferir saldos caja diario");
                        var detail = "No se pudo transferir saldos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirSaldosCajaDiario")
            .WithSummary(
                "Escritura: paridad CajaDiarioController.TransferirSaldos (cajaâ†’caja o cajaâ†’bóveda).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/confirmar-clave-caja-diario",
                async Task<Results<Ok<ConfirmarClaveCajaDiarioResponse>, ProblemHttpResult>> (
                    ConfirmarClaveCajaDiarioRequest body,
                    IConfirmarClaveCajaDiarioService confirmarClave,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ConfirmarClaveCajaDiario");
                    try
                    {
                        var response = await confirmarClave
                            .VerificarAsync(body.Clave ?? string.Empty, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al confirmar clave caja diario");
                        var detail = "No se pudo validar la clave.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoConfirmarClaveCajaDiario")
            .WithSummary("Paridad CajaDiarioController.ConfirmarClave (usuario ADMVENDIX).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ConfirmarClaveCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-anular-movimiento-caja",
                async Task<Results<Ok<ValidarAnularMovimientoCajaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int movimientoCajaId,
                    IMovimientoCajaScopeReadService movimientoScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(oficinaId, movimientoCajaId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarAnularMovimientoCaja");
                    try
                    {
                        var (scopeError, _) = await CajaCreditoWriteGuards
                            .ValidateMovimientoOficinaAsync(oficinaId, movimientoCajaId, movimientoScope, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                        {
                            return scopeError;
                        }

                        var requiere = await movimientoScope
                            .RequiereConfirmacionAnularAsync(movimientoCajaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new ValidarAnularMovimientoCajaResponse(requiere));
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
                        log.LogError(ex, "Error al validar anulación de movimiento de caja");
                        var detail = "No se pudo validar la anulación del movimiento.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarAnularMovimientoCaja")
            .WithSummary(
                "Lectura previa: paridad CreditoController.ValidarAnularMovimientoCaja (INI con cuotas PAG requiere confirmación en UI).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarAnularMovimientoCajaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/anular-movimiento-caja",
                async Task<Results<Ok<AnularMovimientoCajaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AnularMovimientoCajaRequest body,
                    IMovimientoCajaScopeReadService movimientoScope,
                    IMovimientoCajaWriteService movimientoCajaWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(body.OficinaId, body.MovimientoCajaId);
                    if (idError is not null)
                    {
                        return idError;
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

                    var log = loggerFactory.CreateLogger("AnularMovimientoCaja");
                    try
                    {
                        var (scopeError, scope) = await CajaCreditoWriteGuards
                            .ValidateMovimientoOficinaAsync(body.OficinaId, body.MovimientoCajaId, movimientoScope, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                        {
                            return scopeError;
                        }

                        var anulableError = CajaCreditoWriteGuards.ValidateMovimientoAnulable(scope!);
                        if (anulableError is not null)
                        {
                            return anulableError;
                        }

                        var observacion = body.Observacion?.Trim() ?? string.Empty;
                        var response = await movimientoCajaWrite
                            .AnularAsync(body.MovimientoCajaId, observacion, usuarioId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_MovimientoCaja_Del");
                        var detail = "No se pudo anular el movimiento de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAnularMovimientoCaja")
            .WithSummary(
                "Escritura: CREDITO.usp_MovimientoCaja_Del. Paridad CajaDiarioBL.AnularMovimientoCaja / CreditoController.AnularMovimientoCaja.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAnularMovimientoCaja)
            .Produces<AnularMovimientoCajaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/desembolsos-pendientes",
                async Task<Results<Ok<List<DesembolsoPendienteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    IDesembolsoReadService desembolsoRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    if (personaId < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "personaId debe ser >= 0.");
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

                    var log = loggerFactory.CreateLogger("DesembolsosPendientes");
                    try
                    {
                        var items = await desembolsoRead
                            .ListarPendientesAsync(oficinaId, usuarioId, personaId, ct)
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
                        log.LogError(ex, "Error al listar desembolsos pendientes");
                        var detail = "No se pudo listar desembolsos pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoDesembolsosPendientes")
            .WithSummary(
                "Solo lectura: créditos APR pendientes de desembolso (paridad CajaDiarioBL.LstDesembolsoJGrid). personaId=0 filtra por usuario del token.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<DesembolsoPendienteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-desembolso",
                async Task<Results<Ok<ValidarDesembolsoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int cajaDiarioId,
                    int creditoId,
                    IDesembolsoReadService desembolsoRead,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(oficinaId, cajaDiarioId, creditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            oficinaId, cajaDiarioId, creditoId, cajaDiarioOficina, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarDesembolso");
                    try
                    {
                        var response = await desembolsoRead.ValidarAsync(cajaDiarioId, creditoId, ct).ConfigureAwait(false);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar desembolso");
                        var detail = "No se pudo validar el desembolso.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarDesembolso")
            .WithSummary("Lectura previa: paridad CreditoController.ValidarDesembolso (CxC PEN y saldo caja).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarDesembolsoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/realizar-desembolso",
                async Task<Results<Ok<RealizarDesembolsoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RealizarDesembolsoRequest body,
                    IDesembolsoReadService desembolsoRead,
                    IDesembolsoWriteService desembolsoWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId, body.CajaDiarioId, body.CreditoId, cajaDiarioOficina, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("RealizarDesembolso");
                    try
                    {
                        var validacion = await desembolsoRead
                            .ValidarAsync(body.CajaDiarioId, body.CreditoId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeDesembolsar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Desembolso no permitido",
                                detail: validacion.Mensaje ?? "Validación de desembolso fallida.");
                        }

                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await desembolsoWrite
                            .RealizarAsync(body.CajaDiarioId, body.CreditoId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        log.LogWarning(ex, "Crédito no encontrado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Desembolso rechazado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Desembolso no permitido",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
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
                        log.LogError(ex, "Error al realizar desembolso");
                        var detail = "No se pudo realizar el desembolso.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRealizarDesembolso")
            .WithSummary(
                "Escritura: paridad CajaDiarioBL.RealizarDesembolso (UPDATE Credito DES + MovimientoCaja + saldos caja). Idempotente si ya existe movimiento DES.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<RealizarDesembolsoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/entrada-salida-caja-diario",
                async Task<Results<Ok<EntradaSalidaCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EntradaSalidaCajaDiarioRequest body,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IEntradaSalidaCajaDiarioWriteService entradaSalidaWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Descripcion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "descripcion es obligatoria.");
                    }

                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.PersonaId < 1 || body.TipoOperacionId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "personaId y tipoOperacionId deben ser enteros >= 1.");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("EntradaSalidaCajaDiario");
                    try
                    {
                        var preError = await CajaCreditoWriteGuards.ValidateEntradaSalidaPrecondicionesAsync(
                            body.CajaDiarioId,
                            body.TipoOperacionId,
                            body.Importe,
                            body.Descripcion,
                            body.TipoPagoId,
                            entradaSalidaRead,
                            ct).ConfigureAwait(false);
                        if (preError is not null)
                        {
                            return preError;
                        }

                        var response = await entradaSalidaWrite
                            .EjecutarAsync(
                                body.CajaDiarioId,
                                body.PersonaId,
                                body.TipoOperacionId,
                                body.Importe,
                                body.Descripcion!.Trim(),
                                usuarioId,
                                body.TipoPagoId,
                                ct)
                            .ConfigureAwait(false);

                        var resultError = CajaCreditoWriteGuards.MapEntradaSalidaResult(response);
                        if (resultError is not null)
                        {
                            return resultError;
                        }

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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_EntradaSalidaCajaDiario");
                        var detail = "No se pudo registrar la entrada/salida de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEntradaSalidaCajaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_EntradaSalidaCajaDiario. Paridad CajaDiarioBL.EntradaSalida / RealizarEntradaSalidaCajaDiario. Param SQL Decripcion (typo legado).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<EntradaSalidaCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cuentas-por-cobrar-pendientes",
                async Task<Results<Ok<List<CuentaPorCobrarPendienteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int cajaDiarioId,
                    int personaId,
                    ICuentaPorCobrarPagoReadService cuentaPorCobrarPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || cajaDiarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y cajaDiarioId deben ser enteros >= 1.");
                    }

                    if (personaId < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "personaId debe ser >= 0.");
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

                    var log = loggerFactory.CreateLogger("CuentasPorCobrarPendientes");
                    try
                    {
                        var items = await cuentaPorCobrarPago
                            .ListarPendientesAsync(oficinaId, cajaDiarioId, usuarioId, personaId, ct)
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
                        log.LogError(ex, "Error al listar cuentas por cobrar pendientes");
                        var detail = "No se pudo listar cuentas por cobrar pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCuentasPorCobrarPendientes")
            .WithSummary(
                "Solo lectura: paridad CajaDiarioBL.LstCuentasxCobrarJGrid (CxC PEN + orden CON/ENV si personaId > 0). Requiere cajaDiarioId para regla CAJA CENTRAL.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CuentaPorCobrarPendienteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuenta-por-cobrar",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuentaxCobrarRequest body,
                    ICuentaPorCobrarPagoReadService cuentaPorCobrarPagoRead,
                    ICuentaPorCobrarPagoWriteService cuentaPorCobrarPagoWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.CuentaxCobrarId < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "cuentaxCobrarId debe ser >= 0.");
                    }

                    if (body.CuentaxCobrarId == 0 && body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "ordenVentaId debe ser >= 1 cuando cuentaxCobrarId es 0.");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("PagarCuentaxCobrar");
                    try
                    {
                        if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Conflicto",
                                detail: "La caja diario está cerrada.");
                        }

                        var puedeCobrar = await cuentaPorCobrarPagoRead
                            .PuedeCobrarAsync(body.OficinaId, body.OrdenVentaId, body.CuentaxCobrarId, ct)
                            .ConfigureAwait(false);
                        if (!puedeCobrar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "La cuenta por cobrar u orden de venta no existe, no está pendiente o no pertenece a la oficina.");
                        }

                        var response = await cuentaPorCobrarPagoWrite
                            .PagarAsync(body.OrdenVentaId, body.CuentaxCobrarId, body.CajaDiarioId, usuarioId, ct)
                            .ConfigureAwait(false);

                        var resultError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (resultError is not null)
                        {
                            return resultError;
                        }

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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuentaxCobrar");
                        var detail = "No se pudo registrar el pago de cuenta por cobrar.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuentaxCobrar")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuentaxCobrar. Paridad CajaDiarioBL.RealizarPagarCuentaxCobrar. cuentaxCobrarId=0 = orden venta CON.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/boveda-abierta",
                async Task<Results<Ok<BovedaAbiertaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaAbiertaReadService bovedaAbierta,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("BovedaAbierta");
                    try
                    {
                        var boveda = await bovedaAbierta
                            .ObtenerAbiertaPorOficinaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        if (boveda is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe bóveda abierta para la oficina.");
                        }

                        return TypedResults.Ok(boveda);
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
                        log.LogError(ex, "Error al consultar bóveda abierta");
                        var detail = "No se pudo consultar la bóveda abierta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoBovedaAbierta")
            .WithSummary(
                "Solo lectura: bóveda abierta de la oficina (IndCierre=0), ORDER BY IndTemporal, BovedaId â€” paridad operaciones de escritura y BovedaController.Index.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaAbiertaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/bovedas-destino-transferencia",
                async Task<Results<Ok<List<BovedaDestinoTransferenciaDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaOficinaReadService bovedaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("BovedasDestinoTransferencia");
                    try
                    {
                        var rows = await bovedaOficina
                            .ListarDestinosTransferenciaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(rows.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Bóvedas destino transferencia: configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Bóvedas destino transferencia: parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar bóvedas destino para transferencia");
                        var detail = "No se pudo listar bóvedas destino para transferencia.";
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
            .WithName("CreditoBovedasDestinoTransferencia")
            .WithSummary("Bóvedas principales abiertas de otras oficinas para transferencia interoficina.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<BovedaDestinoTransferenciaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/boveda-estado-dinero",
                async Task<Results<Ok<BovedaEstadoDineroDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaAbiertaReadService bovedaAbierta,
                    IBovedaEstadoDineroReadService estadoDinero,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("BovedaEstadoDinero");
                    try
                    {
                        var boveda = await bovedaAbierta.ObtenerAbiertaPorOficinaAsync(oficinaId, ct).ConfigureAwait(false);
                        var saldo = boveda?.SaldoFinal ?? 0m;
                        var dto = await estadoDinero
                            .ObtenerAsync(oficinaId, saldo, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Boveda estado dinero");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Boveda estado dinero");
                        var detail = "No se pudo obtener el estado de dinero.";
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
            .WithName("CreditoBovedaEstadoDinero")
            .WithSummary("Paridad Boveda/Index â€” KPIs estado de dinero y total fondo.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaEstadoDineroDto>(StatusCodes.Status200OK, "application/json");



        app.MapGet(
                "/api/v1/credito/boveda-cuadre-preview",
                async Task<Results<Ok<BovedaCuadrePreviewDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int? bovedaId,
                    IBovedaCuadrePreviewReadService cuadreRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || bovedaId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1 y bovedaId, si se envía, debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("BovedaCuadrePreview");
                    try
                    {
                        var preview = await cuadreRead
                            .ObtenerAsync(oficinaId, bovedaId, ct)
                            .ConfigureAwait(false);
                        if (preview is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe bóveda para generar el cuadre.");
                        }

                        return TypedResults.Ok(preview);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cuadre bóveda: configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Cuadre bóveda: parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al generar cuadre automático de bóveda");
                        var detail = "No se pudo generar el cuadre automático de bóveda.";
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
            .WithName("CreditoBovedaCuadrePreview")
            .WithSummary(
                "Solo lectura: cuadre automático de Bóveda/Caja con responsables, medios, diferencias y pendientes para reemplazar el Excel manual.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaCuadrePreviewDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/boveda-listar",
                async Task<Results<Ok<BovedaListadoResultDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int? page,
                    int? pageSize,
                    IBovedaListadoReadService listado,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("BovedaListar");
                    try
                    {
                        var (rows, total) = await listado
                            .ListarAsync(oficinaId, page ?? 1, pageSize ?? 5, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new BovedaListadoResultDto(rows.ToList(), total, page ?? 1, pageSize ?? 5));
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Boveda listar");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Boveda listar");
                        var detail = "No se pudo listar bóvedas.";
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
            .WithName("CreditoBovedaListar")
            .WithSummary("Paridad ListarBovedaJgrid â€” historial de cierres de bóveda.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaListadoResultDto>(StatusCodes.Status200OK, "application/json");



        app.MapGet(
                "/api/v1/credito/existe-boveda-temporal",
                async Task<Results<Ok<ExisteBovedaTemporalResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaOficinaReadService bovedaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ExisteBovedaTemporal");
                    try
                    {
                        var existe = await bovedaOficina
                            .ExisteBovedaTemporalAbiertaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new ExisteBovedaTemporalResponse(existe));
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
                        log.LogError(ex, "Error al consultar bóveda temporal");
                        var detail = "No se pudo consultar la bóveda temporal.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoExisteBovedaTemporal")
            .WithSummary("Solo lectura: paridad BovedaController.ExisteBovedaTemporal (bóveda abierta IndTemporal=1).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ExisteBovedaTemporalResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



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
                            title: "Solicitud inválida",
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
                            .ValidarCierreMasivoAsync(body.OficinaId, ct)
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CerrarBoveda");
                        var detail = "No se pudo cerrar la bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarBoveda")
            .WithSummary("Escritura: CREDITO.usp_CerrarBoveda. Paridad BovedaBL.Cerrar.")
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
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
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
                        var response = await bovedaWrite
                            .CerrarBovedaTemporalAsync(body.OficinaId, usuarioId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CerrarBovedaTemporal");
                        var detail = "No se pudo cerrar la bóveda temporal.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarBovedaTemporal")
            .WithSummary("Escritura: CREDITO.usp_CerrarBovedaTemporal. Paridad BovedaBL.CerrarBovedaTemporal.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-boveda",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaRequest body,
                    IBovedaWriteService bovedaWrite,
                    IBovedaOficinaReadService bovedaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
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

                    var aceptacionTemporal = body.BovedaMovTempId > 0;
                    if (!aceptacionTemporal)
                    {
                        if (body.BovedaInicioId < 1 || body.BovedaDestinoId < 1)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Solicitud inválida",
                                detail: "bovedaInicioId y bovedaDestinoId deben ser >= 1.");
                        }

                        if (body.Monto <= 0)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Solicitud inválida",
                                detail: "monto debe ser > 0.");
                        }

                        if (string.IsNullOrWhiteSpace(body.Glosa))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Solicitud inválida",
                                detail: "glosa es obligatoria.");
                        }
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
                                    detail: "No existe la bóveda de origen indicada.");
                            }

                            if (oficinaInicio.Value != body.OficinaId)
                            {
                                return TypedResults.Problem(
                                    statusCode: StatusCodes.Status403Forbidden,
                                    title: "Prohibido",
                                    detail: "La bóveda de origen no pertenece a la oficina del token.");
                            }

                            var oficinaDestino = await bovedaOficina
                                .GetOficinaIdByBovedaIdAsync(body.BovedaDestinoId, ct)
                                .ConfigureAwait(false);
                            if (oficinaDestino is null)
                            {
                                return TypedResults.Problem(
                                    statusCode: StatusCodes.Status404NotFound,
                                    title: "No encontrado",
                                    detail: "No existe la bóveda de destino indicada.");
                            }
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
                                ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_TransferirBoveda");
                        var detail = "No se pudo transferir entre bóvedas.";
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
                "Escritura: CREDITO.usp_TransferirBoveda. Transferencia entre bóvedas (origen en oficina JWT) o aceptación temporal (bovedaMovTempId > 0).")
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
                            title: "Solicitud inválida",
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
                            title: "Solicitud inválida",
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
                        var monto = await cajaAsignacion
                            .ObtenerMontoBovedaAsignacionAsync(oficinaId, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new MontoBovedaAsignacionResponse(monto));
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
                        log.LogError(ex, "Error al obtener monto de bóveda");
                        var detail = "No se pudo obtener el monto de bóveda.";
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
                "Solo lectura: paridad SaldosController.MostrarMontoBoveda (bóveda temporal si rol ENCARGADO).")
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
                            title: "Solicitud inválida",
                            detail: "oficinaId y cajaId deben ser enteros >= 1.");
                    }

                    if (body.SaldoInicial < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
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
                "Escritura: paridad CajaDiarioBL.AsignarUsuarioCaja / SaldosController.AsignarCaja (EF + usp_CalcularMontoPorCobrar). Cajero desde Caja.CajeroId.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<AsignarCajaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-cierre-saldos",
                async Task<Results<Ok<ValidarCierreSaldosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreSaldos");
                    try
                    {
                        var response = await saldosCierre
                            .ValidarCierreMasivoAsync(oficinaId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre masivo de saldos");
                        var detail = "No se pudo validar el cierre masivo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreSaldos")
            .WithSummary(
                "Solo lectura: paridad SaldosController.ValidarCierre (previo a cerrar-cajas-diarios / Transferir).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreSaldosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/saldos-caja-diario",
                async Task<Results<Ok<List<SaldoCajaSesionRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCajaDiarioReadService saldos,
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

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("SaldosCajaDiario");
                    try
                    {
                        var items = await saldos.ListarCajaDiarioPorOficinaAsync(oficinaId, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar saldos caja diario");
                        var detail = "No se pudo listar saldos de caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSaldosCajaDiario")
            .WithSummary("Paridad SaldosController.ListarSaldoCajaDiario.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<SaldoCajaSesionRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/saldos-caja-chica-diario",
                async Task<Results<Ok<List<SaldoCajaSesionRowDto>>, ProblemHttpResult>> (
                    ISaldosCajaDiarioReadService saldos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("SaldosCajaChicaDiario");
                    try
                    {
                        var items = await saldos.ListarCajaChicaDiarioAsync(ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar saldos caja chica diario");
                        var detail = "No se pudo listar saldos de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSaldosCajaChicaDiario")
            .WithSummary("Paridad SaldosController.ListarSaldoCajaChicaDiario.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<SaldoCajaSesionRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/saldos-caja-diario-boveda",
                async Task<Results<Ok<List<SaldoCajaSesionRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int bovedaId,
                    int oficinaId,
                    ISaldosCajaDiarioReadService saldos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (bovedaId < 1 || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "bovedaId y oficinaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("SaldosCajaDiarioBoveda");
                    try
                    {
                        var items = await saldos.ListarCajaDiarioBovedaAsync(bovedaId, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar saldos caja diario bóveda");
                        var detail = "No se pudo listar saldos por bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSaldosCajaDiarioBoveda")
            .WithSummary("Paridad SaldosController.ListarSaldoCajaDiarioBoveda.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<SaldoCajaSesionRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-cierre-caja-chica",
                async Task<Results<Ok<ValidarCierreSaldosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreCajaChica");
                    try
                    {
                        var response = await saldosCierre
                            .ValidarCierreCajaChicaAsync(oficinaId, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre caja chica");
                        var detail = "No se pudo validar el cierre de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreCajaChica")
            .WithSummary(
                "Solo lectura: valida cierre de caja chica filtrado por oficina.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreSaldosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-datos-post-cierre-boveda",
                async Task<Results<Ok, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarDatosPostCierreBovedaRequest body,
                    ISaldosCierreWriteService saldosCierreWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ActualizarDatosPostCierreBoveda");
                    try
                    {
                        await saldosCierreWrite
                            .ActualizarDatosPostCierreBovedaAsync(body.OficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok();
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
                        log.LogError(ex, "Error al actualizar datos post cierre bóveda");
                        var detail = "No se pudieron actualizar saldos de cartera ni calificación.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarDatosPostCierreBoveda")
            .WithSummary(
                "Escritura: CREDITO.usp_ActualizarSaldoCartera + usp_CalificarCliente. Paridad CajaDiarioBL.ActualizarDatosPostCierreBoveda.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cajas-abiertas-transferencia-boveda",
                async Task<Results<Ok<List<CajaAbiertaTransferenciaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaMovReadService bovedaMov,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("CajasAbiertasTransferenciaBoveda");
                    try
                    {
                        var items = await bovedaMov
                            .ListarCajasAbiertasParaTransferenciaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar cajas abiertas");
                        var detail = "No se pudo listar cajas abiertas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCajasAbiertasTransferenciaBoveda")
            .WithSummary("Solo lectura: paridad CajaBL.ListarCajasAbiertas (combo transferir bóveda â†’ caja).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CajaAbiertaTransferenciaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/ingreso-egreso-boveda",
                async Task<Results<Ok<BovedaMovOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IngresoEgresoBovedaRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.TipoOperacionId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y tipoOperacionId deben ser >= 1.");
                    }

                    if (body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importe debe ser > 0.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Descripcion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "descripcion es obligatoria.");
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

                    var log = loggerFactory.CreateLogger("IngresoEgresoBoveda");
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

                        var result = await bovedaMovWrite
                            .IngresoEgresoAsync(
                                body.OficinaId,
                                body.Importe,
                                body.Descripcion,
                                body.TipoOperacionId,
                                body.TipoPagoId,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (result is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo registrar",
                                detail: "Tipo de operación inválido, bóveda no disponible o error de negocio.");
                        }

                        return TypedResults.Ok(result);
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
                        log.LogError(ex, "Error en ingreso/egreso bóveda");
                        var detail = "No se pudo registrar ingreso/egreso de bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoIngresoEgresoBoveda")
            .WithSummary(
                "Escritura: paridad BovedaMovBL.IngresoEgresoBovedaCaja + usp_ActualizarSaldosBoveda. Tipo operación debe tener IndBoveda.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMovOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-boveda-caja",
                async Task<Results<Ok<BovedaMovOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaCajaRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.CajaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y cajaId deben ser >= 1.");
                    }

                    if (body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importe debe ser > 0.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Descripcion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "descripcion es obligatoria.");
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

                    var log = loggerFactory.CreateLogger("TransferirBovedaCaja");
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

                        var result = await bovedaMovWrite
                            .TransferirACajaAsync(
                                body.OficinaId,
                                body.CajaId,
                                body.Importe,
                                body.Descripcion,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (result is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: "Bóveda o caja diario no disponible en la oficina.");
                        }

                        return TypedResults.Ok(result);
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
                        log.LogError(ex, "Error al transferir bóveda a caja");
                        var detail = "No se pudo transferir de bóveda a caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirBovedaCaja")
            .WithSummary(
                "Escritura: paridad BovedaMovBL.TransferirBovedaCaja (BovedaMov TRS + MovimientoCaja TRE + usp_ActualizarSaldosBoveda).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMovOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-boveda-caja-chica",
                async Task<Results<Ok<BovedaMovOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaCajaChicaRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    if (body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importe debe ser > 0.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Descripcion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "descripcion es obligatoria.");
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

                    var log = loggerFactory.CreateLogger("TransferirBovedaCajaChica");
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

                        var (error, result) = await bovedaMovWrite
                            .TransferirACajaChicaAsync(
                                body.OficinaId,
                                body.Importe,
                                body.Descripcion,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al transferir bóveda a caja chica");
                        var detail = "No se pudo transferir de bóveda a caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirBovedaCajaChica")
            .WithSummary(
                "Escritura: paridad BovedaMovBL.TransferirBovedaCajaChica. Caja chica abierta sin filtro oficina (como MVC).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMovOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-cierre-caja-chica",
                async Task<Results<Ok<TransferirCierreCajaChicaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirCierreCajaChicaRequest body,
                    ICajaChicaDiarioWriteService cajaChicaDiarioWrite,
                    ISaldosCierreReadService saldosCierre,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
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

                    var validacion = await saldosCierre.ValidarCierreCajaChicaAsync(body.OficinaId, ct).ConfigureAwait(false);
                    if (!validacion.PuedeCerrar)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: validacion.Mensaje);
                    }

                    var log = loggerFactory.CreateLogger("TransferirCierreCajaChica");
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

                        var (error, result) = await cajaChicaDiarioWrite
                            .TransferirCierreABovedaAsync(body.OficinaId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al transferir cierre caja chica a bóveda");
                        var detail = "No se pudo transferir el cierre de caja chica a bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirCierreCajaChica")
            .WithSummary(
                "Escritura: paridad CajaChicaDiarioBL.TransferirCajaChicaDiarioBoveda. Requiere validar-cierre-caja-chica OK.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TransferirCierreCajaChicaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/asignar-boveda-temporal",
                async Task<Results<Ok<AsignarBovedaTemporalResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AsignarBovedaTemporalRequest body,
                    IBovedaTemporalWriteService bovedaTemporalWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    if (body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importe debe ser > 0.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Descripcion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "descripcion es obligatoria.");
                    }

                    if (body.UsuarioId < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "usuarioId debe ser >= 0.");
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

                    var log = loggerFactory.CreateLogger("AsignarBovedaTemporal");
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

                        var (error, result) = await bovedaTemporalWrite
                            .AsignarOTransferirAsync(
                                body.OficinaId,
                                body.Importe,
                                body.Descripcion,
                                body.UsuarioId,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo asignar bóveda temporal",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al asignar bóveda temporal");
                        var detail = "No se pudo asignar o transferir a bóveda temporal.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAsignarBovedaTemporal")
            .WithSummary(
                "Escritura: paridad BovedaController.AsignarBovedaTemporal. usuarioId>0 crea temporal+ENCARGADO; usuarioId=0 solo transferir.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<AsignarBovedaTemporalResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/entrada-salida-caja-chica-diario",
                async Task<Results<Ok<EntradaSalidaCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EntradaSalidaCajaChicaDiarioRequest body,
                    ICajaChicaEntradaSalidaWriteService cajaChicaEntradaSalida,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1 || body.TipoOperacionId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, personaId y tipoOperacionId deben ser >= 1.");
                    }

                    if (body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "importe debe ser > 0.");
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

                    var log = loggerFactory.CreateLogger("EntradaSalidaCajaChicaDiario");
                    try
                    {
                        var (error, result) = await cajaChicaEntradaSalida
                            .EjecutarAsync(
                                body.OficinaId,
                                body.PersonaId,
                                body.TipoOperacionId,
                                body.Importe,
                                body.Descripcion,
                                usuarioId,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo registrar movimiento",
                                detail: error);
                        }

                        if (result!.ResultCode < 0)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo registrar movimiento",
                                detail: $"El procedimiento devolvió código {result.ResultCode}.");
                        }

                        return TypedResults.Ok(result);
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
                        log.LogError(ex, "Error en entrada/salida caja chica");
                        var detail = "No se pudo registrar entrada/salida de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEntradaSalidaCajaChicaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_EntradaSalidaCajaChicaDiario. Caja chica abierta del usuario JWT. Parámetro SQL Decripcion.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<EntradaSalidaCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/caja-chica-sesion",
                async Task<Results<Ok<CajaChicaSesionDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CajaChicaSesion");
                    try
                    {
                        var sesion = await cajaChica.GetSesionAbiertaAsync(usuarioId, ct).ConfigureAwait(false);
                        if (sesion is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe caja chica abierta para el usuario.");
                        }

                        return TypedResults.Ok(sesion);
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
                        log.LogError(ex, "Error al obtener sesión caja chica");
                        var detail = "No se pudo obtener la sesión de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCajaChicaSesion")
            .WithSummary("Paridad CajaChicaController.Index / ObtenerCajaChicaDiario.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaChicaSesionDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/movimientos-caja-chica",
                async Task<Results<Ok<List<MovimientoCajaChicaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? tipo,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (tipo is not ("E" or "S"))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "tipo debe ser E (entrada) o S (salida).");
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("MovimientosCajaChica");
                    try
                    {
                        var items = await cajaChica.ListarMovimientosAsync(usuarioId, tipo, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar movimientos caja chica");
                        var detail = "No se pudo listar movimientos de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMovimientosCajaChica")
            .WithSummary("Paridad CajaChicaController.ListarMovimientosJGrid.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<MovimientoCajaChicaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rendiciones-pendientes-caja-chica",
                async Task<Results<Ok<List<RendicionPendienteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("RendicionesPendientesCajaChica");
                    try
                    {
                        var items = await cajaChica.ListarRendicionesPendientesAsync(usuarioId, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar rendiciones pendientes");
                        var detail = "No se pudo listar rendiciones pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRendicionesPendientesCajaChica")
            .WithSummary("Paridad CajaChicaController.ListarRencionesPendientesJGrid.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RendicionPendienteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rendiciones-caja-chica",
                async Task<Results<Ok<List<RendicionComprobanteRowDto>>, ProblemHttpResult>> (
                    int? movimientoCajaChicaId,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoCajaChicaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "movimientoCajaChicaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RendicionesCajaChica");
                    try
                    {
                        var items = await cajaChica
                            .ListarRendicionesAsync(movimientoCajaChicaId.Value, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar rendiciones");
                        var detail = "No se pudo listar comprobantes de rendición.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRendicionesCajaChica")
            .WithSummary("Paridad CajaChicaController.ListarRendicionGrd.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RendicionComprobanteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/tiene-rendiciones-pendientes-caja-chica",
                async Task<Results<Ok<int>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("TieneRendicionesPendientesCajaChica");
                    try
                    {
                        var count = await cajaChica.ContarRendicionesPendientesAsync(usuarioId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(count);
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
                        log.LogError(ex, "Error al contar rendiciones pendientes");
                        var detail = "No se pudo verificar rendiciones pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTieneRendicionesPendientesCajaChica")
            .WithSummary("Paridad CajaChicaController.TieneRencicionesPendientes.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<int>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/monto-caja-chica",
                async Task<Results<Ok<decimal>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("MontoCajaChica");
                    try
                    {
                        var saldo = await cajaChica.GetSaldoFinalSesionAbiertaAsync(usuarioId, ct).ConfigureAwait(false);
                        if (saldo is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe caja chica abierta.");
                        }

                        return TypedResults.Ok(saldo.Value);
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
                        log.LogError(ex, "Error al obtener monto caja chica");
                        var detail = "No se pudo obtener el saldo de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMontoCajaChica")
            .WithSummary("Paridad CajaChicaController.MostrarMontoCaja.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<decimal>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/rendiciones-caja-chica",
                async Task<Results<Ok, ProblemHttpResult>> (
                    CrearRendicionCajaChicaRequest body,
                    ICajaChicaRendicionWriteService rendiciones,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("CrearRendicionCajaChica");
                    try
                    {
                        var mensaje = await rendiciones.CrearAsync(body, ct).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(mensaje))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Rendición no registrada",
                                detail: mensaje);
                        }

                        return TypedResults.Ok();
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Solicitud inválida");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al crear rendición");
                        var detail = "No se pudo registrar la rendición.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCrearRendicionCajaChica")
            .WithSummary("Paridad CajaChicaController.CrearMovimientoRendicion.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapDelete(
                "/api/v1/credito/rendiciones-caja-chica/{rendicionId:int}",
                async Task<Results<Ok, ProblemHttpResult>> (
                    int rendicionId,
                    ICajaChicaRendicionWriteService rendiciones,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (rendicionId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "rendicionId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarRendicionCajaChica");
                    try
                    {
                        await rendiciones.DeleteAsync(rendicionId, ct).ConfigureAwait(false);
                        return TypedResults.Ok();
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
                        log.LogError(ex, "Error al eliminar rendición");
                        var detail = "No se pudo eliminar el comprobante.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEliminarRendicionCajaChica")
            .WithSummary("Paridad CajaChicaController.EliminarComprobanteCajaChica.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-rendicion-caja-chica",
                async Task<Results<Ok<CerrarRendicionCajaChicaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int movimientoCajaChicaId,
                    ICajaChicaRendicionWriteService rendiciones,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoCajaChicaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "movimientoCajaChicaId debe ser >= 1.");
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CerrarRendicionCajaChica");
                    try
                    {
                        var (error, result) = await rendiciones
                            .CerrarAsync(movimientoCajaChicaId, usuarioId, ct)
                            .ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo cerrar rendición",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al cerrar rendición");
                        var detail = "No se pudo cerrar la rendición.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarRendicionCajaChica")
            .WithSummary("Paridad CajaChicaController.CerrarRendicion.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CerrarRendicionCajaChicaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-caja-chica-diario",
                async Task<Results<Ok<CerrarCajaChicaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioWriteService cajaChicaWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CerrarCajaChicaDiario");
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

                        var (error, result) = await cajaChicaWrite
                            .CerrarSesionAsync(usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo cerrar caja chica",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al cerrar caja chica diario");
                        var detail = "No se pudo cerrar la caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarCajaChicaDiario")
            .WithSummary("Paridad CajaChicaController.CerrarCajaChicaDiario.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CerrarCajaChicaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-saldos-caja-chica-boveda",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirSaldosCajaChicaBovedaRequest body,
                    ICajaChicaDiarioWriteService cajaChicaWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.Importe <= 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId >= 1 e importe > 0 son obligatorios.");
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

                    var log = loggerFactory.CreateLogger("TransferirSaldosCajaChicaBoveda");
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

                        var (error, ok) = await cajaChicaWrite
                            .TransferirSaldosABovedaAsync(
                                body.OficinaId,
                                usuarioId,
                                body.Importe,
                                body.Descripcion,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Transferencia no completada",
                                detail: error);
                        }

                        return TypedResults.Ok(ok);
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
                        log.LogError(ex, "Error al transferir saldos caja chica a bóveda");
                        var detail = "No se pudo transferir a bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirSaldosCajaChicaBoveda")
            .WithSummary("Paridad CajaChicaController.TransferirBoveda / TransferirSaldosBoveda.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<bool>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-anular-credito",
                async Task<Results<Ok<ValidarAnularCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoAnulacionReadService creditoAnulacion,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarAnularCredito");
                    try
                    {
                        var response = await creditoAnulacion
                            .ValidarAnularAsync(creditoId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar anulación de crédito");
                        var detail = "No se pudo validar la anulación del crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarAnularCredito")
            .WithSummary("Lectura previa: paridad CreditoController.ValidarAnularCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarAnularCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/creditos-por-aprobar",
                async Task<Results<Ok<CreditosPorAprobarListResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    string? buscar,
                    int? page,
                    int? pageSize,
                    string? sortField,
                    string? sortOrder,
                    ICreditosPorAprobarReadService creditosPorAprobar,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var oficina = oficinaId.GetValueOrDefault();
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficina);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("CreditosPorAprobar");
                    try
                    {
                        var response = await creditosPorAprobar
                            .ListarAsync(
                                oficina,
                                buscar,
                                page is null or < 1 ? 1 : page.Value,
                                pageSize is null or < 1 ? 15 : pageSize.Value,
                                string.IsNullOrWhiteSpace(sortField) ? "Agente" : sortField,
                                string.IsNullOrWhiteSpace(sortOrder) ? "asc" : sortOrder,
                                ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar créditos por aprobar");
                        var detail = "No se pudo obtener el listado de créditos pendientes de aprobación.";
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
            .WithName("CreditoCreditosPorAprobar")
            .WithSummary(
                "Solo lectura: créditos Estado=PEN (paridad CreditoBL.LstCreditoAprobarJGrid / CreditoAprobarController).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditosPorAprobarListResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/aprobar-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AprobarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.Opcion is not (0 or 1))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "opcion debe ser 0 (primera aprobación) o 1 (segunda aprobación).");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("AprobarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .AprobarAsync(body.CreditoId, body.Opcion, usuarioId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_Credito_Upd");
                        var detail = "No se pudo aprobar el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAprobarCredito")
            .WithSummary(
                "Escritura: CREDITO.usp_Credito_Upd. opcion 0 = AprobarCredito1ra; 1 = AprobarCredito (paridad CreditoBL.AprobarCredito).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/anular-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AnularCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoAnulacionReadService creditoAnulacion,
                    ICreditoOficinaReadService creditoOficina,
                    IConfirmarClaveCajaDiarioService confirmarClave,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (string.IsNullOrWhiteSpace(body.Observacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "observacion es obligatoria.");
                    }

                    if (string.IsNullOrWhiteSpace(body.ClaveAutorizacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "claveAutorizacion es obligatoria.");
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("AnularCredito");
                    try
                    {
                        var autorizacion = await confirmarClave
                            .VerificarAsync(body.ClaveAutorizacion, ct)
                            .ConfigureAwait(false);
                        if (!autorizacion.Autorizado)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "No autorizado",
                                detail: autorizacion.Mensaje ?? "Clave de autorización no válida.");
                        }

                        var validacion = await creditoAnulacion
                            .ValidarAnularAsync(body.CreditoId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeAnular)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Anulación no permitida",
                                detail: "El crédito no cumple las condiciones para anular (paridad ValidarAnularCredito).");
                        }

                        var response = await creditoCiclo
                            .AnularAsync(body.CreditoId, body.Observacion, usuarioId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_Credito_Del");
                        var detail = "No se pudo anular el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAnularCredito")
            .WithSummary("Escritura: CREDITO.usp_Credito_Del. Requiere validar-anular-credito OK (409 si no).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/reprogramar-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ReprogramarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
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

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ReprogramarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .ReprogramarAsync(body.CreditoId, usuarioId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ReprogramarCredito");
                        var detail = "No se pudo reprogramar el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoReprogramarCredito")
            .WithSummary("Escritura: CREDITO.usp_ReprogramarCredito. Paridad CreditoBL.ReprogramarCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolSoloAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/prorrogar-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ProrrogarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.Dias < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "dias debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ProrrogarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .ProrrogarAsync(body.CreditoId, body.Dias, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ProrrogarCredito");
                        var detail = "No se pudo prorrogar el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoProrrogarCredito")
            .WithSummary("Escritura: CREDITO.usp_ProrrogarCredito. Paridad CreditoBL.ProrrogarCredito / CreditoController.ProrrogarCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/crear-solicitud-credito",
                async Task<Results<Ok<CrearSolicitudCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearSolicitudCreditoRequest body,
                    ICreditoSolicitudWriteService creditoSolicitud,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId deben ser >= 1.");
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

                    var log = loggerFactory.CreateLogger("CrearSolicitudCredito");
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

                        var response = await creditoSolicitud
                            .CrearSolicitudAsync(body.OficinaId, body.PersonaId, usuarioId, serverTime.Value, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al crear solicitud de crédito");
                        var detail = "No se pudo crear la solicitud de crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCrearSolicitudCredito")
            .WithSummary("Escritura EF: paridad CreditoBL.CrearSolicitudCredito (estado CRE). Devuelve solicitudCreditoId.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CrearSolicitudCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/crear-credito",
                async Task<Results<Ok<CrearCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoScopeReadService creditoScope,
                    IProductoReadService productos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.SolicitudCreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.ProductoId < 1 || body.NumeroCuotas < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "productoId y numeroCuotas deben ser >= 1.");
                    }

                    if (string.IsNullOrWhiteSpace(body.TipoCuota) || string.IsNullOrWhiteSpace(body.Modalidad)
                        || string.IsNullOrWhiteSpace(body.IndGastosAdm))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "tipoCuota, modalidad e indGastosAdm son obligatorios.");
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

                    var scope = await creditoScope.GetScopeAsync(body.SolicitudCreditoId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la solicitud de crédito indicada.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La solicitud no pertenece a la oficina del token JWT.");
                    }

                    if (!string.Equals(scope.Estado, "CRE", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: $"La solicitud no está en estado CRE (actual: {scope.Estado}).");
                    }

                    var log = loggerFactory.CreateLogger("CrearCredito");
                    try
                    {
                        var producto = await productos.GetActivoByIdAsync(body.ProductoId, ct).ConfigureAwait(false);
                        if (producto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Solicitud inválida",
                                detail: "El producto de crédito no existe o no está activo.");
                        }

                        if (body.InteresMensual < producto.InteresMinima
                            || body.InteresMensual > producto.InteresMaxima)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Solicitud inválida",
                                detail: $"El interés debe estar entre {producto.InteresMinima:N2}% y {producto.InteresMaxima:N2}%.");
                        }

                        var response = await creditoCiclo
                            .CrearDesdeSolicitudAsync(
                                body.SolicitudCreditoId,
                                body.ProductoId,
                                body.TipoCuota,
                                body.MontoInicial,
                                body.MontoCredito,
                                body.MontoGastosAdm,
                                body.IndGastosAdm,
                                body.Modalidad,
                                body.NumeroCuotas,
                                body.InteresMensual,
                                body.FechaPrimerPago,
                                body.Observacion ?? string.Empty,
                                usuarioId,
                                body.IndCentralRiesgo,
                                body.Prenda,
                                ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Solicitud inválida al generar crédito");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_Credito_Ins");
                        var detail = "No se pudo generar el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCrearCredito")
            .WithSummary(
                "Escritura: CREDITO.usp_Credito_Ins. Paridad GenerarCredito; solicitudCreditoId en body (no sesión MVC).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CrearCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/rechazar-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RechazarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoScopeReadService creditoScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scope = await creditoScope.GetScopeAsync(body.CreditoId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el crédito indicado.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El crédito no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("RechazarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .RechazarAsync(body.CreditoId, scope.OrdenVentaId, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al rechazar crédito");
                        var detail = "No se pudo rechazar el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRechazarCredito")
            .WithSummary(
                "Escritura: usp_OrdenVenta_Del o usp_SolicitudCredito_Del. Paridad CreditoBL.RechazarCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-cajas-diarios",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CerrarCajasDiariosRequest body,
                    ICajaDiarioOperacionWriteService cajaDiarioOperacion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
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

                    var log = loggerFactory.CreateLogger("CerrarCajasDiarios");
                    try
                    {
                        var response = await cajaDiarioOperacion
                            .CerrarCajasDiariosAsync(usuarioId, body.OficinaId, body.Sobrante, ct)
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CerrarCajasDiarios");
                        var detail = "No se pudo cerrar/transferir las cajas diarias de la oficina.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarCajasDiarios")
            .WithSummary(
                "Escritura: CREDITO.usp_CerrarCajasDiarios(UsuarioCierreId, OficinaId, Sobrante). usuarioCierreId = vendix:usuario_id. Paridad CajaDiarioBL.TransferirCajaDiarioBoveda / SaldosController.Transferir. Distinto de CerrarCajaDiario (EF compuesto en MVC).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    }
}
