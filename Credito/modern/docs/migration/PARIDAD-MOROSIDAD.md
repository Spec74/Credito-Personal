# Paridad funcional: Morosos empresariales

Spec SSD: [SSD-07-informes.md](../ssd/SSD-07-informes.md) (sección Morosos).  
Fuente legacy: `MorosidadController` / módulo independiente bajo Cierre gerencial (`/Morosidad`).

## Identificación

No usa el menú `MAESTRO.Menu`. La ACL es de aplicación:

| Clave | Valor típico | Efecto |
|-------|--------------|--------|
| `Morosidad:UsuarioConsultaIds` | `[3, 10]` | Solo esos `UsuarioId` ven `/informes/morosos` y llaman la API |
| `Morosidad:PermitirAdministradores` | `false` (default) | Paridad MVC: admin **no** entra solo por rol |

En la SPA: `AppShell` consulta `GET /api/v1/morosidad/permisos` y, si `puedeConsultar`, añade la ruta a `extraAllowedPaths` (`EXACT_MENU_ROUTES` incluye `/informes/morosos`).

## Procedimiento

`CREDITO.usp_MorosidadEmpresa` (`deploy/sql/2026-10-07-usp-morosidad-empresa.sql`).

| Parámetro | Uso |
|-----------|-----|
| `@Tipo` | `TODOS`, `SIN_PAGO`, `NUNCA_PAGO`, `DEJO_PAGAR`, `PAGA_CON_ATRASO` |
| `@OficinaId` | Opcional; filtra cartera |
| `@UsuarioId` | Opcional; gestor (`UsuarioRegId` del crédito) |
| `@FechaCorte` | Opcional; default `dbo.ufnFecha()` |

Una fila por `PersonaId` con mora y saldo > 0. Si hay varias oficinas/gestores → `VARIAS OFICINAS` / `VARIOS`.

Reglas (mismas que el comentario del SP / dashboard gestor mora):

- Crédito `DES`, no irrecuperable, desembolsado antes del día siguiente al corte.
- Mora: cuota `PEN` con `FechaVencimiento < @Hoy`.
- Pago válido: `Operacion = CUO`, `Estado = 1`, `ImportePago > 0`, `FechaReg < mañana`.
- Clasificación: `NUNCA_PAGO` / `DEJO_PAGAR` / `PAGA_CON_ATRASO`.

## API y SPA

| Método | Ruta | Pantalla |
|--------|------|----------|
| `GET` | `/api/v1/morosidad/permisos` | Menú / hub |
| `GET` | `/api/v1/morosidad/empresa` | `/informes/morosos` |
| `GET` | `/api/v1/morosidad/excel` | Export ClosedXML (rango sobre primera cuota vencida) |

El resumen de tarjetas (clientes, créditos, saldo, conteos por código) lo arma el C# sobre el resultado del SP; no hay segundo SP.

## Despliegue Azure (BAK 2026-09-22)

En el bak de producción el procedimiento **ya viene**. No hace falta reaplicarlo salvo que falte o se quiera alinear el cuerpo con el script del repo.

## Qué no es

- Informe `rpt-morosidad-gestor` / cobro diario → SSD-07 catálogo RDLC, otra pantalla.
- Cierre gerencial mensual → módulo aparte (`CierreGerencial` + ACL propia).
