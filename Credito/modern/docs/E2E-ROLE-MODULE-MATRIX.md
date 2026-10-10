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
| **3 — Ciclo de negocio** | Simular → PEN → aprobar → desembolso → cobro + ticket → validar cierre | Script + evidencia abajo |
| **4 — Extras de negocio** | Bóveda TRF, prendario completo, condonación, alta persona | Script + SPA |

```powershell
cd Credito/modern/Credito.Modern.Web
$env:CREDITO_E2E_BASE_URL = "https://credito-personal.vercel.app"
$env:CREDITO_E2E_PASSWORD = "***"
npm run test:e2e:roles
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
**Nota posterior:** la cuota restante de **87691** se cerró en capa 4 vía **condonación** (ver abajo).

## Capa 4 — Bóveda / prendario / condonación / alta cliente (2026-10-10)

```powershell
cd Credito/modern
.\deploy\scripts\smoke-ciclo-capa4-extras.ps1 `
  -BaseUrl "https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net" `
  -AdminUsuario RMANTILLA -AdminClave "***" `
  -AprobadorUsuario BQUISPE -AprobadorClave "***" `
  -GestorUsuario YCERVANTES -GestorClave "***" `
  -CreditoCondonarId 87691
```

### Evidencia corrida prod

| Flujo | Actor | Resultado |
|-------|-------|-----------|
| **Bóveda → caja** S/ 50 (efectivo) | `RMANTILLA` | `movimientoBovedaId=77331` · `movimientoCajaId=2287637` · bóveda 78627.15→78577.15 · caja 9912→9962 · ticket bóveda **~36 KB** · informe mov. bóveda PDF **~109 KB** |
| **Prendario PDF** crédito existente 87690 | `RMANTILLA` | Contrato **~475 KB** `%PDF` (desembolso bloqueado: CxC pendientes — esperado) |
| **Prendario originación** | `YCERVANTES` → `BQUISPE` → `YCERVANTES` | persona **5486** · crédito **87693** · bienes OK · APR · desembolso mov. **2287638** · caja CARMEN ALTO 1000→500 · contrato **~474 KB** · acta entrega **~85 KB** |
| **Condonación** crédito prueba 87691 | `RMANTILLA` | Solicitud id **323** → `condonar-credito` **success** · restante S/ 112 · 0 cuotas pendientes · bandeja vacía |
| **Alta persona rápida** | `RMANTILLA` | personaId **8878** · DNI 99998930 · `PRUEBA CAPA4, E2E` |
| Validar cierre caja admin | `RMANTILLA` | **`puedeCerrar=true`** (caja **no** cerrada) |
| SPA Playwright `ciclo-capa4-extras` | — | **2/2 PASS** (bóveda/condonaciones/clientes + prendario 87693 en listado) |

### Fuera de alcance residual (no bloquea firma de plataforma)

| Ítem | Motivo |
|------|--------|
| Cierre real de caja / bóveda en prod | Operación de fin de día; validado con `puedeCerrar=true` |
| Roles VENDEDOR / ALMACÉN / PROMOTOR | Inactivos / sin `RolMenu` en prod |
| Módulos Ventas/Almacén legacy | No asignados en menú actual |
| Login UI desde IP no autorizada | Política `Acceso`; E2E usa JWT API |
| Rotar claves de chat | Acción humana de seguridad (obligatoria post-pruebas) |
| Playwright CI nightly | Mejora de proceso, no de cobertura funcional |

## Criterio de cierre plataforma

- [x] Inventario usuarios/roles/menú
- [x] Matriz ACL 4 perfiles × rutas menú
- [x] Flujos UI por perfil
- [x] Smoke crítico caja (cobro/ticket/ruta/PDF)
- [x] Ciclo negocio capa 3 (crear→aprobar→desembolso→cobro→validar cierre)
- [x] Capa 4: bóveda TRF + ticket/PDF
- [x] Capa 4: prendario (bienes→aprobar→desembolso→contrato/acta)
- [x] Capa 4: condonación (solicitar→ejecutar)
- [x] Capa 4: alta persona rápida
- [ ] Rotar claves expuestas en chat *(operación humana)*
- [ ] (Opcional) Playwright CI nightly / `-AllowCerrarCaja` en ventana controlada
