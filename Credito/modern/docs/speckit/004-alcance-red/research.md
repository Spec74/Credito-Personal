# Research — 004-alcance-red

**Fecha:** 2026-10-24

## Matriz (todos los SSD)

| SSD | Superficie | ¿Hueco? | Decisión |
|-----|------------|---------|----------|
| 01 Auth | Login elige oficina | No | JWT nace ahí |
| 01 Inicio admin | Tablero | Sí: solo JWT | `sedeId` si Central |
| 01 Inicio analista | KPIs del gestor | No | Ya filtra `OficinaId` + usuario |
| 02 Crédito | Consulta, simular, aprobar, tareas | No (escritura) | `ValidateJwtOficina` |
| 03 Caja diario / cobro / saldos | Operación | No | JWT |
| 03 Maestro cajas | Listado + alta | **Sí** | Filtro + write acotado |
| 04 Bóveda / fondeo / red | Tesorería | No | Red ya es Central; entrar sede = re-login |
| 05 Clientes | Listado | No (producto) | Persona de red; crédito por oficina |
| 06 Prendario | Altas | No | JWT |
| 07 Informes JWT oculto | ~28 pantallas | Diferido | Central re-entra o usa cierre/morosos |
| 07 Cobranza / cobro diario | Filtro oficina | **Sí (fuga)** | Resolver + UI Central |
| 07 Cierre / morosos | Ya 2026-10-24 | Hecho | — |
| 08 Usuarios | Ya 2026-10-24 | Hecho | — |
| 08 Oficinas / roles | Catálogo | No | Alta de sede = Central (wizard) |
| 09 Maestros | Global | No | — |
| 10–11 Ventas / almacén | JWT en writes | No ahora | Menú no vivo oficina 1 |
| 12 Validación | N/A | — | — |
| 14 Intranet | Bandeja JWT; avisos RED | No | Encargado no edita RED |

## Causa de la fuga

`CobroDiarioReportAccess` trataba ADMIN/APROBADOR/REPORTEPARCIAL como “elige cualquier oficina”. Ese rol existe **también en sucursal**.

## Alternativas descartadas

- Poner `IndPrincipal` en el JWT: cambia el contrato de login; no hace falta.
- Relajar `ValidateJwtOficina` en todos los GET: Central podría escribir caja ajena si un POST reusa el guard.
- Feature por cada informe: 40 PRs. El patrón vive en el resolver + caja + tablero.
