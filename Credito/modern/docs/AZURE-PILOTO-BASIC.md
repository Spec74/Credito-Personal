# Azure piloto básico (cuenta nueva 2026-11) — as-built

**Fecha:** 2026-11-23 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md) · Deploy: [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md)

Piloto de costo fijo bajo (créditos Azure ~$200). **No** confundir con el piloto anterior `crediconfiable-api` (cuenta vieja).

---

## 1. Recursos

| Recurso | Nombre / URL | SKU / nota |
|---------|----------------|------------|
| Resource group | `rg-credito-basic` | West Central US (SQL Central US) |
| SQL Server | `sql-credito-basic.database.windows.net` | — |
| SQL Database | `CREDITO` | **DTU Basic** (5 DTU, máx. 2 GB) |
| App Service Plan | `ASP-rgcreditobasic-9c0a` | **Basic B1** Linux |
| API (contenedor) | `api-credito-ocr` | Imagen GHCR con Tesseract |
| API hostname | `https://api-credito-ocr-eug7aeh6dxcxcyh6.westcentralus-01.azurewebsites.net` | Puerto contenedor **8080** |
| Imagen | `ghcr.io/spec74/credito-api:basic` | `Dockerfile` en `Credito/modern/` |
| SPA (actual) | `https://credito-personal.vercel.app` | Vercel; `VITE_API_BASE_URL` → API `/api/v1` |
| SPA (Cloudflare) | Pages `*.pages.dev` (en curso) | [CLOUDFLARE-PAGES.md](CLOUDFLARE-PAGES.md) |
| Presupuesto | `presupuesto-credito` | Alertas 40% / 100% de US$50 |

**App Service “código” (`api-credito-basic`)**: solo ZIP; **sin** Tesseract. Sustituido por contenedor para OCR Plin. Preferir borrar el de código cuando el contenedor esté estable.

---

## 2. Por qué contenedor (OCR)

`TesseractCliOcrEngine` ejecuta el binario `tesseract` del SO.  
El `Dockerfile` instala `tesseract-ocr` + `tesseract-ocr-spa`.  
Deploy ZIP / “Código” en App Service **no** trae Tesseract → mensaje *OCR no está disponible…*.

Workflow imagen: `.github/workflows/publish-api-container.yml` → `ghcr.io/spec74/credito-api:basic` (paquete público).  
Script local: `deploy/scripts/publish-api-container.ps1`.

---

## 3. App Settings mínimas (API contenedor)

| Variable | Rol |
|----------|-----|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `CreditoDatabase__ConnectionString` | Azure SQL `CREDITO` |
| `Jwt__SigningKey` | ≥ 32 bytes UTF-8 |
| `Jwt__Issuer` / `Jwt__Audience` / `Jwt__RefreshAudience` | `Credito.Modern` / … / `Credito.Modern.Refresh` |
| `BrowserCors__AllowedOrigins__0` | `https://credito-personal.vercel.app` |
| `BrowserCors__AllowedOrigins__1` | URL Cloudflare Pages (`https://….pages.dev`) al publicar |
| `Auth__RequerirClienteAcceso` | `false` en piloto |
| `Hosting__AllowDevToken` | `false` |
| `Hosting__ForwardedHeaders__Enabled` | `true` |
| `Hosting__DisableHttpsRedirection` | `true` (TLS en App Service) |
| `WEBSITES_PORT` | `8080` |
| `DOCKER_REGISTRY_SERVER_URL` | `https://ghcr.io` |
| `ApiPeru__Token` | Bearer token ApiPerú (**obligatorio** para DNI/RUC) |
| `ApiPeru__BaseUrl` | opcional; default `https://apiperu.dev/api/` |

Sin `ApiPeru__Token` la API responde: *Validación externa no configurada…* (no es “API vencida” por defecto).

Secretos: solo App Settings / Key Vault. **Nunca** en Git.

---

## 4. Base de datos

1. Local `localhost,14330` / `CREDITO` → scripts + wipe SEMILLA: `deploy/sql/2026-11-05-vaciar-semilla-producto.sql`.  
2. Export `.bacpac` → Import en Azure SQL Basic (caber en **2 GB**).  
3. Login semilla tras wipe: `ADMIN` / `Admin123!` (cambiar).  
4. Gate: `deploy/scripts/smoke-db-gate.ps1`.

---

## 5. Frontend (Vercel hoy · Cloudflare Pages en preparación)

### Vercel (canónico actual)

- Root: `Credito/modern/Credito.Modern.Web`  
- `vercel.json` → `build.env.VITE_API_BASE_URL` = hostname **contenedor** + `/api/v1`  
- Dashboard Env no debe dejar pegada la URL de `api-credito-basic` / `crediconfiable-api`  
- Tras cambiar `VITE_*`: **Redeploy** obligatorio  
- Ejemplo: [`.env.vercel.example`](../Credito.Modern.Web/.env.vercel.example)

### Cloudflare Pages

- Guía: [CLOUDFLARE-PAGES.md](CLOUDFLARE-PAGES.md)  
- Repo: `wrangler.toml`, `.env.cloudflare.example` (sin `_redirects`)  
- Build: Root `Credito/modern/Credito.Modern.Web` · `npm run build` · output `dist` · `NODE_VERSION=22`  
- Mismas `VITE_*` que Vercel; al tener URL Pages → agregar CORS en Azure + referrers Maps

---

## 6. Smoke mínimo (este piloto)

```text
GET /health          → Healthy
GET /health/ready    → Healthy + database Healthy
GET /api/v1/oficinas → OFICINA PRINCIPAL
SPA login            → ADMIN (o usuarios sembrados) + oficina
Simulador ApiPerú    → con ApiPeru__Token
Verificar pagos Plin → OCR (solo en contenedor)
```

---

## 7. Costos (orden de magnitud)

B1 + SQL Basic ≈ US$18–20/mes lista (región varía). Evitar Premium, SQL Serverless/vCore, App Insights agresivo, varios planes.
