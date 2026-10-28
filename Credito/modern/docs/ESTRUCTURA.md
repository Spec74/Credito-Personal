# Estructura del código — Credito.Modern

**Para:** informática / ingeniero de despliegue  
**Fecha:** 2026-10-27 · rama `Eber-migracion`  
**Índice general:** [DOCUMENTACION.md](DOCUMENTACION.md) · **Arquitectura (principios):** [ARCHITECTURE.md](ARCHITECTURE.md)

Este archivo describe **dónde está cada cosa en el árbol**, no la historia de la migración.

---

## 1. Qué se publica y qué no

```
D:\Ebers\GitHub\
├── Credito\
│   ├── modern\                 ← ÚNICO runtime que se despliega
│   ├── ITB.VENDIX.BE / BL / DA ← MVC legado. No se compila en CI. No se publica.
│   ├── Web\                    ← Sitio MVC. Rollback / RDLC si negocio lo pide.
│   ├── mango\ / packages\      ← Soporte NuGet del MVC.
│   └── Creditos.sln            ← Solution del legado. No abrir para el piloto.
└── _wt-PrendarioLegacy\        ← Worktree local. No está en Git. No desplegar.
```

CI (`Eber-migracion` → Azure): `dotnet publish Credito.Modern.Api` desde `Credito/modern`.  
SPA (Vercel): root `Credito/modern/Credito.Modern.Web`.  
Base: una sola `CREDITO` (Azure SQL o SQL Server). Sucursal = `OficinaId` en el JWT, no otro servidor.

---

## 2. Solution moderna (`Credito.Modern.sln`)

Dependencias en una sola dirección:

```
Credito.Modern.Web          SPA React (Vite + Ant Design)
        │  HTTPS /api/v1
        ▼
Credito.Modern.Api          Minimal APIs, JWT, ProblemDetails
        │
        ├── Credito.Modern.Application   casos de uso, DTOs, Validation/
        │         └── Credito.Modern.Domain
        └── Credito.Modern.Infrastructure  Dapper + SqlClient + usp_*
                  └── Application

Credito.Modern.Tests        xUnit contra la API
```

**Regla de negocio:** el dinero y la mora viven en SQL (`usp_*`). C# orquesta, valida entrada y mapea errores. Reloj: `CREDITO.usp_FechaBD`.

No hay referencia de proyecto a `ITB.VENDIX.*`.

---

## 3. API — carpetas

`Credito.Modern.Api/` = endpoints por dominio. Un archivo ≈ un grupo de rutas.

| Carpeta | Qué expone |
|---------|------------|
| `auth/` | Login, refresh, me, claims, policies, guards de oficina |
| `credito/` | Ciclo crédito, caja (diario/chica/saldos), cobros, informes, condonación, cierre gerencial |
| `prendario/` | Alta, ficha, renovar, liberar, remate |
| `caja/` | Maestro de cajas |
| `clientes/` | Padrón, búsqueda, alta rápida, GPS |
| `ventas/` | Orden, venta rápida, listados |
| `almacen/` | Entrada/salida, kardex, constancias |
| `articulos/` | Artículos (inventario) |
| `maestros/` | Marcas, modelos, tipos, ubigeo, valores-tabla |
| `admin/` | Usuarios, roles |
| `dashboard/` | Analista / admin |
| `intranet/` | Comunicados, agenda |
| `pla/` | PLA / UIF |
| `reportes/` | Catálogo de export + PDF tabular (QuestPDF) |
| `hosting/` | CORS, rate limit, headers, health, OTel |
| `Integraciones/` | Webhook WhatsApp |
| `Validation/` | ProblemResults RFC 7807 |

Arranque: `program.cs` (pipeline, DI, JWT).  
Config: `appsettings.json` + `appsettings.{development,staging,preproduction,production}.json`.

---

## 4. Application e Infrastructure

Mismo nombre de carpeta a ambos lados:

| Application (contrato) | Infrastructure (SQL) |
|------------------------|----------------------|
| `I*Service` + DTO + `Validation/` | Clase que ejecuta `usp_*` / SELECT con Dapper |
| `auth/` login | `auth/legacyloginservice.cs` (Usuario + UsuarioOficina + Acceso) |
| `creditoplanes/` caja, bóveda, cobro, reportes | `creditoplanes/*readservice.cs` / `*writeservice.cs` |
| `prendario/` | `prendario/` |
| `clientes/`, `ventas/`, `almacenes/` | igual |
| `dashboard/`, `pla/`, `intranet/` | igual |

Registro DI: `Infrastructure/dependencyinjection.cs`.

---

## 5. SPA — `Credito.Modern.Web/src`

| Carpeta | Rol |
|---------|-----|
| `api/` | Cliente HTTP (`client.ts`) por dominio |
| `auth/` | Sesión JWT, oficina |
| `pages/` | Una carpeta por módulo: `caja`, `credito`, `tesoreria`, `clientes`, `informes`, `admin`, `masters`, `ventas`, `almacen`, `dashboard` |
| `components/` | `layout/` (AppShell), `credix/` (Page, tabla, select), `brand/`, `caja/`, `credito/` |
| `routes/` | `index.tsx` + `lazyPages.ts` |
| `config/` | `branding.ts`, hubs de menú |
| `theme/` | Claro / oscuro (WCAG 2.2 AA) |
| `styles/` | Tokens `--credix-*` + CSS por módulo |
| `utils/` | Menú, fechas, validaciones de flujo |
| `public/brand/` | `credix.png` (250×91) + `credix-3x.png` (748×275) + SVG de archivo |

Login: `pages/LoginPage.tsx`. Shell: `components/layout/AppShell.tsx`.  
Rutas sensibles: `utils/menuRouteAccess.ts` (el menú lo manda `usp_MenuLst`).

---

## 6. SQL y scripts de deploy

| Ruta | Uso |
|------|-----|
| `db/schema/` | Snapshot de tablas / `usp_*` (referencia, no se “corre” entero) |
| `deploy/sql/` | Scripts **ordenados por fecha** a aplicar sobre CREDITO si el bak no los trae |
| `deploy/sql/local/` | Solo máquina de desarrollo (sedes de prueba) |
| `deploy/scripts/` | Smoke, quality, strangler, cutover (PowerShell) |
| `deploy/nginx/` + `docker-compose.strangler.yml` | Proxy local `/api/v1` → API, `/app/` → SPA, resto → MVC |

**Scripts que el ingeniero sí usa**

| Script | Cuándo |
|--------|--------|
| `build-spa.ps1` | Empaquetar SPA |
| `smoke-local-api.ps1` | API en 5288 |
| `smoke-db-gate.ps1` / `smoke-ops-checklist.ps1` | Puerta BD / ops |
| `quality-gate.ps1` | Lint + tests |
| `verify-production-config.ps1` | Plantillas prod |
| `preprod-cutover-checklist.ps1` | Corte A/B |
| `piloto-un-dia.ps1` | Guion de oficina |
| `start-strangler.ps1` + `smoke-strangler-proxy.ps1` | Docker 9080 (convivencia MVC) |
| `apply-migrations.ps1` | Aplicar `deploy/sql` |
| `restore-db-backup.ps1` | Restaurar bak |

Los `test-mvc-*.ps1` y `audit-edmx-msl.ps1` sirven **solo** si se levanta el MVC de rollback. No entran al publish de Azure.

---

## 7. Cómo encontrar un cambio

1. Pantalla → `Web/src/pages/<modulo>/`.
2. Llamada HTTP → `Web/src/api/<dominio>.ts`.
3. Ruta → `Api/<dominio>*endpoints.cs`.
4. SQL → `Infrastructure/<dominio>*service.cs` (nombre del `usp_*`).
5. Spec → `docs/ssd/SSD-XX-*.md`.
6. Si el número no cuadra con el MVC → `docs/migration/BITACORA-DESVIACIONES.md`.

---

## 8. Auth (imprescindible para desplegar)

- `POST /api/v1/auth/login` → `{ nombreUsuario, clave, oficinaId }`.
- JWT claims: `vendix:usuario_id`, `vendix:oficina_id`, `vendix:usuario_oficina_id`, roles MAESTRO.
- Escrituras: oficina del body = oficina del token (`CajaCreditoWriteGuards`).
- Production: `Hosting:AllowDevToken=false`, `Menu:PermiteParametrosQuery=false`, `Auth:RequerirClienteAcceso` según piloto (hoy `false` en Vercel público).
- JWT signing key ≥ 32 caracteres en App Service.

Detalle: [SSD-01](ssd/SSD-01-auth-inicio.md) · [DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md).

---

## 9. Documentación viva (el resto es índice)

| Documento | Para qué |
|-----------|----------|
| Este archivo | Mapa de carpetas |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Principios (una BD, no segundo motor) |
| [ssd/README.md](ssd/README.md) | Spec por módulo 00–19 |
| [API-OVERVIEW.md](API-OVERVIEW.md) | Prefijos y policies |
| [../readme.md](../readme.md) | Inventario largo de endpoints |
| [OPS.md](OPS.md) / [AMBIENTES.md](AMBIENTES.md) | Runbooks |
| [BITACORA-DESVIACIONES.md](migration/BITACORA-DESVIACIONES.md) | Diferencias vs MVC |

`docs/speckit/` = bitácora Spec Kit de features UX (no es el runtime).  
`docs/*_migracion.md` = notas de ingeniería por dominio; el canónico es el SSD.
