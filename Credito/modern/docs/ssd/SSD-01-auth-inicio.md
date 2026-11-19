# SSD-01 — Autenticación, menú e inicio

**Estado:** as-built · 2026-11-18 · ver validación transversal [SSD-12](SSD-12-validacion.md)  
**Código:** `/login`, `/inicio` · `/api/v1/auth/*`, `/api/v1/menu`, `/api/v1/dashboard/*`  
**Fuente legacy:** `HomeController.Autenticar`, `_Layout.cshtml` (menú + Dashboard), `Dashboard/Admin`, `Dashboard/Gestor`  
**Doc de ingeniería:** [dashboard_migracion.md](../dashboard_migracion.md), [PASSWORD-STORAGE-ROADMAP.md](../migration/PASSWORD-STORAGE-ROADMAP.md), [MODERN-E2E-LOGIN-MENU.md](../migration/MODERN-E2E-LOGIN-MENU.md)  
**Cómo se conecta:** [COMO-LEER-EL-CODIGO.md](../COMO-LEER-EL-CODIGO.md)

## 1. Propósito y actores

Entrar al sistema, obtener menú por oficina/usuario y aterrizar en el tablero o mapa de módulos.

| Rol | Inicio |
|-----|--------|
| Analista (sin admin) | Tablero personal `/inicio` (KPIs, mora, ranking, podio, meta mes si hay cierre gerencial) |
| Administrador Central (`IndPrincipal`) | **Tablero Red** por defecto (consolidado + liga de sedes); selector sede → tablero operativo; `?vista=modulos` mapa; `?vista=analista` si también es ANALISTA |
| Administrador sucursal | Tablero gerencial de **su oficina JWT** (sin Red) |
| Gestor / cajero / encargado / aprobador | Mapa de módulos (hub), filtrado en enlaces de crédito |
| Todos | Menú lateral desde `usp_MenuLst` traducido a SPA |

## 2. Alcance

**Entra**

- Login SPA, refresh JWT, `/auth/me`, hora SQL
- Esquema de color (toggle en login y cabecera, SSD-19)
- Resolución de menú MVC → SPA (`legacyRoutes`, `resolveSpaPathFromMenuItem`)
- Tableros analista y admin (oficina JWT; Central: Tablero Red + `?sedeId=` en `/dashboard/admin/*`)
- Ranking/podio analista (`usp_DashboardRanking` / `usp_DashboardTopAnterior`) y meta mes (cierre gerencial)
- Almacenamiento de clave (claro legado o `$pbk2$`) y migración perezosa

**No entra**

- Usuarios, roles, oficinas (CRUD) → SSD-08
- Operaciones de cada módulo (crédito, caja, …)
- Alertas persistentes / umbrales configurables (Fase C pendiente)
- `POST /api/v1/dev/token` (solo Development; no es login de negocio)

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Home/Login` | `/login` | Oficinas activas + usuario/clave/oficina |
| `/Home/Index` | `/inicio` | Dashboard o hub según rol |
| Dashboard en `_Layout` | Ítem fijo «Dashboard» → `/inicio` | |
| `Dashboard/Admin` | `/inicio` | Ítem REPORTES → DASHBOARD |
| `Dashboard/Gestor` | `/inicio` | Tablero analista si el rol es ANALISTA |
| Menú `usp_MenuLst` | `AppShell` | Padres no navegan; hijos sí |

Padres (CREDITO, REPORTES, SEGURIDAD, MANTENIMIENTO) no se mapean a hubs: el SP los une y ampliaría permisos.

## 4. Contrato de datos

| Procedimiento / tabla | Uso |
|-----------------------|-----|
| `MAESTRO.Usuario`, `UsuarioOficina`, opcional `MAESTRO.Acceso` | Login (paridad MVC) |
| `ClaveUsuario` nvarchar(256) | Claro o hash `$pbk2$` (PBKDF2) |
| `MAESTRO.usp_MenuLst` | Menú por oficina y usuario |
| `CREDITO.usp_FechaBD` | Reloj de negocio |
| Tableros KPIs | Batch Dapper en C# (paridad; `usp_DashboardAdmin*` / `usp_DashboardGestor*` no usados en runtime por timeout / no versionados) |
| Ranking / podio | `CREDITO.usp_DashboardRanking`, `CREDITO.usp_DashboardTopAnterior` |
| Mora drill | `CREDITO.usp_DashboardGestorClientesMora` |
| Meta mes analista | Avance cierre gerencial (`ICierreGerencialReadService`) |

La verificación de clave es en C# (`UsuarioPasswordHasher` / `UsuarioPasswordHasherCompat` en MVC). No es `ClaveUsuario == clave` en SQL.

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `POST /api/v1/auth/login` | Anónimo + rate limit `auth-login` | `/login` |
| `POST /api/v1/auth/refresh` | Anónimo + rate limit `auth-refresh` (cuerpo = refresh JWT) | Sesión |
| `GET /api/v1/auth/me` | `CreditoUser` | Shell (roles + `nombreUsuario` vivos desde MAESTRO; ids del JWT) |
| `GET /api/v1/menu` | Sin política JWT; Bearer o query si `Menu:PermiteParametrosQuery` | Shell |
| `GET /api/v1/oficinas` | Anónimo (combo del login) | `/login` |
| `GET /api/v1/database-time` | Anónimo (`usp_FechaBD`) | Reloj |
| `GET /api/v1/dashboard/analista` | `CreditoRolPrendario` (ANALISTA o admin) | `/inicio` |
| `GET /api/v1/dashboard/analista/clientes-mora` | idem | Modal mora |
| `GET /api/v1/dashboard/admin` | `CreditoRolAdministrador` | `/inicio` sede |
| `GET /api/v1/dashboard/admin/shell` | `CreditoRolAdministrador` | `/inicio` sede (rápido) |
| `GET /api/v1/dashboard/admin/detalle` | `CreditoRolAdministrador` | `/inicio` sede (diferido) |
| `GET /api/v1/dashboard/admin/red` | `CreditoRolAdministrador` + Central | `/inicio` Red |

`Auth:MigracionClavePerezosa`: true en Development, Staging, PreProduction, Production; false en tests. `dev/token` no reescribe claves.

## 6. Seguridad

- Claims `vendix:usuario_id`, `vendix:oficina_id`, `vendix:usuario_oficina_id`, roles `ClaimTypes.Role`.
- `/auth/me` y el refresh releen `MAESTRO.UsuarioRol` / `NombreUsuario` para no mostrar roles o login tipado obsoletos.
- Tableros de sede acotados a JWT o `sedeId` permitido (Central).
- Tablero Red solo Central (`IndPrincipal`); sucursal → 403.
- Token WhatsApp / ApiPerú / JWT signing key fuera de Git.
- `Hosting:AllowDevToken` solo Development.

## 7. Criterios de aceptación

> **Documentación:** criterios redactados al 100%. Las casillas registran **evidencia de ejecución** (Development / preprod), no huecos de spec.

- [ ] Login SPA con usuario real (no `dev/token`) entra y carga menú.
- [ ] Misma clave vale en MVC tras hash `$pbk2$` (hasher compat).
- [x] Analista ve tablero personal (KPIs; ranking/podio/meta si hay datos). — API Docker `:5080` 2026-11-18
- [x] Admin Central ve Tablero Red (`GET …/admin/red` 200). — API Docker 2026-11-18
- [x] Admin sucursal no ve Red (`GET …/admin/red` 403). — API Docker 2026-11-18
- [ ] Reportes → Dashboard abre `/inicio`, no Informes. (SPA / menú)
- [ ] Padre CREDITO/SEGURIDAD no habilita consulta ni usuarios.
- [ ] Refresh rota access token; Bearer vencido → 401 y re-login.
- [ ] `dev/token` no existe fuera de Development (Production).

## 8. Desviaciones

- **2026-11-18 — Tablero Red + ranking/meta:** consolidado Central, `usp_DashboardRanking` / TopAnterior y meta mes ([BITACORA](../migration/BITACORA-DESVIACIONES.md), [dashboard_migracion.md](../dashboard_migracion.md)).
- 2026-10-17 — `/auth/me` roles JWT obsoletos + nombre tipado vs `NombreUsuario` ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-09-10 — tableros Inicio acotados a oficina JWT ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- **2026-10-24 — Central `sedeId`:** el tablero gerencial puede mostrar otra sede sin re-login ([004-alcance-red](../speckit/004-alcance-red/spec.md)).
- 2026-09-09 — `menutree` duplicado / iconos

Password: roadmap (columna 256, flag perezoso). No es desviación financiera.

## 9. Pruebas y evidencia

- API: `loginendpointtests`, `authmeendpointtests`, `usuariopasswordhashertests`, `dashboardadminendpointtests`, `dashboardanalistaendpointtests`
- SPA: `creditoOperacionPermisos.test.ts` (quién ve cada tablero), `resolvespapathfrommenuitem.test.ts` (Dashboard/Admin, padres nulos), `menuRouteAccess.test.ts`
- Smoke: `deploy/scripts/smoke-local-api.ps1` (health, 401, token, me, menu, database-time) — OK Docker `:5080` 2026-11-18
- Smoke: `deploy/scripts/smoke-strangler-proxy.ps1` (health, JWT, menú, reportes, ventas lectura, MVC upstream) — OK Docker `:9080` 2026-11-18

## 10. Go-live

Listo en Development. Preprod: login real + menú por rol; confirmar `ALTER ClaveUsuario` en esa base antes de confiar en la migración perezosa.
