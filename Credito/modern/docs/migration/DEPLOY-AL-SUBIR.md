# Deploy al subir servidores

## Pre-requisitos

- Publicar desde `D:\Ebers\GitHub\Credito\modern`.
- Definir `CreditoDatabase__ConnectionString`, `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience` y `Jwt__RefreshAudience`.
- Configurar `BrowserCors:AllowedOrigins` para el origen real de la SPA.
- Configurar `VITE_API_BASE_URL` y `VITE_LEGACY_ORIGIN` antes del build SPA.

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
