# Cómo leer el código — Credito.Modern

**Audiencia:** ingeniero nuevo o AI agent que debe ubicar un cambio en <5 minutos.  
**Fecha:** 2026-11-18 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md) · **Mapa de carpetas:** [ESTRUCTURA.md](ESTRUCTURA.md) · **Principios:** [ARCHITECTURE.md](ARCHITECTURE.md)

Este documento explica **cómo se conectan las piezas**. No sustituye los SSD ni el inventario largo de endpoints.

---

## 1. Una frase

El navegador habla con **Minimal APIs** (`/api/v1`). Cada endpoint orquesta un **contrato** en Application (`I*Service` + DTO). Infrastructure **ejecuta SQL** (Dapper + `usp_*` o batch documentado). El dinero y la mora viven en SQL; C# valida, acota por oficina JWT y mapea errores.

---

## 2. Capas y dependencias (obligatorio)

```
Credito.Modern.Web          React (Vite) → fetch /api/v1
        │
Credito.Modern.Api          endpoints + JWT + ProblemDetails
        │
        ├── Application     I*Service, DTOs, Validation/
        │         └── Domain
        └── Infrastructure  implementa I*Service (Dapper / usp_*)
                  └── Application
```

| Se permite | No se permite |
|------------|---------------|
| Api → Application | Application → Infrastructure |
| Infrastructure → Application (contratos) | Web → Infrastructure |
| Tests → Api (factory) | Domain → Api / Web |
| Endpoint llama `I*Service` | Endpoint con SQL embebido “de negocio” no documentado |

Registro DI: `Credito.Modern.Infrastructure/dependencyinjection.cs`.

---

## 3. Camino de un request (ejemplo: tablero)

```
SPA  pages/dashboard/AdminDashboardPage.tsx
  →  api/dashboard.ts  fetchDashboardAdminRed()
  →  GET /api/v1/dashboard/admin/red
  →  Api/dashboard/dashboardadminendpoints.cs
       · SesionOficina.ResolverAsync (JWT + Destinos)
       · IDashboardAdminReadService.ObtenerRedAsync
  →  Infrastructure/dashboard/dashboardadminreadservice.cs
       · fan-out ObtenerAsync(oficina) por sede
       · cache IMemoryCache 90s
  →  JSON DashboardAdminRedDto (Application)
```

**Regla de búsqueda** (siempre igual):

1. Pantalla → `Web/src/pages/<modulo>/`
2. Cliente HTTP → `Web/src/api/<dominio>.ts`
3. Ruta → `Api/<carpeta>/*endpoints.cs`
4. Contrato → `Application/<dominio>/I*.cs` + DTOs
5. SQL → `Infrastructure/<dominio>/*service.cs`
6. Spec → `docs/ssd/SSD-XX-*.md`
7. Desvío vs MVC → `docs/migration/BITACORA-DESVIACIONES.md`

---

## 4. Multisucursal (cómo se conecta la oficina)

| Concepto | Dónde |
|----------|--------|
| Oficina de sesión | Claim JWT `vendix:oficina_id` |
| ¿Es Central? | `MAESTRO.Oficina.IndPrincipal` vía `SesionOficina.ResolverAsync` |
| Destinos | Central = oficinas activas; sucursal = solo la suya |
| Lectura otra sede | Query `?sedeId=` + `OficinaLectura(..., allowRed: false\|true)` |
| Escritura | Cuerpo `oficinaId` = JWT (`CajaCreditoWriteGuards`) |

Archivo clave: `Api/auth/sesionoficina.cs`.  
Spec de alcance: [speckit/004-alcance-red](speckit/004-alcance-red/spec.md) · flujos: [SSD-18](ssd/SSD-18-flujos-modulos.md).

---

## 5. Organización por dominio (mismo nombre a ambos lados)

| Application | Infrastructure | Api | Web |
|-------------|----------------|-----|-----|
| `dashboard/` | `dashboard/` | `dashboard/` | `pages/dashboard/` + `api/dashboard.ts` |
| `creditoplanes/` | `creditoplanes/` | `credito/` | `pages/caja`, `tesoreria`, `informes`… |
| `auth/` | `auth/` | `auth/` | `auth/` |
| `clientes/` | `clientes/` | `clientes/` | `pages/clientes/` |

Un archivo de endpoints ≈ un grupo de rutas. Si el archivo creció de más, se parte en **`partial class`** (`*.consultas.cs`, `*.cierres.cs`, …) con un facade que solo llama a los `Map*`. Un `I*ReadService` / `I*WriteService` ≈ un caso de uso.

---

## 6. Frontend — cómo está cableado

| Pieza | Rol |
|-------|-----|
| `api/client.ts` | `apiFetch` + Bearer + ProblemDetails |
| `api/<dominio>.ts` | Tipos + funciones por endpoint |
| `pages/<modulo>/` | Pantallas; TanStack Query |
| `components/credix/` | Page, tabla, layout de marca |
| `routes/lazyPages.ts` | Code-split por ruta |
| `styles/` + `theme/` | Tokens `--credix-*`, claro/oscuro |

Auth de sesión: `auth/useAuth` + `hooks/useEsCentral` (lista oficinas + `IndPrincipal`).

---

## 7. SQL y reloj

- Reloj de negocio: `CREDITO.usp_FechaBD` (no `DateTime.UtcNow` para cortes de caja).
- Snapshot de procs: `db/schema/`.
- Scripts a aplicar: `deploy/sql/` (orden por fecha).
- Excepción declarada: algunos tableros usan **batch Dapper** en C# (paridad semántica de USP retirados / timeout) — ver [SSD-01](ssd/SSD-01-auth-inicio.md) y [dashboard_migracion.md](dashboard_migracion.md).

---

## 8. Documentación: qué leer según la tarea

| Tarea | Documento |
|-------|-----------|
| Orientarse en carpetas | [ESTRUCTURA.md](ESTRUCTURA.md) |
| Principios / una BD | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Este mapa de conexiones | **Este archivo** |
| Contrato de un módulo | `ssd/SSD-XX-*.md` |
| Notas de ingeniería largas | `*_migracion.md` (canónico sigue siendo el SSD) |
| Prefijos API | [API-OVERVIEW.md](API-OVERVIEW.md) |
| Inventario endpoint a endpoint | [../readme.md](../readme.md) |
| Deploy / strangler | [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md) |

Índice único: [DOCUMENTACION.md](DOCUMENTACION.md).

---

## 9. Checklist al tocar un módulo

1. ¿El endpoint usa `SesionOficina` / guards de oficina?
2. ¿La política JWT es la del SSD?
3. ¿El SQL está en Infrastructure (no en el endpoint)?
4. ¿Hay DTO en Application y tipo en `api/*.ts`?
5. ¿Actualizaste SSD + BITACORA si cambió paridad?
6. ¿Tests de 401/403 en `Credito.Modern.Tests`?
