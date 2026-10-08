# Deploy al subir servidores

Spec SSD: [SSD-00-cutover.md](../ssd/SSD-00-cutover.md).

## Pre-requisitos

- Publicar desde `D:\Ebers\GitHub\Credito\modern`.
- Definir `CreditoDatabase__ConnectionString`, `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience` y `Jwt__RefreshAudience`.
- Configurar `BrowserCors:AllowedOrigins` para el origen real de la SPA.
- Configurar `VITE_API_BASE_URL` y `VITE_LEGACY_ORIGIN` antes del build SPA.

## Piloto Azure + Vercel (gerente)

Módulos SSD-01…09 pulidos para piloto (auth, crédito, caja, bóveda, clientes, prendario, informes, admin, maestros).

| Pieza | Acción |
|-------|--------|
| Azure SQL `CREDITO` | Reanudar si está *Paused*; importar `.bacpac` desde SQL Server local |
| App Service `crediconfiable-api` | Connection string + `Jwt__SigningKey` (≥32) |
| CORS | `BrowserCors__AllowedOrigins__0=https://credito-personal.vercel.app` (y dominio custom si hay) |
| API URL (Vercel) | Dominio real App Service, p.ej. `https://crediconfiable-api-….azurewebsites.net/api/v1` (no el nombre corto) |
| Acceso IP (piloto) | `Auth__RequerirClienteAcceso=false` **o** IPs del gerente en `MAESTRO.Acceso` |
| Forwarded headers | Recomendado en App Service: `Hosting__ForwardedHeaders__Enabled=true` |
| Vercel | Root `Credito/modern/Credito.Modern.Web`; env de `.env.vercel.example`; `vercel.json` ya hace SPA fallback |
| BD | Confirmar `ClaveUsuario nvarchar(256)` antes de confiar en migración perezosa |

### Exportar / importar `.bacpac`

1. En SSMS local: clic derecho en `CREDITO` → **Tasks** → **Export Data-tier Application** → `.bacpac`.
2. En Azure Portal: SQL Database `CREDITO` → **Import** (o crear DB desde bacpac en el server `sql-credito-crediconfiable`).
3. Aplicar scripts pendientes de `deploy/sql/` si el bacpac / bak no los trae (ver tabla abajo).
4. Probar conexión desde App Service (firewall Azure SQL: Allow Azure services + IP del equipo si usas SSMS).

### Azure con bak `CREDITO20260922` (piloto 2026-10)

| Orden | Script | ¿Obligatorio? |
|-------|--------|----------------|
| 1 | Restaurar bak en Azure | Sí |
| 2 | `2026-09-23-prod-bak-deltas-minimos.sql` (geo + prendario + menú + `ClaveUsuario` 256) | Sí |
| 3 | Mora postergada (`CreditoMora.MovimientoCajaId` NULL + `usp_CreditoMora_*`) | Sí, si el bak aún tiene la columna NOT NULL / faltan SPs |
| 4 | `2026-09-03-usp-credito-ins-quoted-identifier.sql` | **Sí tras crear `IX_Credito_EsPrendario`**: no cambia el cuerpo del SP; solo `QUOTED_IDENTIFIER ON` (evita error 1934 al generar crédito) |
| 5 | `2026-10-07-usp-morosidad-empresa.sql` | Solo si falta el SP (en el bak 2026-09-22 **ya venía**) |
| 6 | `2026-10-06-prendario-drop-fecha-notif-whatsapp.sql` | Solo si existe la columna `FechaNotifWhatsapp3d` (los deltas mínimos **no** la crean) |

**No** reaplicar el cuerpo completo de `usp_Credito_Ins` “a mano” salvo que negocio pida un cambio de reglas: el script del paso 4 recrea el módulo con las opciones SET correctas y deja el texto igual.

**No** ejecutar `2026-09-23-azure-cutover-modern-deltas.sql` sobre este bak: es redundante.

Smoke mínimo: login → menú → tablero `/inicio` → caja diario → un informe PDF → (ACL) `/informes/morosos`.

## Build

```powershell
cd D:\Ebers\GitHub\Credito\modern
dotnet restore Credito.Modern.sln
dotnet build Credito.Modern.sln
dotnet test Credito.Modern.Tests\Credito.Modern.Tests.csproj

cd Credito.Modern.Web
npm install
npm run lint
npm run build
```

## Validaciones smoke

- Login con usuario gestor, cajero, encargado y gerente.
- Menú desde `MAESTRO.usp_MenuLst`.
- Crédito: consulta, simulador, aprobación y reportes.
- Caja: diario, saldos, cierre, caja chica y verificar pagos.
- Bóveda: estado, movimientos, transferencia y cierre.
- Informes: PDF/CSV modernos y fallback RDLC donde aplique.
- Proxy: `/api/v1/*` hacia API moderna y rutas MVC restantes hacia legacy.

## Rollback

Mantener el MVC legacy activo detrás del proxy. Si una rebanada moderna falla, redirigir temporalmente esa ruta al MVC y conservar logs de correlación para diagnóstico.
