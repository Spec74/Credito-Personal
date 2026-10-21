# Plan técnico — 001-experiencia-operativa (sistema)

**Spec Kit:** `/speckit.plan` · 2026-10-20  
**Spec:** [spec.md](spec.md) · **Módulos:** [modulos.md](modulos.md) · **Research:** [research.md](research.md)

## Enfoque

Alcance = **todo Credix Modern**. No hay greenfield. Se endurece el design system y se reordena la IA de **todos** los SSD. Importes = `usp_*`. Adopción visual **al tocar** cada página (no un rewrite de 95 archivos).

```
Oleada A — Sistema P0 (todas las rutas críticas)
  tokens + chrome sucursal + RoleHome
  estándar visual en: login, inicio, clientes, consulta, simulador,
  aprobar, diario, cobro, bóveda, prendario nuevo
  WorkCanvas crédito personal Y prendario
Oleada B — Red de sucursales + informes de decisión
  wizard sede, B-05, tablero red, B-02
  hub informes → búsqueda; KPI en Hoy
  ventas/almacén: mismo estándar si hay menú
Oleada C — Completar producto (contrato datos primero)
  prenda post-desembolso; cliente cruzado; PLA umbral
Oleada D — P0 restante
  tope/bloqueo inline en simulador; ruido P0; tareas en Hoy
```

## Stack (sin cambios de plataforma)

- SPA: React + Ant Design + tokens `--credix-*`
- API: .NET 10, JWT oficina/usuario
- Datos: mismos `usp_*` / tablas; **nuevas** solo en C si gerencia aprueba renovación/remate/hoja resumen
- Tests: Vitest (patrones UI) + tests de API existentes + smoke por oficina ≠ 1

## Arquitectura UI

```
AppShell
  ContextBar (oficina, caja, usuario) + GlobalSearch
  RoleHome ("Hoy")
  Cualquier ruta existente adopta un patrón:
    WorkCanvas  simulador, prendario/nuevo, venta rápida, entrada almacén
    Queue       aprobar, verificar-pagos, cobro campo, condonaciones
    Record      consulta crédito, ficha cliente, gestión prendaria, kardex ítem
    Page        listados, maestros, informes de archivo
    MoneyConfirm  desembolso, transferir bóveda, cierre, condonar
```

`EXACT_MENU_ROUTES` se mantiene. RoleHome **compone** deep-links a rutas que el menú ya autoriza. No ampliar permisos vía hub padre (regla SSD-02).

## Componentes a añadir (mínimos)

| Pieza | Dónde | Responsabilidad |
|-------|-------|-----------------|
| `credix-density` class en `body` | shell | compact / standard / comfortable |
| `CredixContextBar` | layout | sucursal + caja; selector si multi-oficina |
| `CredixWorkCanvas` | credix/ | 3 columnas / stack en móvil |
| `CredixQueue` | credix/ | lista + detalle |
| `CredixMoneyConfirm` | credix/ | importe + consecuencias |
| `RoleHomePage` | pages/ | widgets por rol (datos ya expuestos) |

Reutilizar: `CredixPage`, `CredixDataTable`, `CredixFilterBar`, `formRules`, `prendas.ts`.

## Datos / API

### Oleada A–B (sin tablas nuevas)

- Reusar: `GET /me`, menú, `prendario/resumen`, `prendario/creditos`, crear-solicitud, gastos-adm, bóveda-estado, saldos, creditos-por-aprobar, cobro diario.
- B-05: vista que orquesta `transferir-boveda` N veces (central). Si el endpoint actual pide ID bóveda, la UI resuelve **oficina → bóveda abierta** (hoy el operador pega el ID: cuello).
- Wizard sucursal: secuencia de APIs ya existentes (oficinas, usuarios/roles por sede, cajas, transferir). Un `POST /api/v1/admin/sucursal/provisionar` es **opcional** (P3) si el wizard en cliente es frágil; preferir orquestación SPA + transacciones por paso (más seguro si un paso falla).

### Oleada C (contrato primero)

Hecho: [contrato-datos-c.md](contrato-datos-c.md) + bitácora 2026-10-20. UI solo con dato existente.

Documentar en bitácora + schema **antes** de UI:

| Necesidad | ¿Tabla/USP? | Nota |
|-----------|-------------|------|
| LTV tope % | Parámetro producto u oficina | No hardcode; UI solo muestra |
| Renovación | `usp` o flujo CrearCredito existente | No recalcular interés en C# |
| Devolución bien | `Prenda.Estado` | DEVUELTO + fecha + usuario |
| Remate cola | Estados sobre `FechaRemate` / bitácora | WhatsApp ya avisa 3d; falta pipeline |
| Hoja resumen | PDF Credix + campos TEA/TCEA de `usp` | Transparencia |
| Idempotencia WhatsApp | Tabla o columna (se retiró `FechaNotifWhatsapp3d`) | Solo si negocio lo exige otra vez |

## Seguridad y oficina

- Toda query operativa: `vendix:oficina_id`.
- Tablero red: solo usuarios con varias oficinas o `IndPrincipal` + rol admin; cada card linkea con cambio de contexto explícito.
- No mostrar cartera de otra sede “por curiosidad”.
- Confirmaciones MoneyConfirm registran el mismo usuario JWT que hoy.

## Testing

- Unit: `totalTasacion`, LTV display, density class, RoleHome widgets vacíos.
- API: oficina ≠ 1 en cierre y listado prendario (ya hay filtro JWT).
- Browser (regla del repo): originación, cobro, wizard sucursal, tablero — no un screenshot.
- Accesibilidad: axe en login + WorkCanvas + cobro (P0).

## No hacer en este plan

- Nuevo framework CSS, Storybook obligatorio, app nativa.
- Reescribir informes SSD-07 (solo “6 KPI” en RoleHome gerencia).
- Tocar PrendarioLegacy salvo paridad de validación ya hecha.

## Criterio de converge

`/speckit.converge` pasa cuando:

1. RoleHome por rol + chrome de sucursal.
2. P0 de **todos** los módulos críticos en estándar visual.
3. WorkCanvas en crédito personal **y** prendario.
4. Hilo de cobro (Hoy → diario/campo/verificar).
5. Wizard sede + fondeo por oficina.
6. Tokens de densidad en el shell.
7. Smoke B-02 o dueño explícito.
8. SSD-13 §7 con evidencia.
