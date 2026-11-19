# Dashboard / Inicio — notas de ingeniería

**Canónico de producto:** [ssd/SSD-01-auth-inicio.md](ssd/SSD-01-auth-inicio.md)  
**Cómo se conecta el código:** [COMO-LEER-EL-CODIGO.md](COMO-LEER-EL-CODIGO.md)  
**Fecha:** 2026-11-18 · commit de referencia `9a946e9c` (+ ajustes de docs)

---

## 1. Piezas

| Capa | Archivos |
|------|----------|
| SPA admin | `Web/src/pages/dashboard/AdminDashboardPage.tsx` (`AdminRedDashboard` + `AdminSedeDashboard`) |
| SPA analista | `Web/src/pages/dashboard/AnalystDashboardPage.tsx` |
| Cliente HTTP | `Web/src/api/dashboard.ts` |
| API | `Api/dashboard/dashboardadminendpoints.cs`, `dashboardanalistaendpoints.cs` |
| Contratos | `Application/dashboard/*` (`IDashboardAdminReadService`, `IDashboardAnalistaReadService`, DTOs) |
| SQL / orquestación | `Infrastructure/dashboard/dashboardadminreadservice.cs`, `dashboardanalistareadservice.cs` |
| Tests | `Credito.Modern.Tests/dashboard*tests.cs` |

DI: `AddScoped<IDashboardAdminReadService, DashboardAdminReadService>()` (y analista) en `dependencyinjection.cs`.

---

## 2. Flujos

### Admin Central — Tablero Red (default `/inicio` sin `sedeId`)

1. SPA detecta `esCentral && !sedeId` → `fetchDashboardAdminRed()`.
2. `GET /api/v1/dashboard/admin/red` exige `CreditoRolAdministrador` + `IndPrincipal`.
3. `SesionOficina` arma `Destinos` = oficinas activas.
4. `ObtenerRedAsync` hace fan-out (semáforo 3) de `ObtenerAsync` por sede, agrega KPIs y semáforo (`verde`/`ambar`/`rojo`).
5. Clic en sede → `?sedeId=` → tablero operativo (shell + detalle).

Sucursal que llama `/admin/red` → **403**.

### Admin — Tablero sede

- `GET …/admin/shell` (rápido) + `GET …/admin/detalle` (cartera, flujo, analistas).
- Central con `?sedeId=` lee esa oficina sin re-login.

### Analista — Mi jornada

- `GET /api/v1/dashboard/analista` → KPIs + productividad (batch Dapper).
- Ranking: `CREDITO.usp_DashboardRanking` (best-effort; vacío si falla).
- Podio: `CREDITO.usp_DashboardTopAnterior` (best-effort).
- Meta mes: `ICierreGerencialReadService.ObtenerAvanceAsync` filtrado por `usuarioId` (best-effort; `null` si no hay fila).

---

## 3. Contrato de datos

| Fuente | Uso |
|--------|-----|
| Batch Dapper shell/detalle | Colocación, cobranza CUO, flujo, cartera (paridad semántica; no `usp_DashboardAdmin*` en runtime) |
| `usp_DashboardRanking` | Liga de cobranza del mes (RolId = 6) |
| `usp_DashboardTopAnterior` | Top 3 mes anterior |
| Cierre gerencial (avance) | Meta capital/clientes del analista |
| `usp_DashboardGestorClientesMora` | Drill clientes mora |

Caché: shell/red ~90s, detalle ~180s (`IMemoryCache`).

---

## 4. Semáforo de sede (Red)

| Condición | Semáforo |
|-----------|----------|
| Mora clientes ≥ 20% | rojo |
| Flujo neto &lt; 0 y cobrado &lt; 70% vs ayer | rojo |
| Flujo neto &lt; 0 | ámbar |
| Mora ≥ 10% | ámbar |
| Cobrado hoy &lt; ayer | ámbar |
| Resto | verde |

---

## 5. Cumplimiento de arquitectura

| Regla | Estado |
|-------|--------|
| Endpoint sin SQL de negocio | PASS |
| DTOs en Application | PASS |
| Infra implementa `I*Service` | PASS |
| Oficina vía `SesionOficina` / Destinos | PASS |
| Fan-out Red solo Central | PASS |
| Ranking/podio/meta best-effort (no tumban el tablero) | WARN aceptado (mismo patrón que shell cob/flujo) |
| KPIs principales = batch C# no USP | WARN documentado (histórico timeout USP admin/gestor) |

---

## 6. Smoke rápido (Docker `:5080`)

```text
POST /api/v1/dev/token  { usuarioId, oficinaId:1, roles:["ADMINISTRADOR"] }  → Bearer
GET  /api/v1/dashboard/admin/red     → 200 (Central)
GET  /api/v1/dashboard/admin/red     → 403 (oficina no IndPrincipal)
GET  /api/v1/dashboard/analista      → 200 (rol ANALISTA)
```

SPA: `/app/inicio` con admin Central muestra “Toda la red”; selector “Toda la red | sede”.
