# Endurecimiento de plataforma (Credito.Modern)

**Estado:** as-built · 2026-10-08 · rama `Eber-migracion`  
Índice: [DOCUMENTACION.md](DOCUMENTACION.md)

Cierra el gap de “calidad de plataforma” (~4/5 → checklist de release).

## 1. CI

| Workflow | Qué hace |
|----------|----------|
| `.github/workflows/credito-modern-quality.yml` | `dotnet test` + `npm run quality` en pushes/PR a `Eber-migracion` (paths `Credito/modern/**`) |
| `.github/workflows/eber-migracion_crediconfiable-api.yml` | **Test API** antes de `dotnet publish` / deploy Azure |

## 2. Observabilidad

- OpenTelemetry (ASP.NET + HttpClient + Runtime): `Hosting/observabilityextensions.cs`
- Config: `OpenTelemetry:OtlpEndpoint`, `OpenTelemetry:ConsoleExporter`
- Logs con scope `CorrelationId` + cabecera `X-Correlation-ID`

## 3. Cabeceras de seguridad

- API: `UseCreditoSecurityHeaders` (`X-Content-Type-Options`, `X-Frame-Options`, CSP, etc.)
- SPA: meta CSP + nosniff en `index.html` (Vite / Vercel)

## 4. Tokens

- Access en **localStorage** (visor PDF multi-pestaña) + refresh en **sessionStorage** por defecto.
- Cookie httpOnly/BFF: fase futura (rompe el modelo actual del visor).
- Tests: `src/auth/tokenStorage.test.ts`

## 5. ACL shell

- `AppShell` no monta `<Outlet />` mientras carga el menú o los permisos ACL (cierre/morosos).
- Evita flash de contenido sin permiso.

## 6. E2E

- Playwright: `Credito.Modern.Web/e2e/smoke.spec.ts` + `playwright.config.ts`
- Local: `npm run build && npx playwright install chromium && npm run test:e2e`
- Login real: `CREDITO_E2E_USER` / `CREDITO_E2E_PASSWORD` / opcional `CREDITO_E2E_BASE_URL`

## 7. Gate BD

- `deploy/scripts/smoke-db-gate.ps1` — EsPrendario, Prenda, mora, MorosidadEmpresa, QUOTED_IDENTIFIER, ClaveUsuario 256

## 8. Responsive

- Checklist en [responsive-audit.md](responsive-audit.md) — firmar en preprod (~375px) antes del cutover.

## 9. Higiene

- `.gitignore`: `dist/`, `bin/`, `obj/`, coverage, playwright-report
