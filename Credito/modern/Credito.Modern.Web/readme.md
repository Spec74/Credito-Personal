# Credito.Modern.Web

SPA **React + TypeScript + Vite + Ant Design** que consume `Credito.Modern.Api` (`/api/v1`).

**Documentación del sistema:** [DOCUMENTACION.md](../docs/DOCUMENTACION.md) · [SSD](../docs/ssd/README.md) · [Arquitectura](../docs/ARCHITECTURE.md)

## Requisitos

- Node.js 20+
- API accesible (recomendado: proxy strangler en **9080**)
- SQL Server con usuarios de prueba

## Desarrollo

```powershell
# Terminal 1 — Docker proxy + API (desde modern/)
cd Credito\modern
.\deploy\scripts\start-strangler.ps1

# Terminal 2 — SPA
cd Credito\modern\Credito.Modern.Web
npm install
npm run dev
```

Abrir **http://localhost:5173/login**

Marca: **Inversiones CrediConfiable** (`public/brand/credix.png`).

En desarrollo las llamadas van a `/api/v1` y Vite las reenvía a **9080** (sin CORS). Si el combo Oficina sale vacío, compruebe que el proxy esté arriba y reinicie `npm run dev`.

Variables (`.env.development` / `.env.development.local` — no commitear claves):

```env
VITE_API_BASE_URL=http://localhost:9080/api/v1
VITE_LEGACY_ORIGIN=http://localhost:9080
VITE_GOOGLE_MAPS_API_KEY=su_clave_aqui
```

Si el login falla contra SQL tras clave correcta, reconstruya API:

```powershell
cd Credito\modern
.\deploy\scripts\restart-strangler-fresh.ps1
```

## Build producción

```powershell
npm run build
```

Salida en `dist/`. Servir bajo `/app/` (ver [PHASE-5-UI-ARCHITECTURE.md](../docs/migration/PHASE-5-UI-ARCHITECTURE.md), [SSD-00](../docs/ssd/SSD-00-cutover.md)).

## Cobertura SPA

Módulos operativos: autenticación, inicio, crédito, clientes, caja, tesorería/bóveda, ventas, almacén, maestros, admin, informes/reportes.  
Catálogo SSD: [docs/ssd/README.md](../docs/ssd/README.md). UI Credix: [ui_modernizacion_modulos.md](../docs/ui_modernizacion_modulos.md).

## Tests

```powershell
npm test -- --run
```

Estrategia: [TEST-STRATEGY.md](../docs/TEST-STRATEGY.md).
