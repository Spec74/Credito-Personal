# SSD-04 — Tesorería / bóveda

**Estado:** as-built · código completo 2026-11-13 (temporal/retorno/caja chica/analista embudo/auth saldos) · validación 2026-09-27 ([SSD-12](SSD-12-validacion.md)) · **multisucursal 2026-10-16…24**  
**Código:** `/tesoreria/boveda`, `/tesoreria/movimiento-boveda` · `/api/v1/credito/*` (bóveda)  
**Fuente legacy:** `BovedaController`, `Views/Boveda/Index.cshtml`  
**Doc de ingeniería:** [boveda_migracion.md](../boveda_migracion.md) · [PLAN-MULTISUCURSAL-PRENDARIO.md](../migration/PLAN-MULTISUCURSAL-PRENDARIO.md)

## 1. Propósito y actores

Fondo de oficina: saldos, movimientos, transferencias (caja, caja chica, bancos, otra oficina) y cierre.

| Rol | Uso |
|-----|-----|
| Encargado / administrador | Opera bóveda principal; asigna **bóveda temporal** a un responsable |
| Responsable de temporal | Recibe rol `ENCARGADO` al asignar; abre cajas desde Saldos; el monto sale de la **temporal** (re-login tras alta) |
| Gestor (con menú BOVEDA) | Misma pantalla; la API de movimiento es `CreditoUser` |
| Cajero | No opera bóveda desde caja diario (eso es SSD-03) |

**Ciclo de dinero con temporal (cero céntimos, sede = JWT):** fondeo principal→temporal → asignar caja (débito temporal si ENCARGADO) → operación → retorno/cierre de cajas a **temporal** → cuadre → cierre temporal→principal. Ver [BITACORA 2026-11-13](../migration/BITACORA-DESVIACIONES.md).

## 2. Alcance

**Entra**

- Estado de dinero, saldos, resumen de cuenta, historial
- Ingreso/egreso, transferir caja / caja chica / entre bancos / inter-oficina
- Bóveda temporal (asignar, confirmar, cerrar)
- Informe y tickets PDF de movimiento ([tickets_comprobantes.md](../tickets_comprobantes.md): oficina real, agente, medio tipado, E/S)

**No entra**

- Caja diario ni saldos de cajas asignadas como pantalla → SSD-03
- Informes de cartera vencida como módulo reportes → SSD-07

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Boveda/Index` | `/tesoreria/boveda` | Menú BOVEDA (módulo CREDITO) |
| `Reporte/ReporteMovimientoBoveda` | `/tesoreria/movimiento-boveda` | El ítem bóveda habilita el informe (no hace falta menú extra) |
| `RegistrarTransferenciaBancos` | pestaña «Entre bancos» | Catálogo `ValorTabla` 13; no IDs 1–8 de Huanta |
| Combo oficina destino | ID bóveda destino | Ver bitácora / diferencias |

## 4. Contrato de datos

| Procedimiento / tabla | Uso |
|-----------------------|-----|
| `usp_ResumenCuentaBoveda` | Tarjetas por entidad |
| `usp_ObtenerMontoPendientePlanPago` | KPI estado de dinero |
| Ingreso/egreso, transferir caja/chica, cierre, temporal | Mismos `usp_*` que `BovedaController` |
| `usp_RegistrarTransferenciaBancos` | Entre bancos |
| `usp_ActualizarSaldosBoveda` | Tras TRF bancos (el BL MVC no lo llamaba) |
| `usp_RptMovimientoBoveda` | Informe / PDF |

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET boveda-estado-dinero`, `GET boveda-listar` | `CreditoUser` | `/tesoreria/boveda` |
| `POST ingreso-egreso-boveda` | `CreditoUser` | Operaciones |
| `POST transferir-boveda-caja`, `transferir-boveda-caja-chica` | `CreditoUser` | Operaciones |
| `POST transferir-boveda-bancos` | `CreditoUser` | Entre bancos |
| `POST transferir-boveda` (inter-oficina o aceptar temp si `bovedaMovTempId > 0`) | `CreditoUser` | Operaciones |
| `POST asignar-boveda-temporal` | `CreditoRolEncargadoOAdministrador` | Temporal |
| `GET rpt-movimiento-boveda*` (JSON/CSV/PDF) | `CreditoUser` | `/tesoreria/movimiento-boveda` |

La puerta de negocio es el menú BOVEDA en la SPA. La API replica al MVC: autenticado basta salvo asignar temporal (encargado o admin).

## 6. Seguridad

- `oficinaId` del JWT en lecturas de estado e historial.
- Hub `/tesoreria` y menú `/boveda` habilitan `/tesoreria/movimiento-boveda`.
- Caja diario **no** habilita el informe de movimiento bóveda.
- Escrituras de importe/glosa: `TesoreriaValidacion` + panel SPA con `formRules` ([SSD-12](SSD-12-validacion.md)).

## 7. Criterios de aceptación

> **Documentación:** criterios redactados al 100%. Las casillas registran **evidencia de ejecución** (Development / preprod), no huecos de spec.

- [ ] Ítem BOVEDA abre estado de dinero de la oficina de la sesión.
- [ ] Ingreso/egreso y transferencias a caja/chica llaman los `usp_*` del MVC.
- [ ] Entre bancos lista tabla 13, registra TRF y actualiza saldos bóveda.
- [ ] Informe movimientos accesible desde bóveda (mismo permiso de menú).
- [ ] Caja diario no abre `/tesoreria/movimiento-boveda`.
- [ ] Ticket PDF de un movimiento de bóveda usa API JWT y muestra oficina, agente, medio tipado, glosa y entrada/salida (no placeholders legacy).
- [ ] Cierre de cajas diarios en oficina ≠ 1 genera `TRE` en la bóveda de esa oficina (no solo oficina 1).
- [ ] Desde central, transferencia interoficina a Pichari acredita saldo en destino de inmediato.

## 8. Desviaciones

- 2026-09-10 — transferencia entre bancos + `usp_ActualizarSaldosBoveda` ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-09-10 — informe movimientos no exige ítem de menú extra
- 2026-09-09 — comprobante de movimiento (tabla incorrecta, corregida)
- **2026-10-16 — multisucursal:** en prod el destino del cierre iba a bóveda de `OficinaId=1`; ahora acredita en la bóveda de `@OficinaId`, conservando CTE `MOVS_BASE` (saldo inicial + movimientos). Deploy: `2026-10-16-usp-cerrar-cajas-diarios-oficina.sql`.
- **2026-10-16 — fondeo central:** transferencia inter-oficina (`flagAceptar = 0`) es **inmediata** (TRS origen + TRE destino). Pendientes no es el camino de fondeo: solo anula leftovers (`flag 2`).
- **2026-10-23 — Pendientes:** sin aceptación inventada. API 400 si tempId > 0 y flag ≠ 2; destino debe ser la oficina del token.
- **2026-10-24 — marcas 1–5:** wordmark oficial PNG (Yape, BCP, Interbank, BN, CASH). 6–8 no son catálogo vigente. Huanta = `OficinaId`, no medio legado.
- **2026-11-08/09 — ticket bóveda:** deja de hardcodear oficina/usuario; imprime `DisplayLabel` del medio y montos E/S ([BITACORA](../migration/BITACORA-DESVIACIONES.md)).
- **2026-11-13 — módulo al 100% código:** tab caja chica visible; analista embudo→efectivo; a caja mismo medio; retorno cajas→temporal; cierre temporal con validación propia; `saldos-caja-diario-boveda` exige `bovedaId` de la sede JWT.

Producto: destino inter-oficina por ID de bóveda, no combo de oficina.

### 8.1 Autonomía de sucursales (gerencia)

| Paso | Actor | Mecanismo |
|------|-------|-----------|
| Alta sucursal | Admin | `/mantenimiento/oficinas` → crea `MAESTRO.Oficina` + `CREDITO.Boveda` |
| Fondear | Central (Huamanga) | Interoficina → bóveda de Pichari / Huanta / Andahuaylas |
| Operar | Encargado sucursal | Transferir / asignar a cajeros; Caja Diario |
| Cerrar día | Sucursal | Cerrar cajas → bóveda; cerrar bóveda arrastra saldo |
| Ida y vuelta | Central ↔ sucursal | Misma transferencia inter-oficina |

Checklist completo: [PLAN-MULTISUCURSAL-PRENDARIO.md §5.1](../migration/PLAN-MULTISUCURSAL-PRENDARIO.md).

## 9. Pruebas y evidencia

- API: `bovedaendpointtests`, `bovedamovendpointtests`, `bovedatemporalendpointtests`
- SPA: `menuRouteAccess.test.ts` (bóveda vs caja diario)
- Smoke: abrir bóveda, ver TRF/TRE en historial, Ticket con Medio + oficina, PDF movimientos

## 10. Go-live

Listo en Development. Cutover: encargado opera una transferencia caja y una entre bancos de prueba.
