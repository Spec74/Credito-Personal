# SSD-02 — Crédito (operaciones)

**Estado:** as-built · 2026-09-11  
**Código:** `/credito/*` · `/api/v1/credito/*`  
**Fuente legacy:** `CreditoController`, `CreditoAprobarController`, `TareasController`, `Condonacion`, vistas `Creditos.cshtml` / `CreditoAprobar/Index` / `Tareas/Index`  
**Doc de ingeniería:** [credito_migracion.md](../credito_migracion.md)

## 1. Propósito y actores

Ciclo de vida del crédito personal (no prendario): consultar cartera, simular y solicitar, aprobar, gestionar el expediente e impresos.

| Rol | Uso |
|-----|-----|
| Gestor / cajero | Consulta, solicitud, seguimiento, enlace a cobro en caja |
| Analista | Misma operación de cartera (el prendario es SSD-06) |
| Aprobador 1 | Bandeja 1.ª / 2.ª; sin otros roles operativos solo ve `/credito/aprobar` |
| Administrador | Reprogramar, condonar, parámetros del simulador, bandeja de solicitudes de condonación |
| Lectura | Consulta e impresos; sin alta ni gestión |

## 2. Alcance

**Entra**

- Hub `/credito`, consulta unificada, simulador, parámetros, bandeja aprobar, tareas, gestión (cargos, aval, tope, evidencias, condonar en ficha), solicitudes de condonación
- Impresos del crédito: plan, estado de cuenta, movimientos, ficha cliente (PDF tabular Credix)

**No entra**

- Caja diario / cobro / desembolso → [SSD-03](SSD-03-caja.md)
- Bóveda → SSD-04
- Alta y ficha de persona → SSD-05
- Cartera prendaria y WhatsApp → SSD-06
- Índices Reportes → Crédito / cobranza → SSD-07
- Layout idéntico ReportViewer (no hay motor RDLC en la API)

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Credito/Creditos` | `/credito/consulta` | Query `personaId` / `creditoId` |
| `/Credito/Simulador` | `/credito/simulador` | Alta solicitud + generar crédito |
| `/Credito/ParametrosSimulador` | `/credito/parametros-simulador` | Exact menu |
| `/CreditoAprobar` | `/credito/aprobar` | Exact menu; 1.ª/2.ª y rechazo en bandeja |
| `/Tareas/Index` | `/credito/tareas` | Ítem de menú directo, no hub |
| `/Condonacion/Index` | `/credito/condonaciones` | Pedido nace en caja (SSD-03) |
| Impresos RDLC plan/estado/mov. | `CreditoConsultaImpresosBar` | Datos `usp_*`; layout Credix |

Menú vivo oficina 1 (`usp_MenuLst`): CREDITOS `Credito/Creditos`, SIMULADOR `Credito/Simulador`, APROBACION `CreditoAprobar`, TAREAS `Tareas`, CONDONACION `Condonacion`. El padre CREDITO no otorga el hub (evitaría ampliar permisos).

## 4. Contrato de datos

El C# no calcula cuota, mora ni condonación. Invoca procedimientos y tablas versionadas.

| Procedimiento / tabla | Uso |
|-----------------------|-----|
| `usp_SimuladorCredito`, `usp_CalcularTEM` | Plan tentativo |
| `usp_Credito_Ins` / `_Upd` | Alta / actualización (índice prendario: bitácora 2026-09-09) |
| `usp_EstadoPlanPago`, `usp_CuotasPendientes` | Consulta de plan |
| `usp_RptMovimientoCredito`, `usp_Rpt*` plan/estado/cliente | Impresos |
| Aprobar / rechazar / anular / prorrogar / reprogramar | Mismos `usp_*` que `CreditoBL` |
| `usp_CreditoMora_Registrar` / `_Liquidar` | Mora postergada (consulta + caja) |
| `usp_SolicitarCondonacion`, `CREDITO.CreditoCondonacion` | Pedido de mora; no recalcula `TotalPago` |
| `condonar-credito` | Aprobación en ficha (admin) |

## 5. API y SPA

Prefijo `/api/v1/credito`. JWT con `vendix:oficina_id` / `vendix:usuario_id`.

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET creditos-grilla-persona`, `GET persona-credito-ficha`, `GET credito-contexto` | `CreditoUser` | `/credito/consulta` |
| `GET estado-plan-pago`, `GET rpt-movimiento-credito*` | `CreditoUser` | Consulta + impresos |
| `GET/POST rpt-plan-pagos-pdf`, `rpt-estado-credito-pdf`, `rpt-cliente-pdf` | `CreditoUser` | Impresos (pestaña nueva) |
| `POST simulador-credito`, `POST crear-solicitud-credito`, `POST crear-credito` | `CreditoUser` | `/credito/simulador` |
| `GET/POST parametros-simulador` | User / `CreditoRolAdministrador` | `/credito/parametros-simulador` |
| `GET creditos-por-aprobar`, `POST aprobar-credito`, `POST rechazar-credito` | Aprobador1 o admin | `/credito/aprobar` |
| `GET/POST tareas/*` | `CreditoUser` (alta restringida) | `/credito/tareas` |
| `POST condonar-credito` | `CreditoRolSoloAdministrador` | Gestión en consulta |
| `GET condonaciones-pendientes`, `POST solicitar-condonacion` | `CreditoRolOperador` | `/credito/condonaciones` + caja |
| `POST cambiar-analista`, `POST actualizar-tope`, anular, prorrogar | `CreditoRolAprobador1OAdministrador` | Gestión |

## 6. Seguridad

- Oficina del JWT en listados de condonación y operaciones de oficina.
- `EXACT_MENU_ROUTES`: `/credito/aprobar`, `/credito/parametros-simulador` (el hub `/credito` no las abre).
- APROBADOR 1 solo bandeja: `esCreditoPerfilSoloBandeja` + `CreditoOperacionRoute`.
- Rol LECTURA: UI sin gestión; API `CreditoRolOperador` en escrituras.
- Padres de menú (CREDITO) no se mapean a `/credito` para no conceder `/credito/*`.

Archivos: `creditoOperacionPermisos.ts`, `creditoHubFilter.ts`, `CreditoAuthorizationPolicies.cs`, `menuRouteAccess.ts`.

## 7. Criterios de aceptación

- [ ] Gestor con ítem CREDITOS abre `/credito/consulta`, busca persona y ve plan del crédito elegido.
- [ ] Simular con `monto <= 0` no llama al SP (lista vacía, paridad MVC).
- [ ] APROBADOR 1 sin gestor/admin no entra a consulta ni simulador por menú; sí a `/credito/aprobar`.
- [ ] Aprobar 1.ª / 2.ª y rechazar llaman los mismos `usp_*` que el MVC.
- [ ] Impresos abren PDF JWT (no MVC RDLC) con plan / estado / movimientos.
- [ ] Admin condona en ficha; la solicitud previa nace en caja y aparece en `/credito/condonaciones` acotada a la oficina.
- [ ] LECTURA no crea solicitud ni gestiona.
- [ ] Sin menú CREDITOS, `hasMenuRouteAccess('/credito/consulta')` es falso si solo existe el padre CREDITO.

## 8. Desviaciones

Relato en [BITACORA-DESVIACIONES.md](../migration/BITACORA-DESVIACIONES.md):

- 2026-09-09 — `usp_Credito_Ins` e índice filtrado prendario
- 2026-09-09 — mora postergada / `SCOPE_IDENTITY` en liquidación
- 2026-09-10 — solicitud de condonación (oficina JWT; el MVC listaba toda la compañía)

Aceptadas de producto (no bitácora financiera): doble clic en bandeja por crédito (no por persona); aprobar/rechazar en la grilla; PDF Credix ≠ píxel RDLC.

## 9. Pruebas y evidencia

- API: `simuladorcreditoendpointtests`, `creditogestionendpointtests`, `creditocicloendpointtests`, `creditosporaprobarendpointtests`, `creditocondonacionendpointtests`, `creditorolauthorizationendpointtests`, `rptestadocreditoendpointtests`, `rptmovimientocreditoendpointtests`
- SPA: `creditoOperacionPermisos.test.ts`, `menuRouteAccess.test.ts`, `resolvespapathfrommenuitem.test.ts` (CREDITOS, SIMULADOR, TAREAS, CONDONACION, APROBACION)
- Smoke: login gestor → consulta → simulador; login aprobador 1 → solo bandeja; PDF desde impresos

## 10. Go-live

Listo en Development. Cutover preprod: smoke por rol (gestor, aprobador 1, admin, lectura) según `DEPLOY-AL-SUBIR.md`.
