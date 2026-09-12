# SSD-04 — Tesorería / bóveda

**Estado:** as-built · 2026-09-11  
**Código:** `/tesoreria/boveda`, `/tesoreria/movimiento-boveda` · `/api/v1/credito/*` (bóveda)  
**Fuente legacy:** `BovedaController`, `Views/Boveda/Index.cshtml`  
**Doc de ingeniería:** [boveda_migracion.md](../boveda_migracion.md)

## 1. Propósito y actores

Fondo de oficina: saldos, movimientos, transferencias (caja, caja chica, bancos, otra oficina) y cierre.

| Rol | Uso |
|-----|-----|
| Encargado / administrador | Operar bóveda, temporal (alta de encargado), cierre, transferencias |
| Gestor (con menú BOVEDA) | Misma pantalla; la API de movimiento es `CreditoUser` |
| Cajero | No opera bóveda desde caja diario (eso es SSD-03) |

## 2. Alcance

**Entra**

- Estado de dinero, saldos, resumen de cuenta, historial
- Ingreso/egreso, transferir caja / caja chica / entre bancos / inter-oficina
- Bóveda temporal (asignar, confirmar, cerrar)
- Informe y tickets PDF de movimiento

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

## 7. Criterios de aceptación

- [ ] Ítem BOVEDA abre estado de dinero de la oficina de la sesión.
- [ ] Ingreso/egreso y transferencias a caja/chica llaman los `usp_*` del MVC.
- [ ] Entre bancos lista tabla 13, registra TRF y actualiza saldos bóveda.
- [ ] Informe movimientos accesible desde bóveda (mismo permiso de menú).
- [ ] Caja diario no abre `/tesoreria/movimiento-boveda`.
- [ ] Ticket PDF de un movimiento de bóveda usa API JWT.

## 8. Desviaciones

- 2026-09-10 — transferencia entre bancos + `usp_ActualizarSaldosBoveda` ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-09-10 — informe movimientos no exige ítem de menú extra
- 2026-09-09 — comprobante de movimiento (tabla incorrecta, corregida)

Producto: destino inter-oficina por ID de bóveda, no combo de oficina.

## 9. Pruebas y evidencia

- API: `bovedaendpointtests`, `bovedamovendpointtests`, `bovedatemporalendpointtests`
- SPA: `menuRouteAccess.test.ts` (bóveda vs caja diario)
- Smoke: abrir bóveda, ver TRF en historial, PDF movimientos

## 10. Go-live

Listo en Development. Cutover: encargado opera una transferencia caja y una entre bancos de prueba.
