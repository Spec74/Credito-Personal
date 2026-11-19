# Credito.Modern

**Documentación (paquete as-built 100% escrito):** [docs/DOCUMENTACION.md](docs/DOCUMENTACION.md)  
**Cómo leer el código:** [docs/COMO-LEER-EL-CODIGO.md](docs/COMO-LEER-EL-CODIGO.md) · **Estructura:** [docs/ESTRUCTURA.md](docs/ESTRUCTURA.md) · **Arquitectura:** [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)  
**API (visión):** [docs/API-OVERVIEW.md](docs/API-OVERVIEW.md) · **Inventario largo de rutas:** [docs/API-ENDPOINTS-INVENTARIO.md](docs/API-ENDPOINTS-INVENTARIO.md)  
**Dashboard:** [docs/dashboard_migracion.md](docs/dashboard_migracion.md) · **SSD:** [docs/ssd/README.md](docs/ssd/README.md) · **Ops:** [docs/OPS.md](docs/OPS.md) · **Deuda técnica restante:** [docs/DEUDA-TECNICA.md](docs/DEUDA-TECNICA.md)

**Migración strangler:** candidata a go-live. Al subir: [docs/migration/DEPLOY-AL-SUBIR.md](docs/migration/DEPLOY-AL-SUBIR.md).

Arquitectura: **Domain → Application → Infrastructure → Api** + **Credito.Modern.Web** (SPA). Validación: [docs/ssd/SSD-12-validacion.md](docs/ssd/SSD-12-validacion.md).

API **.NET 10** (
et10.0). Para alinear a **.NET 8 LTS**, retargetea los .csproj e instala el hosting bundle 8.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (o 8 si retargeteas)
- Node.js 20+ para la SPA

## Build SPA

```powershell
cd Credito\modern
.\deploy\scripts\build-spa.ps1
# o: npm run build
```

## Ejecutar en local

```powershell
cd Credito\modern
dotnet restore Credito.Modern.sln
dotnet run --project Credito.Modern.Api
```

- Swagger (Development): `https://localhost:7288/swagger`
- Health: `GET /health`
- Login: `POST /api/v1/auth/login`
- Prefijos por dominio: [docs/API-OVERVIEW.md](docs/API-OVERVIEW.md)
- Inventario endpoint a endpoint: [docs/API-ENDPOINTS-INVENTARIO.md](docs/API-ENDPOINTS-INVENTARIO.md)

## Cómo correr (dos terminales)

**1 — API**

```powershell
cd D:\Ebers\GitHub\Credito\modern
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project Credito.Modern.Api --launch-profile Credito.Modern.Api
```

**2 — Front**

```powershell
cd D:\Ebers\GitHub\Credito\modern\Credito.Modern.Web
npm run dev
```

Abre `http://localhost:5173/login`. API en `https://localhost:7288`.

## Operaciones

- Desplegar: [DEPLOY-AL-SUBIR.md](docs/migration/DEPLOY-AL-SUBIR.md)
- Strangler Docker `:9080`: `deploy/docker-compose.strangler.yml`
- PDF: QuestPDF en la API
