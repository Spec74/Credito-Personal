using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Api.Admin;
using Credito.Modern.Api.Almacen;
using Credito.Modern.Api.Articulos;
using Credito.Modern.Api.Clientes;
using Credito.Modern.Api.Caja;
using Credito.Modern.Api.Dashboard;
using Credito.Modern.Api.Credito;
using Credito.Modern.Api.Maestros;
using Credito.Modern.Api.Prendario;
using Credito.Modern.Api.Ventas;
using Credito.Modern.Application.Auth;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Application.CreditoCartera;
using Credito.Modern.Application.CreditoTareas;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Documentos;
using Credito.Modern.Application.Marcas;
using Credito.Modern.Application.Menus;
using Credito.Modern.Application.Modelos;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Productos;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Api.Reportes;
using Credito.Modern.Application.TipoArticulos;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.TipoOperaciones;
using Credito.Modern.Application.Ubigeo;
using Credito.Modern.Application.Inventario;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Ocupaciones;
using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.ListaPrecios;
using Credito.Modern.Application.ValorTablas;
using Credito.Modern.Application.SerieArticulos;
using Credito.Modern.Application.Ventas;
using Credito.Modern.Infrastructure;
using Credito.Modern.Infrastructure.Auth;
using Credito.Modern.Infrastructure.CreditoPlanes;
using HealthChecks.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Credito Modern API", Version = "v1" });
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "JWT de acceso (audience de acceso) con claims vendix:oficina_id, vendix:usuario_id (opcional vendix:usuario_oficina_id) y roles MAESTRO en ClaimTypes.Role. Obtén pares access+refresh con POST /api/v1/auth/login o POST /api/v1/auth/refresh; en Development local opcionalmente POST /api/v1/dev/token (solo si Hosting:AllowDevToken es true).",
        });
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                },
                Array.Empty<string>()
            },
        });
});

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());
var sqlConnForHealth = builder.Configuration["CreditoDatabase:ConnectionString"];
if (!string.IsNullOrWhiteSpace(sqlConnForHealth))
{
    healthChecksBuilder.AddSqlServer(sqlConnForHealth, name: "database");
}

builder.Services.AddOptions<CreditoStorageOptions>()
    .BindConfiguration(CreditoStorageOptions.SectionName);
builder.Services.AddOptions<ArticuloStorageOptions>()
    .BindConfiguration(ArticuloStorageOptions.SectionName);

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(
        j => !string.IsNullOrWhiteSpace(j.SigningKey)
            && Encoding.UTF8.GetBytes(j.SigningKey).Length >= 32
            && j.AccessTokenLifetimeHours >= 1
            && j.AccessTokenLifetimeHours <= 168
            && !string.IsNullOrWhiteSpace(j.RefreshAudience)
            && j.RefreshTokenLifetimeDays >= 1
            && j.RefreshTokenLifetimeDays <= 90
            && j.RefreshTokenVersion >= 1
            && j.RefreshTokenVersion <= 999_999,
        "Jwt: SigningKey (â‰¥32 bytes UTF-8), AccessTokenLifetimeHours (1-168), RefreshAudience no vacío, RefreshTokenLifetimeDays (1-90) y RefreshTokenVersion (1-999999) son obligatorios.")
    .ValidateOnStart();

builder.Services.AddSingleton<JwtTokenIssuer>();
builder.Services.AddSingleton<RefreshTokenValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((options, jwtApp) =>
    {
        var j = jwtApp.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(j.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = j.Issuer,
            ValidateAudience = true,
            ValidAudience = j.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(CreditoAuthorizationPolicies.CreditoUser, static policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolAdministrador,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasAdministrador(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolEncargado,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasEncargado(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolAprobador1,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasAprobador1(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasAprobador1OAdministrador(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolSoloAdministrador,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasAdministrador(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasEncargadoOAdministrador(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolAnularMovimientoCaja,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasAnulacionMovimientoCaja(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolOperador,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasRolOperador(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoNoLecturaSaldo,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => !CreditoAuthorizationPolicies.HasLecturaSaldo(GetRoleClaims(ctx.User)));
        });
    options.AddPolicy(
        CreditoAuthorizationPolicies.CreditoRolPrendario,
        static policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx => CreditoAuthorizationPolicies.HasPrendario(GetRoleClaims(ctx.User)));
        });
});

static IEnumerable<string> GetRoleClaims(ClaimsPrincipal user) =>
    user.FindAll(ClaimTypes.Role).Select(static c => c.Value);

builder.Services.AddModernInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();
builder.AddCreditoHostSecurity();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Items["CorrelationId"] = correlationId;
    await next().ConfigureAwait(false);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCreditoHostSecurity();
app.UseAuthentication();
app.UseAuthorization();

static Task WriteHealthJson(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = System.Text.Json.JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
    });
    return context.Response.WriteAsync(payload);
}

// Liveness: proceso vivo (App Service / balanceador). No incluye SQL.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = static r => r.Name == "self",
    ResponseWriter = WriteHealthJson,
});

// Readiness: self + SQL si hay connection string.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = WriteHealthJson,
});

app.MapAuthEndpoints();
app.MapReportesCatalogoEndpoints();

app.MapClienteOperacionEndpoints();
app.MapPrendarioEndpoints();
app.MapCreditoTareasCrudEndpoints();
app.MapCreditoOperacionEndpoints();
app.MapCobroPlanillaBloqueEndpoints();
app.MapCierreGerencialEndpoints();
app.MapCreditoReportesRestantesEndpoints();
app.MapCobroDiarioMorosidadGestorEndpoints();
app.MapVentasEndpoints();
app.MapAlmacenOperacionEndpoints();

app.MapMaestroReadEndpoints();
app.MapMaestroCrudEndpoints();
app.MapArticuloCrudEndpoints();
app.MapCajaMaestroEndpoints();
app.MapUsuarioAdminEndpoints();
app.MapRolAdminEndpoints();
app.MapCreditoConfigEndpoints();
app.MapMovimientoCajaTicketEndpoints();
app.MapRptComprobantesCajaChicaEndpoints();
app.MapCreditoInformesLegacyEndpoints();
app.MapCobranzaPagosEndpoints();

app.MapCreditoTareasReportEndpoints();
app.MapDashboardAnalistaEndpoints();
app.MapDashboardAdminEndpoints();
app.MapCreditoCondonacionEndpoints();
app.MapBovedaTransferenciaBancosEndpoints();
app.MapClienteExtensionEndpoints();
app.MapAlmacenConstanciaEndpoints();
app.MapHostingEndpoints();

app.Run();

public partial class Program { }


