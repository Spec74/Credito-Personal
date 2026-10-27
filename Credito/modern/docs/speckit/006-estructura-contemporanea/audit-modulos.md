# Auditoría de estructura — 006

**Fecha:** 2026-10-26 · Cómo se **propaga** la plantilla moderna (no 95 PRs).

## Cómo se hereda

| Capa | Quién la usa | ¿Hereda 006? |
|------|----------------|--------------|
| `AppShell` (banner, nav, sesión, caja) | Todas las rutas autenticadas | Sí — un solo host |
| `CredixPage` (`article` + `header` de página + toolbar) | crédito, caja, tesorería, informes, intranet, hubs, RoleHome, ventas, almacén | Sí |
| `CredixCrudPage` | usuarios, roles, oficinas, maestros, clientes | Sí (envuelve Page) |
| `CredixInformePage` | ~28 informes | Sí |
| `CredixWorkCanvas` | simulador, venta rápida, entrada almacén | Sí (hijo del body) |
| `CredixListToolbar` | clientes, caja, **orden de venta** | Sí |
| **Fuera de Page** | `LoginPage` (`main`), visor RDLC (`100vh` a propósito) | Login sí; visor no se toca |

## Leftovers (qué se hizo / por qué no)

| Superficie | Hueco | Qué se hizo |
|------------|--------|-------------|
| Chrome móvil | `Usuario · Oficina` truncado | Sesión = control + panel; caja = `Caja` / `Sin caja` |
| `CredixPage` | `div` plano (molde 2018) | `article` + `header` de página + slot `toolbar` — **todas** las Page heredan |
| `/ventas/ordenes` | Toolbar MVC (`Space` + search 260 px + primaria dentro del panel) | `CredixListToolbar` + **Nueva orden** en acciones del H1 |
| Login | `div` wrapper | `main.credix-login-wrap` |
| Visor RDLC | `100vh` | Intencional (iframe). No tocar |
| Host `100vh` | `.credix-app` en design-system | `100dvh` en origen + candado |
| Título con aire doble | `page-head` margen 16 + padding 16 + gap 12 | origen `margin 0` / `padding 10`; candado |
| `/inicio` | `dash-analista` 28rem + head 9.5rem | reserva 0 (también escritorio) |
| Caja diario | KPI `margin-bottom: 12` extra | margin 0 |
| Hubs | cards `min-height: 148px` | 0 |
| Avisos vacío | `min-height: 240px` | 0 |
| Consulta evidencias | card `min-height: 126px` | 0 |
| `app-shell-content` | margen 12+8 (llega *después* del shell) | 0 en origen |
| Otras 90 Page | Ya iban por Page/Crud/Informe | Heredan landmarks y toolbar slot sin reescribirlas |
| Tarjetas móviles | `pagination={false}` o `pageSize: 20` volcaba 100 filas | tope 8 + «N datos más» |
| Inicio KPI | 1 columna ≤720 + `.dash-kpis-4` 4 col | 2 columnas desde 1100 |
| Hubs cards | cromo WinForms (gradiente #c0c8d0) | tokens `--credix-*` |
| Agenda | `page-head` margen 10 extra | 0 |
| 403 | `Result` suelto (aire Ant) | `CredixPage` + Result compacto |
| Título `flex: 1 1 200px` | basis 200 en columna = alto | `1 1 auto` + candado |

## Qué no es leftover

- `Space wrap` en un formulario (agregar serie, canje) no es chrome.
- Hubs de módulo (`CredixModuleHubPage`) ya son 1 columna en móvil.
- Reescribir 95 TSX. El patrón **es** la propagación.
