# Spec — Experiencia del **sistema** Credix (todas las pantallas)

**Feature:** `001-experiencia-operativa`  
**SSD:** [SSD-13](../../ssd/SSD-13-experiencia-operativa.md)  
**Inventario:** [modulos.md](modulos.md) — SSD-01 … SSD-12  
**Tipo:** cambio de producto (no as-built) · Spec Kit `/speckit.specify`  
**Fecha:** 2026-10-20 (alcance **sistema**, no un módulo)

## Problema

Gerencia vio el avance moderno y pidió el **siguiente nivel de todo el sistema**: cada página ordenada, visible, usable, sin ruido; robusto **años**; flujos simples; ventaja competitiva; **multisucursal**.

Hoy Credix ya cubre auth, clientes, crédito personal, prendario, caja/cobro, bóveda, informes, admin, maestros, ventas y almacén. El fallo no es “falta prendario”: es que **95 pantallas aún se sienten de migración** (hubs, textos, pasos partidos) y la red de sedes se opera con checklists.

El legado era funcional para quien lo memorizó. El moderno debe ser la **herramienta de la empresa** (mostrador, campo, tesorería, gerencia).

## Visión

Un **Work OS** con el mismo Credix en **todas** las rutas:

- El menú concede permiso. La **home es el trabajo del rol**.
- Misma anatomía de página en caja, crédito, clientes, informes, ventas, almacén.
- La sucursal es contexto permanente.
- Se agregan sedes o productos **sin rediseñar**.

| Rol | Home (“Hoy”) | Sistema que toca |
|-----|----------------|------------------|
| Cajero / gestor | Caja sesión, cola cobro, desembolso APR, (venta si hay menú) | 01, 03, 02, 10 |
| Analista | Originación + cartera de la sede | 02, 05, 06 |
| Aprobador | Cola de solicitudes + ficha | 02 |
| Encargado de sede | Bóveda, cajas, vencidos, arqueo | 03, 04, 07 |
| Almacén / ventas | Entradas pendientes, stock bajo, venta del día | 10, 11 |
| Admin / gerencia | Red de sedes, fondeo, 6 KPI, usuarios×oficina | 01, 04, 07, 08 |

## User stories (sistema)

### US-1 — Cada página del sistema sin ruido (P0, transversal)

Como cualquier usuario, en **cualquier** ruta (`/caja/*`, `/credito/*`, `/clientes/*`, `/informes/*`, `/tesoreria/*`, `/ventas/*`, `/almacen/*`, `/admin/*`, `/maestros/*`) entiendo qué hacer en 2 segundos.

**Aceptación**

- Anatomía de [modulos.md](modulos.md) §Estándar visual: 1 H1, 1 primaria, 0–4 stats, error al campo, 0 párrafos de migración.
- Oficinas y caja en el chrome, no escondidas.
- Densidad compact/estándar/táctil en el shell (todas las páginas la heredan).
- Adopción: patrón listo + cada PR que toque una página la deja en estándar (no reescribir 95 archivos en un commit).

### US-2 — Entrar y trabajar (P0 · SSD-01)

Como cajero, analista o admin, `/inicio` es **mi día**, no un mapa de módulos.

**Aceptación**

- RoleHome sustituye el hub de cards como pieza central.
- `?vista=modulos` queda como respaldo (búsqueda de pantalla).
- Login: oficina clara; error de clave/oficina junto al campo.

### US-3 — Cliente una vez (P0 · SSD-05)

Como analista o cajero, no abandono el crédito/caja para “ir a clientes” si solo necesito un DNI.

**Aceptación**

- Alta/búsqueda embebida (drawer o canvas) desde originación, diario y venta rápida.
- Ficha = Record (pestañas: datos, créditos, ubicación). Sin scroll monstruo sin anclas.

### US-4 — Crédito personal en un hilo (P0 · SSD-02)

Como analista/aprobador/cajero, simulo, apruebo y desembolsar **sin perder la ficha**.

**Aceptación**

- Simulador = WorkCanvas (plan a la vista).
- Aprobar = Queue (lista + expediente).
- Consulta = Record (plan, cargos, aval, impresos).
- Tope/bloqueo/SBS se ven **antes** de generar, no en un informe aparte.

### US-5 — Prendario en el mismo estándar (P0 · SSD-06)

Mismo WorkCanvas que el crédito personal: cliente + N bienes + crédito + resultado/PDF. Listado = consulta. (Detalle de ciclo post-desembolso: US-11.)

### US-6 — Caja y cobranza de un hilo (P0 · SSD-03)

Como cajero, de Hoy paso a cobrar o desembolsar; verificar y chica no son islas.

**Aceptación**

- Hoy: sesión caja, pendientes de cobro, APR por desembolsar, transferencias por verificar.
- Diario y cobro campo conservan GPS/offline; no se re-busca el crédito.
- Arqueo / cierre: pasos numerados, MoneyConfirm.

### US-7 — Tesorería de la red (P0–P1 · SSD-04)

Como encargado o central, muevo efectivo **eligiendo sede**, no un ID.

**Aceptación**

- Transferencia: oficina destino + importe grande.
- B-05 fondeo consolidado. Pendientes incompletas no se venden como flujo.
- Cierre oficina ≠ 1: smoke B-02.

### US-8 — Informes de decisión, no un mall (P1 · SSD-07)

Como gerencia o encargado, veo 6–8 números que importan; el resto se busca.

**Aceptación**

- RoleHome gerencia: cartera, mora, caja, desembolsos, custodia, (ventas si aplica), por sede.
- Hub `/informes` = búsqueda + recientes, no 30 tiles iguales.
- Cierre gerencial y morosos empresa enlazados, no escondidos en ACL opaca.

### US-9 — Gobierno de sedes y personas (P0 · SSD-08, SSD-09)

Como admin, doy de alta una sucursal **completa** y veo roles por sede.

**Aceptación**

- Wizard: oficina → bóveda → usuarios/roles **de esa sede** → cajas → fondeo.
- Ficha usuario: matriz oficina × rol.
- Maestros: mismo Page; sin copy de más.

### US-10 — Ventas y almacén en el mismo día (P1 · SSD-10, SSD-11)

Como cajero/almacenero de una sede que vende, no cambio de “sistema mental”.

**Aceptación**

- Si el menú lo tiene: Hoy incluye venta del día y stock/alertas.
- Venta rápida muestra **la misma** caja de sesión que el diario.
- Entrada/salida: wizard con progreso; transferencia declara almacén **y** oficina.

### US-11 — Completar lo que falta (P1–P2, por dominio)

El sistema es funcional; “completo” para el sector microfinanzas + prenda + (opcional) comercio:

| Dominio | Hoy | Completar |
|---------|-----|-----------|
| Originación | 2–4 pantallas | WorkCanvas personal y prendario |
| Cobranza | Varias islas | Un hilo + verificar |
| Tesorería red | ID bóveda | Sede + B-05 + B-02 |
| Prenda post-desembolso | Custodia + WA 3d | LTV, devolución, remate, renovación |
| Cliente red | Por oficina | Consulta cruzada de existencia (no editar sin permiso) |
| Informes | Catálogo | KPI de decisión + archivo |
| Ventas/almacén | SPA lista, menú no en of. 1 | Mismo estándar el día que una sede los active |
| Cumplimiento | DNI / cliente | RO legal US$ 10k/50k + bitácora ROS (oficial) |

Cada ítem de datos nuevos: `usp_*`/tabla **antes** de UI.

### US-12 — Multisucursal es una propiedad, no un módulo (P0)

Huamanga, Pichari, Huanta, Andahuaylas: **misma** plantilla de pantallas.

**Aceptación**

- Chrome muestra la sede. JWT filtra. Consolidado solo con permiso.
- Alta de sede = wizard (US-9). Operación diaria = US-6 + US-7.
- Ninguna página nueva asume `OficinaId = 1`.

## Fuera de alcance

- Reescribir BD / abandonar `usp_*`.
- App nativa; pixel RDLC; EDMX.
- IA que decida cupo o mora.
- Un rediseño “big bang” de 95 páginas en un sprint (sí: patrón + adopción al tocar).

## Métricas (sistema)

| Métrica | Meta 90 días |
|---------|----------------|
| Tiempo a primera tarea tras login | ≤ 2 clics en todos los roles P0 |
| Páginas P0 (login, inicio, consulta, simulador, aprobar, diario, cobro, bóveda, clientes, prendario nuevo) en estándar visual | 100% |
| Páginas tocadas por un PR sin estándar | 0 |
| Originación personal o prendaria | 1 canvas |
| Sede nueva “a medias” | 0 |
| Quejas “no se entiende” en P0 | 0 hints eternos |

## Dependencias

- [CONSTITUTION-UX.md](../CONSTITUTION-UX.md)
- [modulos.md](modulos.md) · [research.md](research.md)
- SSD-00…12 as-built (el **qué** funcional no se reabre aquí)
- PLAN-MULTISUCURSAL (bóveda / sede)
