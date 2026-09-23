using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Application.Auth;
using Credito.Modern.Application.Time;
using Credito.Modern.Infrastructure.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Auth;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/database-time", async Task<Results<Ok<DatabaseTimeResponse>, ProblemHttpResult>> (
            IDatabaseTimeProvider clock,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("DatabaseTime");
            try
            {
                var databaseTime = await clock.GetServerTimeAsync(ct).ConfigureAwait(false);
                return TypedResults.Ok(new DatabaseTimeResponse(databaseTime));
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
                log.LogError(ex, "Error al abrir SQL o ejecutar usp_FechaBD");
                var detail =
                    "No se pudo conectar a SQL Server o falló la consulta. Revisa CreditoDatabase:ConnectionString (user-secrets), que la instancia esté en marcha y usuario/clave. En instancias locales a veces hace falta añadir Encrypt=False;TrustServerCertificate=True.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("DatabaseTime")
        .WithTags("read-only")
        .Produces<DatabaseTimeResponse>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost("/api/v1/auth/refresh", async Task<Results<Ok<LoginTokenResponse>, ProblemHttpResult>> (
            RefreshTokenRequest body,
            RefreshTokenValidator refreshValidator,
            ILegacyLoginService loginService,
            JwtTokenIssuer jwt,
            IOptions<JwtOptions> jwtOpts,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("AuthRefresh");
            if (string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Solicitud inválida",
                    detail: "refreshToken es obligatorio.");
            }

            if (!refreshValidator.TryValidate(body.RefreshToken, out var usuarioId, out var oficinaId, out var usuarioOficinaId))
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "No autorizado",
                    detail: "Refresh token inválido o expirado.");
            }

            try
            {
                var roles = await loginService.GetUsuarioRolesAsync(usuarioId, oficinaId, ct).ConfigureAwait(false);
                var accessLifetime = TimeSpan.FromHours(jwtOpts.Value.AccessTokenLifetimeHours);
                var refreshLifetime = TimeSpan.FromDays(jwtOpts.Value.RefreshTokenLifetimeDays);
                var accessToken = jwt.CreateAccessToken(usuarioId, oficinaId, accessLifetime, usuarioOficinaId, roles);
                var refreshToken = jwt.CreateRefreshToken(usuarioId, oficinaId, usuarioOficinaId);
                return TypedResults.Ok(
                    new LoginTokenResponse(
                        accessToken,
                        (int)accessLifetime.TotalSeconds,
                        refreshToken,
                        (int)refreshLifetime.TotalSeconds,
                        usuarioId,
                        oficinaId,
                        usuarioOficinaId ?? 0));
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
                log.LogError(ex, "Error SQL al refrescar roles");
                var detail = "No se pudo consultar roles en SQL Server.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("AuthRefresh")
        .WithTags("auth")
        .RequireRateLimiting("auth-refresh")
        .Produces<LoginTokenResponse>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost("/api/v1/auth/login", async Task<Results<Ok<LoginTokenResponse>, ProblemHttpResult>> (
            LoginRequest body,
            ILegacyLoginService loginService,
            JwtTokenIssuer jwt,
            IOptions<JwtOptions> jwtOpts,
            IOptions<AuthOptions> authOpts,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var log = loggerFactory.CreateLogger("AuthLogin");
            if (string.IsNullOrWhiteSpace(body.NombreUsuario) || string.IsNullOrWhiteSpace(body.Clave) || body.OficinaId < 1)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Solicitud inválida",
                    detail: "nombreUsuario, clave y oficinaId (> 0) son obligatorios.");
            }

            if (authOpts.Value.RequerirClienteAcceso && string.IsNullOrWhiteSpace(body.ClienteAcceso))
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Solicitud inválida",
                    detail: "clienteAcceso (tk) es obligatorio cuando Auth:RequerirClienteAcceso es true.");
            }

            try
            {
                var outcome = await loginService.TryAuthenticateAsync(body, ct).ConfigureAwait(false);
                if (outcome.Status == LegacyLoginStatus.ClienteAccesoDenegado)
                {
                    log.LogWarning(
                        "Login denegado por MAESTRO.Acceso (tk) para NombreUsuario={NombreUsuario} OficinaId={OficinaId}.",
                        body.NombreUsuario?.Trim() ?? string.Empty,
                        body.OficinaId);
                    return TypedResults.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Acceso no autorizado",
                        detail: "El cliente no está autorizado (MAESTRO.Acceso), igual que en el login MVC con tk inválido.");
                }

                if (outcome.Status != LegacyLoginStatus.Success)
                {
                    log.LogWarning(
                        "Login fallido (estado {Estado}) para NombreUsuario={NombreUsuario} OficinaId={OficinaId}.",
                        outcome.Status,
                        body.NombreUsuario?.Trim() ?? string.Empty,
                        body.OficinaId);
                    return TypedResults.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "No autorizado",
                        detail: "Usuario, clave u oficina no válidos.");
                }

                var accessLifetime = TimeSpan.FromHours(jwtOpts.Value.AccessTokenLifetimeHours);
                var refreshLifetime = TimeSpan.FromDays(jwtOpts.Value.RefreshTokenLifetimeDays);
                var accessToken = jwt.CreateAccessToken(
                    outcome.UsuarioId,
                    outcome.OficinaId,
                    accessLifetime,
                    outcome.UsuarioOficinaId,
                    outcome.Roles);
                var refreshToken = jwt.CreateRefreshToken(outcome.UsuarioId, outcome.OficinaId, outcome.UsuarioOficinaId);
                return TypedResults.Ok(
                    new LoginTokenResponse(
                        accessToken,
                        (int)accessLifetime.TotalSeconds,
                        refreshToken,
                        (int)refreshLifetime.TotalSeconds,
                        outcome.UsuarioId,
                        outcome.OficinaId,
                        outcome.UsuarioOficinaId));
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
                log.LogError(ex, "Error SQL en login legado");
                var detail = "No se pudo validar credenciales contra SQL Server.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("AuthLogin")
        .WithTags("auth")
        .RequireRateLimiting("auth-login")
        .Produces<LoginTokenResponse>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/auth/registrar-acceso-ip",
                async Task<Results<Ok, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IAccesoIpWriteService accesoWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString();
                    if (string.IsNullOrWhiteSpace(ip))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "No se pudo determinar la IP del cliente.");
                    }

                    var log = loggerFactory.CreateLogger("RegistrarAccesoIp");
                    try
                    {
                        await accesoWrite.RegistrarSiNoExisteAsync(ip, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al registrar acceso IP");
                        var detail = "No se pudo registrar la IP en MAESTRO.Acceso.";
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
            .WithName("AuthRegistrarAccesoIp")
            .WithSummary("Alta administrada de MAESTRO.Acceso usando la IP real del request.")
            .WithTags("auth")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/auth/me",
                async Task<Results<Ok<AuthMeResponse>, ProblemHttpResult>> (HttpContext httpContext) =>
                {
                    var user = httpContext.User;
                    var u = user.FindFirst(VendixClaims.UsuarioId)?.Value;
                    var o = user.FindFirst(VendixClaims.OficinaId)?.Value;
                    if (!int.TryParse(u, NumberStyles.Integer, CultureInfo.InvariantCulture, out var usuarioId)
                        || !int.TryParse(o, NumberStyles.Integer, CultureInfo.InvariantCulture, out var oficinaId)
                        || usuarioId < 1
                        || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene vendix:usuario_id y vendix:oficina_id válidos.");
                    }

                    int? usuarioOficinaId = null;
                    var uo = user.FindFirst(VendixClaims.UsuarioOficinaId)?.Value;
                    if (!string.IsNullOrEmpty(uo)
                        && int.TryParse(uo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uoi)
                        && uoi >= 1)
                    {
                        usuarioOficinaId = uoi;
                    }

                    var roles = user
                        .FindAll(ClaimTypes.Role)
                        .Select(static c => c.Value)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(static r => r, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    return TypedResults.Ok(new AuthMeResponse(usuarioId, oficinaId, usuarioOficinaId, roles));
                })
            .WithName("AuthMe")
            .WithTags("auth")
            .WithSummary("Devuelve usuario, oficina y roles del access JWT actual.")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<AuthMeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var runningInCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
        var hostingForDevToken = app.Configuration.GetSection(HostingPipelineOptions.SectionName).Get<HostingPipelineOptions>() ?? new();
        // AllowDevToken=false en Production/PreProduction. En Staging local el override
        // docker-compose.devtoken.override.yml activa el endpoint sin cambiar el environment
        // (así ui-config SPA de Staging sigue cargando).
        if (!app.Environment.IsProduction()
            && !app.Environment.IsEnvironment("PreProduction")
            && !runningInCi
            && hostingForDevToken.AllowDevToken)
        {
            app.MapPost(
                    "/api/v1/dev/token",
                    (DevTokenRequest body, JwtTokenIssuer issuer, IOptions<JwtOptions> jwtOpts) =>
                    {
                        if (body.UsuarioId < 1 || body.OficinaId < 1)
                        {
                            return Results.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Parámetros inválidos",
                                detail: "oficinaId y usuarioId deben ser enteros >= 1.");
                        }

                        var lifetime = TimeSpan.FromHours(jwtOpts.Value.AccessTokenLifetimeHours);
                        var token = issuer.CreateAccessToken(
                            body.UsuarioId,
                            body.OficinaId,
                            lifetime,
                            usuarioOficinaId: null,
                            roles: body.Roles);
                        return Results.Ok(new DevTokenResponse(token, (int)lifetime.TotalSeconds));
                    })
                .WithName("DevToken")
                .WithTags("auth")
                .WithSummary("Emite JWT de desarrollo (AllowDevToken; no Production/PreProduction/CI).")
                .Produces<DevTokenResponse>(StatusCodes.Status200OK, "application/json")
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }
    }
}
