# Tasks — 001-experiencia-operativa

**Spec Kit:** `/speckit.tasks` · 2026-10-20

Leyenda: `[ ]` no hecho · `[x]` hecho en código/docs.

## Oleada A — Sistema P0 (todas las rutas críticas)

- [x] T-A0 Documentar constitución, spec **de sistema**, inventario SSD-01…12, research, plan, SSD-13
- [x] T-A1 Inventario de ruido en **toda** la SPA (no solo prendario): `Alert` + subtitle + hints
- [x] T-A2 Tokens densidad + clase `body` (afecta todas las páginas)
- [x] T-A3 `CredixContextBar` (oficina + caja) en el shell
- [x] T-A4 Patrones `WorkCanvas` / `Queue` / `Record` / `MoneyConfirm`
- [x] T-A5 RoleHome en `/inicio` para **todos** los roles (cajero, analista, aprobador, encargado, admin)
- [x] T-A6 Aplicar estándar a P0: login, clientes, consulta, simulador, aprobar, diario, cobro, bóveda, prendario nuevo
- [x] T-A7 WorkCanvas crédito **personal** (simulador) y **prendario** (nuevo + resultado)
- [x] T-A8 Queue en aprobar **y** verificar-pagos
- [x] T-A9 Tests + browser 2026-10-20: cobro, diario, simulador, prendario (LTV), inicio admin + gestor YCERVANTES, cobro 390px. API `categoria=Vencido` ya filtra.

## Oleada B — Red + informes + resto de módulos

- [x] T-B1 Wizard “Nueva sucursal” (PLAN §5.1)
- [x] T-B2 Fondeo por **sede** (resolver bóveda abierta)
- [x] T-B3 Vista B-05 “Fondear sucursales”
- [x] T-B4 Tablero red: cartera, mora, caja, desembolsos, custodia, ventas si aplica
- [x] T-B5 Smoke B-02 oficina ≠ 1
- [x] T-B6 Hub `/informes` → búsqueda + recientes; 6 KPI en RoleHome
- [x] T-B7 Estándar visual en maestros, oficinas, usuarios (matriz sede × rol)
- [x] T-B8 Si hay menú: venta rápida + entrada almacén en WorkCanvas; atajo en Hoy
- [x] T-B9 Tests oficina Pichari/Huanta

## Oleada C — Completar producto (varios dominios)

Contrato: [contrato-datos-c.md](contrato-datos-c.md). Lo que no tiene `usp_*` queda bloqueado.

- [x] T-C0 Spec de datos **antes** de UI (LTV, renovación, devolución, remate, hoja resumen, PLA)
- [x] T-C1 LTV observado en canvas/listado (tope vigente = 100%; no se inventó otro %)
- [x] T-C2 `usp_DevolverPrenda` + comprobante PDF (no DES; `FechaMod`/`UsuarioModId`)
- [x] T-C3 Cola remate = filtro de la lista existente (`Rematado` / `Vencido` / `PorVencer`)
- [x] T-C4 Renovación = prórroga existente (`usp_ProrrogarCredito`) en consulta y gestión
- [x] T-C5 Hoja = contrato/acta QuestPDF ya existentes (sin TEA/TCEA)
- [x] T-C6 Búsqueda de persona global + aviso de no duplicar; cartera solo en la sede del JWT
- [ ] T-C7 (Opcional) bitácora PLA umbral — **sin umbral UIF**: no se inventa tabla/USP

## Oleada E — Estándar en el resto (SSD-03…11)

- [x] T-E1 Quitar subtitles eternos en caja restante, tesorería, informes, admin, maestros, ventas, almacén
- [x] T-E2 Recorrido 2026-10-20 RMANTILLA oficina 1: `/informes` + cobro diario, bóveda/red/fondeo, hub tesorería, admin/oficinas/usuarios, reportes crédito. Ventas/almacén/maestros 403 (sin ítem de menú; T-Q1). ACL: hub `/informes` si hay Reportes crédito; hub `/tesorería` si hay Bóveda. Hoy ya no apunta venta/almacén.

## Oleada D — Cerrar P0 restante (sin inventar USP)

- [x] T-D1 Tope/bloqueo **inline** en simulador (ficha `ClienteDetalle`; no se inventa tope)
- [x] T-D2 Quitar subtitles/hints eternos en consulta, gestionar, condonar, tareas, clientes, aprobar
- [x] T-D3 RoleHome analista → `/credito/tareas`

## Calidad Spec Kit

- [x] T-Q1 Cerrado 2026-10-20: LTV 100%; renovación=prórroga; ventas of.1 no; wizard SPA
- [x] T-Q2 Checklist [checklist.md](checklist.md) — review 2026-10-21 (P0 A–E + cambio de sede)
- [x] T-Q3 `/speckit.analyze` antes de Oleada C (contrato datos vs UI) → [contrato-datos-c.md](contrato-datos-c.md)
- [x] T-Q4 `/speckit.converge` A+B+C+E. T-C7 sigue sin umbral UIF.
