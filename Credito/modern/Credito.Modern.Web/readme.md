# Credito.Modern.Web (Fase 5.0)

SPA **React + TypeScript + Vite** que consume `Credito.Modern.Api` (`/api/v1`).

## Requisitos

- Node.js 20+
- API accesible (recomendado: proxy strangler en **9080**)
- SQL Server con usuarios de prueba

## Desarrollo

```powershell
# Terminal 1 — Docker proxy + API (desde modern/)
cd D:\GitHub\Credito\modern
.\deploy\scripts\start-strangler.ps1

# Terminal 2 — SPA
cd D:\GitHub\Credito\modern\Credito.Modern.Web
npm install
npm run dev
```

Abrir **http://localhost:5173/login**

Marca: logo y textos de **Inversiones CrediConfiable** (`public/brand/credix.png`, copiado del MVC).

En desarrollo las llamadas van a `/api/v1` y Vite las reenvía a **9080** (sin CORS). Si el combo Oficina sale vacío, compruebe que el proxy strangler esté arriba y reinicie `npm run dev`.

Marca: logo y textos de **Inversiones CrediConfiable** (`public/brand/credix.png`, copiado del MVC).

En desarrollo las llamadas van a `/api/v1` y Vite las reenvía a **9080** (sin CORS). Si el combo Oficina sale vacío, compruebe que el proxy strangler esté arriba y reinicie `npm run dev`.

Variables (`.env.development` y, para la clave de mapas, `.env.development.local` — no commitear la API key):

```env
VITE_API_BASE_URL=http://localhost:9080/api/v1
# Opcional: origen del MVC para enlaces de menú legacy
VITE_LEGACY_ORIGIN=http://localhost:9080
# Google Maps (Mantenimiento → Oficinas). Restrinja la clave por dominio en Google Cloud.
VITE_GOOGLE_MAPS_API_KEY=su_clave_aqui
```

Copie `.env.example` a `.env.development.local` y asigne su clave de **Maps JavaScript API** + **Places API** habilitadas.

Si el login devuelve *No se pudo validar credenciales contra SQL Server* tras clave correcta, reconstruya la API (contenedor con `MigracionClavePerezosa=true` antiguo):

```powershell
cd D:\GitHub\Credito\modern
.\deploy\scripts\restart-strangler-fresh.ps1
```

Si el login devuelve *No se pudo validar credenciales contra SQL Server* tras clave correcta, reconstruya la API (contenedor con `MigracionClavePerezosa=true` antiguo):

```powershell
cd D:\GitHub\Credito\modern
.\deploy\scripts\restart-strangler-fresh.ps1
```

Tras cambiar CORS en `appsettings.Staging.json`, recrear contenedor API:

```powershell
docker compose -f deploy/docker-compose.strangler.yml up -d --build credito-modern-api
```

## Build producción

```powershell
npm run build
```

Salida en `dist/`. Servir bajo `/app/` en nginx (ver [PHASE-5-UI-ARCHITECTURE.md](../../docs/migration/PHASE-5-UI-ARCHITECTURE.md)).

## Incluido

**5.0**

- Login (`POST /auth/login`) + refresh automático
- `GET /auth/me`, `GET /menu`, `GET /oficinas`
- Shell con menú lateral (árbol o agrupado por módulo)
- Enlaces legacy con mapeo SPA cuando aplica; resto → MVC en nueva pestaña
- Placeholder para ítems sin URL

**5.2 (piloto)**

- **Marcas** — `/maestros/marcas` (`GET /marcas`)
- **Modelos** — `/maestros/modelos` (`GET /modelos?marcaId=`)
- **Saldo cartera** — `/informes/saldo-cartera` (JSON + CSV/PDF API)

## Documentación

- [PHASE-5-UI-START.md](../../docs/migration/PHASE-5-UI-START.md)
- [PHASE-5-UI-ROADMAP.md](../../docs/migration/PHASE-5-UI-ROADMAP.md)
