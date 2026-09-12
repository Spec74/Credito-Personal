# SSD-01 — Autenticación, menú e inicio

**Estado:** as-built · 2026-09-11  
**Código:** `/login`, `/inicio` · `/api/v1/auth/*`, `/api/v1/menu`, `/api/v1/dashboard/*`  
**Fuente legacy:** `HomeController.Autenticar`, `_Layout.cshtml` (menú + Dashboard), `Dashboard/Admin`, `Dashboard/Gestor`  
**Doc de ingeniería:** [PASSWORD-STORAGE-ROADMAP.md](../migration/PASSWORD-STORAGE-ROADMAP.md), [MODERN-E2E-LOGIN-MENU.md](../migration/MODERN-E2E-LOGIN-MENU.md)

## 1. Propósito y actores

Entrar al sistema, obtener menú por oficina/usuario y aterrizar en el tablero o mapa de módulos.

| Rol | Inicio |
|-----|--------|
| Analista (sin admin) | Tablero personal `/inicio` |
| Administrador | Tablero gerencial de **su oficina**; `?vista=modulos` mapa; `?vista=analista` si también es ANALISTA |
| Gestor / cajero / encargado / aprobador | Mapa de módulos (hub), filtrado en enlaces de crédito |
| Todos | Menú lateral desde `usp_MenuLst` traducido a SPA |

## 2. Alcance

**Entra**

- Login SPA, refresh JWT, `/auth/me`, hora SQL
- Resolución de menú MVC → SPA (`legacyRoutes`, `resolveSpaPathFromMenuItem`)
- Tableros analista y admin (oficina JWT)
- Almacenamiento de clave (claro legado o `$pbk2$`) y migración perezosa

**No entra**

- Usuarios, roles, oficinas (CRUD) → SSD-08
- Operaciones de cada módulo (crédito, caja, …)
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
| Tableros | Batch Dapper en C# (no `usp_DashboardAdmin*` / `usp_DashboardGestor*` del bak: no versionados y timeout en prod) |

La verificación de clave es en C# (`UsuarioPasswordHasher` / `UsuarioPasswordHasherCompat` en MVC). No es `ClaveUsuario == clave` en SQL.

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `POST /api/v1/auth/login` | Anónimo + rate limit `auth-login` | `/login` |
| `POST /api/v1/auth/refresh` | Anónimo + rate limit `auth-refresh` (cuerpo = refresh JWT) | Sesión |
| `GET /api/v1/auth/me` | `CreditoUser` | Shell |
| `GET /api/v1/menu` | Sin política JWT; Bearer o query si `Menu:PermiteParametrosQuery` | Shell |
| `GET /api/v1/oficinas` | Anónimo (combo del login) | `/login` |
| `GET /api/v1/database-time` | Anónimo (`usp_FechaBD`) | Reloj |
| `GET /api/v1/dashboard/analista` | `CreditoRolPrendario` (ANALISTA o admin) | `/inicio` |
| `GET /api/v1/dashboard/admin` | `CreditoRolAdministrador` | `/inicio` |

`Auth:MigracionClavePerezosa`: true en Development, Staging, PreProduction, Production; false en tests. `dev/token` no reescribe claves.

## 6. Seguridad

- Claims `vendix:usuario_id`, `vendix:oficina_id`, `vendix:usuario_oficina_id`, roles `ClaimTypes.Role`.
- Tableros acotados a la oficina del JWT (no compañía).
- Token WhatsApp / ApiPerú / JWT signing key fuera de Git.
- `Hosting:AllowDevToken` solo Development.

## 7. Criterios de aceptación

- [ ] Login SPA con usuario real (no `dev/token`) entra y carga menú.
- [ ] Misma clave vale en MVC tras hash `$pbk2$` (hasher compat).
- [ ] Analista ve tablero personal; admin el gerencial de su oficina.
- [ ] Reportes → Dashboard abre `/inicio`, no Informes.
- [ ] Padre CREDITO/SEGURIDAD no habilita consulta ni usuarios.
- [ ] Refresh rota access token; Bearer vencido → 401 y re-login.
- [ ] `dev/token` no existe fuera de Development.

## 8. Desviaciones

- 2026-09-10 — tableros Inicio acotados a oficina JWT ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-09-09 — `menutree` duplicado / iconos

Password: roadmap (columna 256, flag perezoso). No es desviación financiera.

## 9. Pruebas y evidencia

- API: `loginendpointtests`, `authmeendpointtests`, `usuariopasswordhashertests`, `dashboardadminendpointtests`, `dashboardanalistaendpointtests`
- SPA: `creditoOperacionPermisos.test.ts` (quién ve cada tablero), `resolvespapathfrommenuitem.test.ts` (Dashboard/Admin, padres nulos), `menuRouteAccess.test.ts`
- Smoke: `deploy/scripts/smoke-local-api.ps1` (health, 401, token, me, menu, database-time)

## 10. Go-live

Listo en Development. Preprod: login real + menú por rol; confirmar `ALTER ClaveUsuario` en esa base antes de confiar en la migración perezosa.
