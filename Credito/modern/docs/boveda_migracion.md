# Módulo Bóveda — Legacy → Modern

Spec SSD (actores, aceptación, go-live): [docs/ssd/SSD-04-boveda.md](ssd/SSD-04-boveda.md). Este archivo conserva el mapa fino de pantallas y fases.

Paridad con `Web/Controllers/BovedaController.cs` y `Views/Boveda/Index.cshtml`.

## Mapa

| Legacy | Modern | API |
|--------|--------|-----|
| `/Boveda/Index` | `/tesoreria/boveda` | Varios bajo `/api/v1/credito/*` |
| Bloque «ESTADO DE DINERO» | `BovedaEstadoDineroPanel` | `GET /credito/boveda-estado-dinero` |
| Grilla cierres bóveda | `BovedaHistorialGrillas` (tabla 1) | `GET /credito/boveda-listar` |
| Grilla movimientos | Historial (tabla 2) | `GET /credito/rpt-movimiento-boveda` |
| Grilla saldos caja | Historial (tabla 3) | `GET /credito/saldos-caja-diario-boveda` |
| Operaciones (tabs) | `BovedaOperacionesPanel` | ingreso/egreso, transferencias, cierre |
| Informe movimientos | `/tesoreria/movimiento-boveda` | PDF/CSV existentes |
| Menú Crédito → Clientes / Bóveda | Hub `/credito` + enlace Bóveda | `legacyRoutes` `/Boveda/Index` |

## Pantalla principal

1. **Estado de dinero** — KPIs: bóveda, caja chica, cajas, plan pago, vencidos (total y franjas), total fondo. Enlaces a caja, saldos e informe vencido.
2. **Bóveda abierta** — saldos, temporal, resumen cuenta.
3. **Operaciones** — ingreso/egreso, transferencia caja/caja chica, **entre bancos**, cierre, bóveda temporal (si aplica).
4. **Historial** — selección de bóveda + movimientos + saldos caja diario de la sesión.

## API nuevas (2026)

- `GET /api/v1/credito/boveda-estado-dinero?oficinaId=` — `BovedaEstadoDineroReadService` (cajas abiertas, caja chica, `usp_ObtenerMontoPendientePlanPago`, cartera vencida).
- `GET /api/v1/credito/boveda-listar?oficinaId=&page=&pageSize=` — historial paginado por oficina.
- `POST /api/v1/credito/transferir-boveda-bancos` — `usp_RegistrarTransferenciaBancos` + `usp_ActualizarSaldosBoveda`.

## UI

- `styles/boveda-module.css` — KPIs y grillas (tema `#114885`).
- `CredixPage` + `CredixPanel` como Clientes/Tareas.
- Resumen de cuenta: tarjetas por entidad con iconos SVG inline modernos en `resumenCuentaBrandIcons.tsx` (Yape, Plin, Interbank, BCP, BN, efectivo, etc.). Para logos oficiales de marca, ampliar ese archivo o añadir un componente por `variant`.
- Resumen de cuenta: tarjetas por entidad con iconos SVG inline modernos en `resumenCuentaBrandIcons.tsx` (Yape, Plin, Interbank, BCP, BN, efectivo, etc.). Para logos oficiales de marca, ampliar ese archivo o añadir un componente por `variant`.

## Checklist de paridad (2026)

| Funcionalidad legacy | Estado modern |
|---------------------|---------------|
| KPIs estado de dinero | Completo |
| Saldos bóveda (ini/ent/sal/fin) | Completo (`BovedaSaldosGrid`) |
| Resumen cuenta (`usp_ResumenCuentaBoveda`) | Completo (`BovedaResumenCuenta`, tarjetas + iconos marca) |
| Ingreso / egreso | Completo |
| Transferir a caja | Completo |
| Transferir a caja chica | Completo |
| Transferir entre bancos | Completo (`usp_RegistrarTransferenciaBancos`, catálogo tabla 13) |
| Bóveda temporal (asignar / transferir / cerrar) | Completo |
| Transferir bóveda inter-oficina | Completo (ID destino, ver diferencia) |
| Aceptar / rechazar transferencia | Completo |
| Cierre principal + validación saldos | Completo |
| Grilla historial bóvedas | Completo |
| Grilla movimientos + ticket PDF | Completo |
| Grilla saldos caja diario sesión | Completo (`UsuarioAsignadoId` en SQL) |
| Informe movimientos (página + PDF) | Completo |

## Checklist de paridad (2026)

| Funcionalidad legacy | Estado modern |
|---------------------|---------------|
| KPIs estado de dinero | Completo |
| Saldos bóveda (ini/ent/sal/fin) | Completo (`BovedaSaldosGrid`) |
| Resumen cuenta (`usp_ResumenCuentaBoveda`) | Completo (`BovedaResumenCuenta`, tarjetas + iconos marca) |
| Ingreso / egreso | Completo |
| Transferir a caja | Completo |
| Transferir a caja chica | Completo |
| Transferir entre bancos | Completo (`usp_RegistrarTransferenciaBancos`, catálogo tabla 13) |
| Bóveda temporal (asignar / transferir / cerrar) | Completo |
| Transferir bóveda inter-oficina | Completo (ID destino, ver diferencia) |
| Aceptar / rechazar transferencia | Completo |
| Cierre principal + validación saldos | Completo |
| Grilla historial bóvedas | Completo |
| Grilla movimientos + ticket PDF | Completo |
| Grilla saldos caja diario sesión | Completo (`UsuarioAsignadoId` en SQL) |
| Informe movimientos (página + PDF) | Completo |

## Diferencias conocidas

- Transferencia inter-oficina: legacy combo de oficina destino; moderno usa **ID bóveda destino** (`transferir-boveda`).
- Aceptación de transferencias pendientes: `aceptarTransferenciaBoveda` con `bovedaMovTempId`.
- Transferencia entre bancos: el MVC hardcodea IDs 1–8 de Huanta; el moderno lista `MAESTRO.ValorTabla` tabla 13. Tras el SP se llama `usp_ActualizarSaldosBoveda` (el BL legado no lo hacía).
- `POST /api/v1/credito/transferir-boveda-bancos` — paridad `Boveda/RegistrarTransferenciaBancos`.

## Pruebas

- `BovedaEndpointTests`: 401 en endpoints sensibles (incl. `boveda-estado-dinero`, `boveda-listar`).
