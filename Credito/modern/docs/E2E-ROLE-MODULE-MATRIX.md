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
| **3 — Ciclo de negocio** | Simular → PEN → aprobar → desembolso → cobro + ticket → validar cierre | Script + evidencia abajo; [SMOKE-SIGN-OFF.md](./SMOKE-SIGN-OFF.md) |

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

## Capa 3 — Ciclo de negocio (2026-10-10)

Script reutilizable:

```powershell
cd Credito/modern
.\deploy\scripts\smoke-ciclo-credito-capa3.ps1 `
  -BaseUrl "https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net" `
  -OperadorUsuario RMANTILLA -OperadorClave "***" `
  -AprobadorUsuario BQUISPE -AprobadorClave "***" `
  -PersonaId 48 -OficinaId 1 -MontoCredito 200
# Por defecto NO cierra la caja. Solo con -AllowCerrarCaja.
```

### Evidencia corrida prod

| Paso | Actor | Resultado |
|------|-------|-----------|
| Simulador S/ 200 · 2 cuotas · TEM 6% · prod CREDI RAPP | `RMANTILLA` | Plan 2× S/ 112 |
| Solicitud + crédito | `RMANTILLA` / caja **29578 CAJA CENTRAL 1** | **creditoId 87691** estado PEN |
| Aprobar (`opcion=1`) | `BQUISPE` (APROBADOR 1) | `ok=true` → APR |
| Validar + realizar desembolso | `RMANTILLA` | `movimientoCajaId=2287635` · saldo 10000→9800 |
| Cobro CUOTA 1 (planPago **1735681**, S/ 112, efectivo) | `RMANTILLA` | `resultId=2287636` · ticket PDF **~35 KB** `%PDF` |
| Validar cierre | `RMANTILLA` | Tras rechazar PEN huérfano 87692: **`puedeCerrar=true`** |
| Cerrar caja | — | **No ejecutado** (cajas operativas del día) |
| Limpieza | `BQUISPE` | Rechazo crédito accidental **87692** |

Cliente usado: personaId **48** — FERNANDEZ LEON, RUTH MARIA (DNI 76738856).

### Qué queda fuera (consciente)

- Cierre real de caja en prod (validado sí; ejecutado no).
- Alta de cliente nuevo, bóveda, condonación, prendario con bienes.
- Roles inactivos (VENDEDOR / ALMACÉN / PROMOTOR) y menús Ventas/Almacén no asignados en `RolMenu`.
- Login UI desde IP no autorizada (suite usa JWT API).

## Criterio de cierre plataforma

- [x] Inventario usuarios/roles/menú
- [x] Matriz ACL 4 perfiles × rutas menú
- [x] Flujos UI por perfil
- [x] Smoke crítico caja (cobro/ticket/ruta/PDF) — ver SMOKE-SIGN-OFF
- [x] Ciclo negocio capa 3 (crear→aprobar→desembolso→cobro→validar cierre)
- [ ] Rotar claves expuestas en chat
- [ ] (Opcional) Playwright CI nightly con secretos
- [ ] (Opcional) `-AllowCerrarCaja` en ventana controlada post-operación
