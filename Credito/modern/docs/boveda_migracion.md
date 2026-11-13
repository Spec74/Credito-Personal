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
2. **Bóveda abierta** — saldos, temporal, resumen cuenta (pantalla = **principal**).
3. **Operaciones** — ingreso/egreso, a caja, a analista (embudo→efectivo), caja chica, entre medios, temporal, red, cierre.
4. **Historial** — selección de bóveda + movimientos + saldos caja diario de la sesión.

## Checklist de paridad (2026-11-13)

| Funcionalidad legacy | Estado modern |
|---------------------|---------------|
| KPIs estado de dinero | Completo |
| Saldos bóveda (ini/ent/sal/fin) | Completo |
| Resumen cuenta / medios | Completo (`boveda-medios-red`) |
| Ingreso / egreso | Completo |
| Transferir a caja (mismo medio) | Completo |
| Transferir a analista (embudo → efectivo) | Completo |
| Transferir a caja chica | Completo (tab visible) |
| Transferir entre bancos / medios | Completo (tabla 13) |
| Bóveda temporal (asignar / transferir / cerrar) | Completo (`UsuarioId` + retorno a temporal) |
| Transferir bóveda inter-oficina | Completo (inmediato; ID bóveda destino) |
| Pendientes leftover | Solo **anular** (`flagAceptar=2`); no hay aceptar/rechazar |
| Cierre principal + validación | Completo (cuadre Σ medios) |
| Cierre temporal | Completo (validación acotada a temporal) |
| Grillas historial + ticket PDF | Completo |
| Informe movimientos | Completo |

## Diferencias conocidas

- Transferencia inter-oficina: legacy combo de oficina destino; moderno usa **ID bóveda destino**.
- Pendientes: solo anula leftovers (`flag 2`). El USP no acepta (no hay flag 1).
- Entre bancos: MVC hardcodeaba IDs Huanta; moderno lista `ValorTabla` 13 + `usp_ActualizarSaldosBoveda`.
- A caja: mismo medio bóveda→caja. A analista: embudo a efectivo (paridad MVC).
- Con temporal abierta, retorno de cajas va a **temporal** (`BovedaOperativaSql` = `usp_CerrarCajasDiarios`).

## Ciclo temporal (cero céntimos)

1. Principal → temporal  
2. Temporal → caja (si ENCARGADO)  
3. Retorno / cierre cajas → temporal  
4. Cerrar temporal → principal  

Deploy: `2026-11-13-usp-cerrar-boveda-temporal-centimos.sql` (+ scripts caja ROUND 2).

## Pruebas

- `BovedaEndpointTests`, `BovedaMovEndpointTests`, `BovedaTemporalEndpointTests`
- SPA: `menuRouteAccess.test.ts` (bóveda vs caja diario)
