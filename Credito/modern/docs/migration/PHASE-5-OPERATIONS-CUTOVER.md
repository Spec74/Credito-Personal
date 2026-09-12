# Fase 5 — Operaciones y cutover

Spec: [SSD-00-cutover.md](../ssd/SSD-00-cutover.md).  
Checklist imprimible: `deploy/scripts/preprod-cutover-checklist.ps1 -SkipInteractive`.

El MVC permanece detrás del proxy hasta que negocio firme cada rebanada. No se apaga IIS el primer día.

## Estado (2026-09-11)

| Comprobación | Resultado |
|--------------|-----------|
| Plantilla `appsettings.Production.json` | `verify-production-config.ps1` OK (`AllowDevToken=false`, menú query off, rate limit on). CORS vacío: hay que llenarlo en el servidor. |
| Plantilla PreProduction | Mismo contrato. `Ui:UseSpaForModule` / `DefaultLoginToSpa` siguen en **false** (piloto fase 1: API lista, login MVC hasta Corte B/C). |
| Tests strangler | `stranglerproductioncontracttests` + `stranglerstagingcontracttests` OK |
| Smoke Development | `smoke-local-api.ps1 -BaseUrl http://localhost:5288` (health, 401, `dev/token`, me, menu, `usp_FechaBD`) |
| Proxy Docker local (9080) | OK 2026-09-11: stack `credito-modern-current`; health + 401 + SPA `/app/`; `dev/token` ausente; MVC IIS Express :4779 (404/302 legado, no 502); API recreada con `MigracionClavePerezosa=true`. JWT real: abrir `/app/login`. |
| Proxy nginx/IIS preprod | **No ejecutado** |
| Login SPA real en preprod | **No ejecutado** |

CORS y `Jwt__SigningKey` no van en Git. Se definen en `deploy/.env.production` (partir de `.env.production.example`).

## Corte A — API y proxy

1. SQL accesible desde el host de la API. Aplicar `deploy/sql/` pendientes (`ClaveUsuario` nvarchar(256), condonación, TRF bancos) **antes** de la migración perezosa de claves.
2. Copiar `deploy/.env.production.example` → `.env` del servidor (fuera de Git).
3. `Jwt__SigningKey` ≥ 32 caracteres. `Hosting__AllowDevToken=false`.
4. Publicar API. Fachada: `deploy/nginx/production.conf.example` o `deploy/iis-arr-web.config.example`.
5. Tras el proxy: `Hosting__ForwardedHeaders__Enabled=true` y `KnownProxies` = IP del proxy. Entonces:  
   `.\deploy\scripts\verify-production-config.ps1 -RequireForwardedHeaders`
6. `.\deploy\scripts\smoke-strangler-proxy.ps1 -BaseUrl <URL preprod>`  
   Confirmar: `/api/v1/*` no cae al MVC; una URL MVC no migrada sigue en legado.
7. `.\deploy\scripts\test-auth-login.ps1` con usuario real (no `dev/token`).

## Corte B — SPA (`/app/`)

1. `cd Credito.Modern.Web`; `VITE_BASE_URL=/app/`; `VITE_API_BASE_URL` al origen del proxy; `npm run build`.
2. Copiar `dist/` a la `location /app/` del proxy (`production.conf.example`).
3. `BrowserCors:AllowedOrigins` = origen real de la SPA.
4. Login SPA + menú `usp_MenuLst` (SSD-01).
5. Piloto de una oficina: caja, consulta, aprobar (SSD-02 / SSD-03). Ventas y almacén (SSD-10 / SSD-11) solo si esa oficina las tiene en menú.

## Corte C — retiro gradual del MVC

No es el primer día.

- Observación ≥ 14 días (404, tickets).
- Incluir `spa-legacy-redirects.conf` cuando las rebanadas del piloto estén firmadas.
- Conservar `ReporteController` mientras negocio exija RDLC idéntico.
- `test-local-cutover.ps1` en Docker local antes de repetir en prod.
- Flags `Ui:UseSpaForModule` / `DefaultLoginToSpa`: Production ya los tiene en true (plantilla); PreProduction los deja en false hasta este corte.

## Rollback

Reenrutar la rebanada afectada al MVC. No borrar API ni SPA. Conservar `X-Correlation-ID` en logs.

## Scripts

| Script | Uso |
|--------|-----|
| `verify-production-config.ps1` | Contrato de plantilla (sin secretos) |
| `smoke-local-api.ps1` | API Development, sin segundo `dotnet run` |
| `smoke-strangler-proxy.ps1` | Fachada `/api/v1` |
| `verify-preprod-cutover.ps1 -BaseUrl <URL>` | Paquete preprod (config + ui-config + proxy) |
| `preprod-cutover-checklist.ps1 -SkipInteractive` | Lista A/B/C |

## Qué no hacer

- No usar el token WhatsApp de prueba Meta en producción.
- No activar `-RequireForwardedHeaders` contra la plantilla del repo (KnownProxies vacío a propósito).
- No citar cifras viejas de tests (714 / 690). Ver [MIGRATION-CLOSURE.md](MIGRATION-CLOSURE.md).
