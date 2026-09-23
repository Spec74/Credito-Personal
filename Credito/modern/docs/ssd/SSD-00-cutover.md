# SSD-00 — Cutover y strangler

**Estado:** as-built (código y runbooks) · Development verificado 2026-09-11 · piloto Azure App Service + Vercel verificado 2026-09-23 (login real + smoke JWT) · retiro MVC **no** firmado
**Código:** `deploy/`, nginx/IIS ARR, SPA `/app/`, API `/api/v1`  
**Fuente:** convivencia MVC (`Credito/Web`) + `Credito.Modern`  
**Doc de ingeniería:** [STRANGLER-MIGRATION.md](../migration/STRANGLER-MIGRATION.md), [DEPLOY-AL-SUBIR.md](../migration/DEPLOY-AL-SUBIR.md), [PHASE-5-OPERATIONS-CUTOVER.md](../migration/PHASE-5-OPERATIONS-CUTOVER.md), [MIGRATION-CLOSURE.md](../migration/MIGRATION-CLOSURE.md)

## 1. Propósito y actores

Poner la SPA y la API delante del MVC sin apagar el legado hasta que negocio firme cada rebanada.

| Rol | Uso |
|-----|-----|
| Informática | Proxy, secretos, SQL, smoke, rollback |
| Negocio (oficina piloto) | Login, menú, caja, crédito, un informe PDF |
| Desarrollo | No segundo `dotnet run` local (candado de DLL) |

## 2. Alcance

**Entra**

- Fachada única (nginx 9080 staging / `production.conf.example`): `/api/v1` y `/health` → API; `/app/` → SPA; resto → MVC
- Contratos Production: `AllowDevToken=false`, `Menu:PermiteParametrosQuery=false`, rate limit on, `Auth:RequerirClienteAcceso=true`
- Scripts: `smoke-local-api.ps1`, `smoke-strangler-proxy.ps1`, `verify-production-config.ps1`, `preprod-cutover-checklist.ps1`
- SQL versionado en `deploy/sql/` (incl. `ClaveUsuario` 256, condonación, TRF bancos)
- Rollback: reenrutar la rebanada al MVC; no borrar API/SPA

**No entra**

- Especificación funcional de cada módulo: [SSD-01](SSD-01-auth-inicio.md) … [SSD-11](SSD-11-almacen.md)
- Decisión de retirar RDLC (sigue opcional)
- Piloto de ventas/almacén en oficinas que no las tienen en menú (SSD-10 / SSD-11)
- Token WhatsApp de prueba Meta en producción

## 3. Paridad MVC

El strangler no cambia reglas: el MVC sigue sirviendo lo no piloto. `legacyRoutes.ts` y `spa-legacy-redirects.conf` mandan URLs antiguas a SPA cuando la rebanada está lista.

Hasta que se incluya `spa-legacy-redirects.conf` en nginx, el usuario puede seguir abriendo el MVC por URL directa.

## 4. Contrato de datos

Misma base CREDITO. Aplicar scripts de `deploy/sql/` **antes** de confiar en migración perezosa de claves, condonación o TRF bancos.

No hay `usp_*` de cutover. Reloj de negocio: `usp_FechaBD`.

## 5. API y SPA (despliegue)

| Pieza | Dónde |
|-------|--------|
| API | `dotnet` / Docker `credito-modern-api`; puerto debug 5080 en compose strangler |
| SPA | `npm run build` con `VITE_API_BASE_URL`, `VITE_BASE_URL=/app/`, `VITE_LEGACY_ORIGIN` si hace falta RDLC |
| Proxy | `deploy/docker-compose.strangler.yml`, `deploy/nginx/*`, `iis-arr-web.config.example` |
| Secretos | `deploy/.env*.example` — nunca `deploy/.env` en Git |

Staging compose: `Auth__MigracionClavePerezosa=true`, `Hosting__AllowDevToken=false`.

## 6. Seguridad

- JWT signing key ≥ 32 caracteres en prod.
- `dev/token` ausente fuera de Development.
- WhatsApp / ApiPerú / Maps por entorno, no el token de prueba Meta.
- Forwarded headers solo con `KnownProxies` reales (`verify-production-config.ps1`).

## 7. Criterios de aceptación (corte)

Checklist vivo: `deploy/scripts/preprod-cutover-checklist.ps1` (Corte A API+proxy, Corte B SPA).

- [x] `verify-production-config.ps1` OK contra plantillas Production y PreProduction del repo (sin `-RequireForwardedHeaders`; CORS sigue vacío a propósito)
- [x] Smoke Development: `smoke-local-api.ps1 -BaseUrl http://localhost:5288` (health, 401, `dev/token`, me, menú, `database-time`)
- [x] Tests `stranglerproductioncontracttests` / `stranglerstagingcontracttests`
- [x] Docker local (`credito-modern-current`, 2026-09-11): proxy 9080 + API 5080 healthy; `/health` 200; `X-Correlation-ID` eco; 401 en `/api/v1/*`; SPA `/app/` y `/app/login` 200; oficinas públicas 200; `dev/token` 404 (`AllowDevToken=false`); Swagger 404 (Staging). `smoke-strangler-proxy.ps1 -SkipJwt` OK.
- [x] MVC local en IIS Express :4779 detrás del proxy: `/Home/Index` 302 → `/Home/Login` (MVC); `/Home/Login` 302 → `/app/login` (nginx); `/Credito/FooNoExiste` 404 del legado (ya no 502).
- [x] Contenedor API recreado: `Auth__MigracionClavePerezosa=true` (alineado al YAML).
- [ ] Login SPA **real** por `http://localhost:9080/app/login` (mismo usuario CREDITO; este stack no tiene `dev/token`) y el equivalente en preprod + menú `usp_MenuLst`
- [ ] Smoke por rol en preprod: gestor, cajero, encargado, aprobador, admin (SSD-01…07)
- [ ] Un PDF Credix y, si negocio lo pide, el RDLC equivalente
- [ ] Proxy: `/api/v1/*` no cae al MVC; una URL MVC no migrada sigue en legado
- [ ] Rollback ensayado (reenrutar una rebanada)
- [ ] `ALTER ClaveUsuario nvarchar(256)` aplicado en **esa** base de preprod

El software es candidato. El corte en preproducción **no** está firmado.

## 8. Desviaciones

Ninguna financiera de cutover. Cifras viejas: `MIGRATION-CLOSURE.md` citaba **714** tests; al 2026-09-11 hay **738** métodos `[Fact]`/`[Theory]` (los `[Theory]` suman más casos al correr).

## 9. Pruebas y evidencia

- API: `stranglerproductioncontracttests`, `stranglerstagingcontracttests`, `healthendpointtests`
- 2026-09-11 Development: `verify-production-config.ps1`; `smoke-local-api.ps1` en `http://localhost:5288`; filtro Strangler 2/2
- 2026-09-11 Docker Desktop stack `credito-modern-current`: `verify-strangler-proxy.ps1` + `smoke-strangler-proxy.ps1 -SkipJwt`. API recreada (`MigracionClavePerezosa=true`). IIS Express en 4779: rutas no migradas llegan al MVC (404/302), no 502. Login JWT por proxy pendiente de usuario/clave reales.
- Runbook operativo: [PHASE-5-OPERATIONS-CUTOVER.md](../migration/PHASE-5-OPERATIONS-CUTOVER.md)

Eso no sustituye el smoke con datos reales de preprod.

## 10. Go-live

**Pendiente de ejecución.** Desarrollo: listo para piloto. Preprod: seguir Corte A → B; no retirar IIS MVC en el primer día.
