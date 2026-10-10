# E2E — Matriz rol × módulo (producción)

**Fecha:** 2026-10-10  
**SPA:** https://credito-personal.vercel.app  
**API:** Azure `crediconfiable-api-…`  
**Oficina:** 1  

> El smoke anterior (cajeros/ruta/PDF) **no** cubría toda la plataforma. Esta corrida valida **perfiles de rol representativos × módulos del menú ACL** + flujos de UI con datos.

## Cómo se prueba

| Capa | Qué valida | Cómo |
|------|------------|------|
| **0 — Inventario** | Login + `/auth/me` + menú por usuario activo | API |
| **1 — Matriz ACL** | Cada ruta del menú carga; rutas ajenas → **Sin permiso** | Playwright `e2e/role-module-matrix.spec.ts` |
| **2 — Flujos** | Pantallas clave muestran UI útil (tablas/títulos/estado caja) | Playwright `e2e/role-flows.spec.ts` |
| **3 — Operación crítica** | Cobro + ticket + ruta mapa + PDF | Ya firmado en [SMOKE-SIGN-OFF.md](./SMOKE-SIGN-OFF.md) |

```powershell
cd Credito/modern/Credito.Modern.Web
$env:CREDITO_E2E_BASE_URL = "https://credito-personal.vercel.app"
$env:CREDITO_E2E_PASSWORD = "***"   # en prueba: 123.
npx playwright test e2e/role-module-matrix.spec.ts e2e/role-flows.spec.ts --workers=1
```

**Nota:** el login UI desde IP no autorizada (`Auth` / cliente acceso) puede fallar en headless; la suite siembra JWT vía `POST /api/v1/auth/login` y valida la SPA con sesión real.

## Perfiles representativos (no hace falta probar 30 usuarios idénticos)

En prod casi todos los gestores comparten el mismo menú `ANALISTA|CAJA`. Se elige **un usuario por perfil de ACL distinto**:

| Perfil | Usuario | Roles | Por qué |
|--------|---------|-------|---------|
| **ADMIN** | `RMANTILLA` | ADMINISTRADOR, ANALISTA, APROBADOR 1/2, CAJA | Menú completo (seguridad, bóveda, aprobación, caja chica, reportes) |
| **GESTOR_CAJA** | `YCERVANTES` | ANALISTA, CAJA | Operación diaria típica + prendario; sin admin |
| **APROBADOR_REPORTE** | `BQUISPE` | ANALISTA, APROBADOR 1, CAJA, REPORTE*, SALDOS CAJA, LECTURA_SALDO | Aprobación + reportes + saldos |
| **CAJERO_ENCARGADO** | `JVILLALOBOS` | ANALISTA, CAJA, CAJA CENTRAL, ENCARGADO, REPORTE* | Caja abierta + saldos + verificar pagos |

`ADMVENDIX` (ADMIN|APROBADOR|CAJA, sin prendario en menú) queda cubierto por solapamiento con ADMIN/`RMANTILLA`.

## Resultado 2026-10-10

### Capa 1 — Matriz ACL (Playwright)

**4/4 passed** (~1.5 min)

| Perfil | Allowed (carga OK) | Forbidden (403 Sin permiso) |
|--------|--------------------|-----------------------------|
| ADMIN | inicio, crédito*, clientes, caja*, bóveda, admin*, mantenimiento*, reportes*, informes* | — |
| GESTOR_CAJA | inicio, crédito/consulta/simulador/tareas/prendario*, clientes, caja/diario | admin/usuarios, admin/roles, bóveda, aprobar, saldos, caja chica, condonaciones |
| APROBADOR_REPORTE | aprobar, saldos, verificar pagos, reportes crédito/cobranza, consulta, diario, prendario, tareas | admin/usuarios, bóveda, caja chica |
| CAJERO_ENCARGADO | diario, saldos, verificar pagos, reportes crédito, consulta, clientes, prendario, tareas, simulador | admin/usuarios, bóveda, aprobar |

### Capa 2 — Flujos UI

Ejecutar / evidencia en misma corrida `role-flows.spec.ts` (usuarios, roles, oficinas, bóveda, aprobación, condonaciones, clientes, simulador, tareas, prendario, caja ABIERTO + botones ruta/PDF).

### Capa 0 — Inventario login (muestra)

| Usuario | Login | Roles (resumen) |
|---------|-------|-----------------|
| ADMVENDIX | OK (`123456`) | ADMIN + APROBADOR 1/2 + CAJA |
| RMANTILLA | OK (`123.`) | ADMIN + ANALISTA + APROBADOR 1/2 + CAJA |
| YCERVANTES / APARIONA / MVENTURA / … | OK (`123.`) | ANALISTA + CAJA |
| BQUISPE / BGARCIA / LPARIONA | OK (`123.`) | ANALISTA + APROBADOR 1 + REPORTE* + SALDOS |
| JVILLALOBOS | OK (`123.`) | CAJA + CAJA CENTRAL + ENCARGADO + REPORTE* |
| VFERNANDEZ | OK (`123.`) | CAJA + CAJA CENTRAL + REPORTE* |
| BLOPEZ | FAIL (hash PBKDF2, clave desconocida) | — |
| DSANCHEZ | intermitente (rate limit / reintentar) | ANALISTA+CAJA (caja ESPERANZA) |

## Qué NO cubre aún (alcance consciente)

- Transacciones destructivas en **todos** los módulos (alta cliente, desembolso, cierre caja, movimiento bóveda, condonación real).
- Roles inactivos en menú: VENDEDOR, ALMACEN, PROMOTOR (Estado=False / sin usuarios).
- Módulos legacy de **Ventas/Almacén** no asignados en `RolMenu` actual (menú prod no los entrega).
- Login UI desde IP no registrada (bloqueo de acceso por cliente).

Esas capas son **pruebas de negocio** planificadas por sprint; la matriz rol×módulo + smoke crítico ya demuestran que la plataforma moderna responde por ACL y carga operativa.

## Criterio de cierre plataforma

- [x] Inventario usuarios/roles/menú
- [x] Matriz ACL 4 perfiles × rutas menú
- [x] Flujos UI por perfil
- [x] Smoke crítico caja (cobro/ticket/ruta/PDF) — ver SMOKE-SIGN-OFF
- [ ] Rotar claves expuestas en chat
- [ ] (Opcional) Playwright CI nightly con secretos
