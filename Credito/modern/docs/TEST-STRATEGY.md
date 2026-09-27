# Estrategia de pruebas

Índice: [DOCUMENTACION.md](DOCUMENTACION.md).

## Capas

| Capa | Dónde | Qué cubre |
|------|-------|-----------|
| **API / contratos** | `Credito.Modern.Tests` | Endpoints, strangler contracts, políticas, ProblemDetails |
| **Validación** | Tests de reglas + `formRules.test.ts` | Money/string/id; reglas Ant Design |
| **SPA unit** | `Credito.Modern.Web` (`*.test.ts`) | Menú/rutas (`menuRouteAccess`), resolvers |
| **Smoke local** | `deploy/scripts/smoke-local-api.ps1` | health, 401, login/dev, me, menú, database-time |
| **Smoke strangler** | `smoke-strangler-proxy.ps1`, `verify-strangler-proxy.ps1` | Proxy 9080, SPA `/app/`, API detrás |
| **Preprod cutover** | `preprod-cutover-checklist.ps1` | Corte A / B (SSD-00) — **ejecución operativa** |
| **Config prod** | `verify-production-config.ps1` | Plantillas AllowDevToken / menú / CORS |

## Cómo correr

```powershell
cd Credito\modern
dotnet test Credito.Modern.Tests\Credito.Modern.Tests.csproj
cd Credito.Modern.Web
npm test -- --run
```

## Conteos

Los documentos citan **812** métodos `[Fact]`/`[Theory]` al **2026-09-27** (historial: 714 → 738 en 2026-09-11).  
Los `[Theory]` expanden más casos al ejecutar.

## Qué no sustituye

- Tests ≠ smoke con datos reales de preprod.
- Casillas `[ ]` en SSD §7 = criterios **escritos**; el check es evidencia de ejecución, no un hueco de documentación.
