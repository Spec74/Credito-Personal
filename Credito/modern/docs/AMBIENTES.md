# Ambientes — Credito.Modern

Índice: [DOCUMENTACION.md](DOCUMENTACION.md) · Arquitectura: [ARCHITECTURE.md](ARCHITECTURE.md) §7.

| Ambiente | URL típica | API | SPA | Auth / menú | Swagger | Notas |
|----------|------------|-----|-----|-------------|---------|-------|
| **Development** | API `https://localhost:7288` · SPA Vite `5173` | `dotnet run` | `npm run dev` | `AllowDevToken` posible; menú query off por defecto | Sí | User-secrets / `appsettings.Development.json` |
| **Staging strangler (Docker)** | Proxy `http://localhost:9080` | Contenedor `:5080` detrás del proxy | `/app/` en nginx | `AllowDevToken=false`; `MigracionClavePerezosa=true` | No (Staging) | `deploy/docker-compose.strangler.yml` |
| **PreProduction** | Según IIS/App Service | Production-like | `/app/` | Igual prod + checklist SSD-00 | No | Secretos de entorno; SQL scripts `deploy/sql/` |
| **Piloto Azure B1 (2026-11)** | SPA Vercel `credito-personal.vercel.app` (+ Cloudflare Pages en prep) · API `api-credito-ocr-….azurewebsites.net` | Contenedor GHCR + Tesseract | `/` | `AllowDevToken=false`; `RequerirClienteAcceso=false` en piloto | No | [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md) · [CLOUDFLARE-PAGES.md](CLOUDFLARE-PAGES.md) |
| **Production** | Dominio oficial | App Service contenedor / IIS | `/` o `/app/` | `AllowDevToken=false`, `Menu:PermiteParametrosQuery=false`, `Auth:RequerirClienteAcceso=true`, rate limit | No | JWT ≥ 32; `ApiPeru__Token`; KnownProxies reales |

## Variables clave

| Clave | Dev | Staging Docker | Prod / piloto Azure |
|-------|-----|----------------|---------------------|
| `Hosting:AllowDevToken` | true (opcional) | false | false |
| `Menu:PermiteParametrosQuery` | false recomendado | false | false |
| `Auth:MigracionClavePerezosa` | según local | true | true en piloto / false prod salvo ventana |
| `Auth:RequerirClienteAcceso` | según | true en contratos | false piloto / true prod |
| `ApiPeru:Token` | user-secrets | env | App Setting `ApiPeru__Token` |
| `VITE_API_BASE_URL` | proxy o API | `/api/v1` relativo | URL absoluta API Azure `/api/v1` |
| `VITE_BASE_URL` | `/` en Vite | `/app/` | `/` en Vercel / Cloudflare Pages |
| `VITE_LEGACY_ORIGIN` | opcional RDLC | opcional | solo si negocio exige RDLC |
| `WEBSITES_PORT` | — | — | `8080` (contenedor) |

Secretos: **nunca** `deploy/.env` en Git. Usar ejemplos `deploy/.env*.example` y user-secrets / App Settings.

Verificación: `deploy/scripts/verify-production-config.ps1`.
