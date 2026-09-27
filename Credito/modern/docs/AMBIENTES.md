# Ambientes — Credito.Modern

Índice: [DOCUMENTACION.md](DOCUMENTACION.md) · Arquitectura: [ARCHITECTURE.md](ARCHITECTURE.md) §7.

| Ambiente | URL típica | API | SPA | Auth / menú | Swagger | Notas |
|----------|------------|-----|-----|-------------|---------|-------|
| **Development** | API `https://localhost:7288` · SPA Vite `5173` | `dotnet run` | `npm run dev` | `AllowDevToken` posible; menú query off por defecto | Sí | User-secrets / `appsettings.Development.json` |
| **Staging strangler (Docker)** | Proxy `http://localhost:9080` | Contenedor `:5080` detrás del proxy | `/app/` en nginx | `AllowDevToken=false`; `MigracionClavePerezosa=true` | No (Staging) | `deploy/docker-compose.strangler.yml` |
| **PreProduction** | Según IIS/App Service | Production-like | `/app/` | Igual prod + checklist SSD-00 | No | Secretos de entorno; SQL scripts `deploy/sql/` |
| **Production** | Dominio oficial | App Service / IIS | `/app/` | `AllowDevToken=false`, `Menu:PermiteParametrosQuery=false`, `Auth:RequerirClienteAcceso=true`, rate limit | No | JWT key ≥ 32 chars; KnownProxies reales |

## Variables clave

| Clave | Dev | Staging Docker | Prod |
|-------|-----|----------------|------|
| `Hosting:AllowDevToken` | true (opcional) | false | false |
| `Menu:PermiteParametrosQuery` | false recomendado | false | false |
| `Auth:MigracionClavePerezosa` | según local | true | false (salvo ventana acordada) |
| `Auth:RequerirClienteAcceso` | según | true en contratos | true |
| `VITE_API_BASE_URL` | proxy o API | `/api/v1` relativo al host | producción |
| `VITE_BASE_URL` | `/` en Vite | `/app/` | `/app/` |
| `VITE_LEGACY_ORIGIN` | opcional RDLC | opcional | solo si negocio exige RDLC |

Secretos: **nunca** `deploy/.env` en Git. Usar ejemplos `deploy/.env*.example` y user-secrets / App Settings.

Verificación: `deploy/scripts/verify-production-config.ps1`.
