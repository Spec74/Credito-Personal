# SSD-13 — Experiencia del sistema Credix (todas las pantallas + red)

**Estado:** spec de **cambio** · 2026-10-20 · **ámbito: SSD-01 … SSD-12**  
**Spec Kit:** [spec.md](../speckit/001-experiencia-operativa/spec.md) · [modulos.md](../speckit/001-experiencia-operativa/modulos.md)  
**Constitución UX:** [CONSTITUTION-UX.md](../speckit/CONSTITUTION-UX.md)

No es un spec de prendario. Es el cambio de **producto** que gerencia pidió para **todo** el moderno: interfaz al límite, años, sin ruido, flujos simples, sistema completo, multisucursal.

Los SSD-00…12 siguen siendo el as-built funcional. Este SSD define **cómo se opera y se ve** ese sistema.

## 1. Propósito y actores

Credix = puesto de trabajo de la empresa (crédito personal, prendario, caja, tesorería, informes, admin; ventas/almacén cuando el menú lo tenga).

| Rol | Home |
|-----|------|
| Cajero / gestor | Caja, cobro, desembolso, (venta) |
| Analista | Originación + cartera |
| Aprobador | Cola + ficha |
| Encargado | Bóveda, cajas, vencidos |
| Almacén | Entradas / stock (si hay menú) |
| Admin / gerencia | Red de sedes + 6 KPI |

## 2. Alcance

**Entra (todo el sistema)**

- Estándar visual único (anatomía, densidad, cero ruido) en **cualquier** ruta
- RoleHome + chrome de sucursal/caja
- Patrones: Page, WorkCanvas, Queue, Record, MoneyConfirm
- Hilos: originación personal, originación prendaria, cobro, tesorería, cliente
- Wizard sucursal + fondeo por sede + tablero red
- Informes de decisión vs archivo
- Completar huecos (prenda post-desembolso, cliente cruzado, PLA) con contrato de datos primero
- Ventas/almacén: mismo estándar, mismo día de caja

**No entra**

- Rehacer `usp_*` / EDMX / RDLC / app nativa / IA que decide crédito
- Big-bang de 95 páginas en un PR (adopción al tocar)

## 3. De MVC-migrado a Work OS

| Antes | Después |
|-------|---------|
| Hub de cards por módulo | Home = colas del rol |
| Cada pantalla con su “estilo de migración” | Una anatomía (modulos.md) |
| Simular / aprobar / caja / clientes desconectados | Hilos (spec US-3…US-6) |
| Oficina 1 implícita | Chrome + JWT + wizard de sede |
| 30 informes | 6 KPI + búsqueda |

Rutas exactas de menú **no se inflan**. Se re-componen.

## 4. Contrato de datos

A–B: sin tablas nuevas. C: versionado **antes** de UI — [contrato-datos-c.md](../speckit/001-experiencia-operativa/contrato-datos-c.md).

Reusa JWT, `UsuarioOficina`, bóveda, caja, `usp_*` de crédito/caja/informes, `Prenda`, maestros, ventas/almacén.

| Dato | En C (2026-10-20) |
|------|-------------------|
| LTV | Observado (`MontoCredito / MontoTasacion`). Tope = 100%. Sin parámetro de producto. |
| Renovación | No. Prórroga `usp_ProrrogarCredito` sigue en consulta. |
| Devolución | `usp_DevolverPrenda` + comprobante PDF. |
| Remate | Filtro `categoria` sobre el CASE ya existente. |
| Hoja | Contrato y acta QuestPDF. Sin TEA/TCEA. |
| Cliente cruzado | `MAESTRO.Persona` global. Operar cartera solo con `vendix:oficina_id`. |
| PLA | Piso legal D.S. 020-2017-JUS (US$ 10 000 / 50 000). Bitácora inusual. Sin cifra inventada en S/. |

## 5. API y SPA

Orquestación de APIs **ya existentes** (SSD-01…12). Composición nueva:

| Ruta | Patrón |
|------|--------|
| `/inicio` | RoleHome |
| `/login` | Page corta |
| `/clientes*` | Record / drawer |
| `/credito/simulador` | WorkCanvas |
| `/credito/consulta` | Record |
| `/credito/aprobar` | Queue |
| `/credito/prendario/nuevo` | WorkCanvas |
| `/caja/diario`, `/caja/cobro` | Page / Queue |
| `/caja/verificar-pagos` | Queue |
| `/tesoreria/boveda` | MoneyConfirm + red |
| `/informes`, `/reportes/*` | Búsqueda + Page archivo |
| `/ventas/rapida`, `/almacen/entrada` | WorkCanvas |
| `/mantenimiento/*` | Page + wizard sede |

## 6. Seguridad

JWT oficina/usuario. Menú exacto. Tablero red ≠ ver todo. Validación SSD-12 en **todas** las formas.

## 7. Criterios de aceptación

- [x] RoleHome para cada rol P0 (no mapa de 12 módulos como centro)
- [x] Chrome con sucursal (y caja si hay sesión); «Cambiar sede» re-login
- [x] P0 en estándar: login, inicio, clientes, consulta, simulador, aprobar, diario, cobro, bóveda, prendario nuevo
- [x] Originación personal y prendaria en WorkCanvas
- [x] Cobro y verificar alcanzables desde Hoy
- [x] Wizard sede; fondeo por nombre de oficina
- [x] Densidad AA; 0 hints eternos en P0
- [x] Smoke caja oficina ≠ 1
- [x] Toda PR de UI deja la página en estándar ([checklist](../speckit/001-experiencia-operativa/checklist.md)) — review 2026-10-21

## 8. Desviaciones

Producto, no financieras. PLAN-MULTISUCURSAL, SSD-06 WhatsApp, bitácora existente.

- **2026-10-24 — alcance de red as-built:** usuarios (SSD-08), cierre gerencial y morosos (SSD-07) ya aíslan sucursal vs Central. No ensancha RoleHome.
- **2026-10-24 — móvil:** el hueco título→cuerpo y el traductor se resuelven en [SSD-15](SSD-15-movil-shell.md), no aquí.

## 9. Pruebas

[tasks.md](../speckit/001-experiencia-operativa/tasks.md) · browser por **hilo** (originación, cobro, tesorería, inicio de cada rol), no un screenshot.

## 10. Go-live

No bloquea SSD-00. Oleadas sobre el moderno en marcha.

## 11. Spec Kit

```
constitution → CONSTITUTION-UX.md
specify      → spec.md + modulos.md
plan         → plan.md
research     → research.md
checklist    → checklist.md
tasks        → tasks.md
analyze      → antes de Oleada C
implement    → PRs por hilo de sistema
converge     → A+B (P0 de todos los módulos críticos)
```
