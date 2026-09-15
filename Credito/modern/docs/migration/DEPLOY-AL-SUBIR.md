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
| CORS | `BrowserCors__AllowedOrigins__0=https://<proyecto>.vercel.app` (y dominio custom si hay) |
| Acceso IP (piloto) | `Auth__RequerirClienteAcceso=false` **o** IPs del gerente en `MAESTRO.Acceso` |
| Forwarded headers | Recomendado en App Service: `Hosting__ForwardedHeaders__Enabled=true` |
| Vercel | Root `Credito/modern/Credito.Modern.Web`; env de `.env.vercel.example`; `vercel.json` ya hace SPA fallback |
| BD | Confirmar `ClaveUsuario nvarchar(256)` antes de confiar en migración perezosa |

### Exportar / importar `.bacpac`

1. En SSMS local: clic derecho en `CREDITO` → **Tasks** → **Export Data-tier Application** → `.bacpac`.
2. En Azure Portal: SQL Database `CREDITO` → **Import** (o crear DB desde bacpac en el server `sql-credito-crediconfiable`).
3. Aplicar scripts pendientes de `deploy/sql/` si el bacpac no los trae (mora, `ClaveUsuario` 256, etc.).
4. Probar conexión desde App Service (firewall Azure SQL: Allow Azure services + IP del equipo si usas SSMS).

Smoke mínimo: login → menú → tablero `/inicio` → caja diario → un informe PDF.

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
