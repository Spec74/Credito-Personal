# SSD-08 — Administración (seguridad y oficinas)

**Estado:** as-built · 2026-09-11  
**Código:** `/admin/*`, `/mantenimiento/oficinas` · `/api/v1/usuarios/*`, `/api/v1/roles/*`, `/api/v1/oficinas/*`  
**Fuente legacy:** `UsuarioController`, `RolController`, `OficinaController`, `ComisionController`  
**Doc de ingeniería:** [ui_modernizacion_modulos.md](../ui_modernizacion_modulos.md) (no hay `*_migracion.md` propio)

## 1. Propósito y actores

Dar de alta oficinas, usuarios, roles/menús y (vacío a propósito) comisiones.

| Rol | Uso |
|-----|-----|
| Administrador | Único que escribe usuarios, roles, activar, resetear clave, asignar oficinas/roles/menús |
| Cualquier autenticado | Combo de oficinas activas en login (SSD-01); gestores para filtros de informe |
| Resto | Sin ítem USUARIO/ROL/OFICINA no entra |

## 2. Alcance

**Entra**

- Hub `/admin` (mapa; no sustituye ítems exactos)
- Usuarios: listado, ficha, guardar, activar, resetear clave (`123456` hasheada), oficinas y roles
- Roles: listado, guardar, activar, menús del rol
- Oficinas: `/mantenimiento/oficinas` (`/admin/oficinas` redirige)
- Comisiones: pantalla reservada (el MVC no calcula)

**No entra**

- Login / hash perezoso / menú de sesión → SSD-01
- Maestro de cajas → SSD-03
- Marcas, artículos, almacenes → SSD-09
- Tablero gerencial → SSD-01

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Usuario` | `/admin/usuarios` | Menú USUARIO módulo SEGURIDAD |
| `/Rol` | `/admin/roles` | Exact menu |
| `/Oficina` | `/mantenimiento/oficinas` | Módulo MANTENIMIENTO |
| `/Comision` | `/admin/comisiones` | Solo título; hub admin sí abre esta reserva |
| Padre SEGURIDAD | — | No habilita usuarios ni roles |

## 4. Contrato de datos

| Tabla / procedimiento | Uso |
|-----------------------|-----|
| `MAESTRO.Usuario`, `UsuarioOficina`, roles | CRUD; `ClaveUsuario` nvarchar(256), hasher C# |
| `MAESTRO.Rol`, menú por rol (`usp_MenuLst` consume esto) | Asignar menús |
| `MAESTRO.Oficina` | Alta/edición/activar |
| Comisiones | **Ningún** `usp_*` ni tabla |

Resetear clave: paridad MVC (`123456`) con hash `$pbk2$` para el login moderno.

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET /api/v1/usuarios/gestion`, `GET .../detalle` | `CreditoRolAdministrador` | `/admin/usuarios` |
| `POST /usuarios/guardar`, `.../activar`, `.../resetear-clave` | Admin | Usuarios |
| `POST .../asignar-oficinas`, `.../asignar-roles` | Admin | Usuarios |
| `GET /usuarios/reporte-gestores` | `CreditoUser` | Combos de informes |
| `GET/POST /api/v1/roles/*` | Admin | `/admin/roles` |
| `GET /oficinas/gestion` | `CreditoUser` | Oficinas |
| `POST /oficinas/guardar`, `.../activar` | Admin | Oficinas |

## 6. Seguridad

- `EXACT_MENU_ROUTES`: `/admin/usuarios`, `/admin/roles`, `/mantenimiento/oficinas`.
- Hub `/admin` no abre usuarios/roles/oficinas; sí puede abrir `/admin/comisiones`.
- Padre SEGURIDAD no se mapea a `/admin`.
- Escrituras de usuario/rol: solo `CreditoRolAdministrador`.

## 7. Criterios de aceptación

- [ ] Ítem USUARIO abre listado; sin ítem, 403 de ruta aunque exista el padre SEGURIDAD.
- [ ] Guardar usuario y asignar oficina/rol permite login SPA en esa oficina.
- [ ] Resetear clave deja `123456` verificable por el hasher (claro legado o `$pbk2$`).
- [ ] Rol + menús cambia `usp_MenuLst` en el siguiente login.
- [ ] `/admin/comisiones` no inventa liquidación.
- [ ] Gestor no llama `POST /usuarios/guardar` (403).

## 8. Desviaciones

- 2026-09-10 — Comisiones: paridad del vacío del MVC ([BITACORA](../migration/BITACORA-DESVIACIONES.md))

Password: [PASSWORD-STORAGE-ROADMAP.md](../migration/PASSWORD-STORAGE-ROADMAP.md) (columna 256).

## 9. Pruebas y evidencia

- API: `usuariosadminendpointtests`, `roladminendpointtests`, `oficinaendpointtests`, `usuariopasswordhashertests`
- SPA: `menuRouteAccess.test.ts` (padre SEGURIDAD; hub admin vs usuarios; comisiones), `resolvespapathfrommenuitem.test.ts`
- Smoke: admin crea usuario de prueba, asigna GESTOR + oficina, login en SSD-01

## 10. Go-live

Listo en Development. Preprod: un alta real + reset de clave y comprobar MVC y SPA con la misma fila.
