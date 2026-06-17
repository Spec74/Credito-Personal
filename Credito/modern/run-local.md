# Ejecutar Credito Modern en local

El código fuente moderno de esta rama está en `D:\Ebers\GitHub\Credito\modern`. Esta carpeta contiene la API .NET 10, la SPA React/Vite, deploy y documentación de migración.

## API (.NET 10)

Desde la carpeta `Credito\modern`:

```powershell
cd D:\Ebers\GitHub\Credito\modern
dotnet restore Credito.Modern.sln
dotnet run --project Credito.Modern.Api
```

- Swagger (Development): **https://localhost:7288/swagger**
- Health: **https://localhost:7288/health**

Configure secretos fuera de Git. Ejemplo:

```powershell
dotnet user-secrets set "CreditoDatabase:ConnectionString" "Server=...;Database=CREDITO;User Id=...;Password=...;TrustServerCertificate=True" --project .\Credito.Modern.Api\Credito.Modern.Api.csproj
```

## Frontend (SPA)

Para desarrollo SPA:

```powershell
cd D:\Ebers\GitHub\Credito\modern\Credito.Modern.Web
npm install
npm run dev
```

- Abra: **http://localhost:5173/login** o **http://localhost:5173/app/login** si usa `VITE_BASE_URL=/app/`.
- El CSS responsive extra está en `dist/assets/credix-responsive-global.css`.

Para build de producción:

```powershell
cd D:\Ebers\GitHub\Credito\modern\Credito.Modern.Web
npm run build
```

## Probar responsive

DevTools → ancho ~375px → rutas: `/credito/aprobar`, `/caja/saldos`, `/informes/comprobantes-caja-chica`, `/reportes/venta`.

## Proxy strangler

Para ejecutar MVC + API moderna detrás del proxy local:

```powershell
cd D:\Ebers\GitHub\Credito\modern
Copy-Item deploy\.env.example deploy\.env
.\deploy\scripts\start-strangler.ps1 -Build
```

## Cierre de migración

Ver:

- `docs/migration/MIGRATION-CLOSURE.md`
- `docs/migration/DEPLOY-AL-SUBIR.md`
- `docs/ui_modernizacion_modulos.md`
