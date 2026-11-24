# Cloudflare Pages — SPA Credito.Modern.Web

**Fecha:** 2026-11-24 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md) · Piloto Azure: [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md)

Despliegue del frontend en **Cloudflare Pages** (alternativa / sustituto de Vercel). La API sigue en Azure App Service contenedor.

---

## 1. Qué hay en el repo

| Archivo | Uso |
|---------|-----|
| `Credito.Modern.Web/public/_redirects` | SPA fallback (`/* → /index.html`) en Pages |
| `Credito.Modern.Web/wrangler.toml` | Nombre proyecto + `dist` |
| `Credito.Modern.Web/.env.cloudflare.example` | Variables `VITE_*` de build |

Build local:

```powershell
cd Credito/modern/Credito.Modern.Web
$env:VITE_BASE_URL="/"
$env:VITE_API_BASE_URL="https://api-credito-ocr-eug7aeh6dxcxcyh6.westcentralus-01.azurewebsites.net/api/v1"
npm run build
```

Salida: `dist/`.

---

## 2. Crear proyecto en Cloudflare (Git)

1. [Cloudflare Dashboard](https://dash.cloudflare.com) → **Workers & Pages** → **Create** → **Pages** → **Connect to Git**.
2. Repo: `Spec74/Credito-Personal` (o el remoto que usen).
3. Configuración de build:

| Campo | Valor |
|--------|--------|
| **Root directory** | `Credito/modern/Credito.Modern.Web` |
| **Framework preset** | Vite (o None) |
| **Build command** | `npm run build` |
| **Build output directory** | `dist` |
| **Node version** | `22` (o `20`) — Environment variable `NODE_VERSION=22` |

4. **Environment variables** (Production y Preview):

```text
VITE_BASE_URL=/
VITE_API_BASE_URL=https://api-credito-ocr-eug7aeh6dxcxcyh6.westcentralus-01.azurewebsites.net/api/v1
VITE_GOOGLE_MAPS_API_KEY=<misma clave Maps>
NODE_VERSION=22
```

5. **Save and Deploy**. Anota la URL: `https://<proyecto>.pages.dev`.

---

## 3. CORS en Azure (obligatorio)

En App Service `api-credito-ocr` → Variables de entorno, agrega el origen Cloudflare:

```text
BrowserCors__AllowedOrigins__0=https://credito-personal.vercel.app
BrowserCors__AllowedOrigins__1=https://<proyecto>.pages.dev
```

Si usas dominio custom en Cloudflare (p.ej. `app.tudominio.com`):

```text
BrowserCors__AllowedOrigins__2=https://app.tudominio.com
```

**Aplicar** → reiniciar API.

Sin esto, login/oficinas fallan por CORS aunque el build esté bien.

---

## 4. Google Maps

En Google Cloud → credenciales de la API key → HTTP referrers:

- `https://<proyecto>.pages.dev/*`
- `https://*.pages.dev/*` (si usan previews)
- dominio custom `https://app.tudominio.com/*`

---

## 5. Deploy manual con Wrangler (opcional)

```powershell
cd Credito/modern/Credito.Modern.Web
npm run build
npx wrangler pages deploy dist --project-name=credito-personal
```

Requiere `npx wrangler login` una vez.

---

## 6. Checklist post-deploy

- [ ] Abre `https://<proyecto>.pages.dev/login`
- [ ] Cargan oficinas (CORS OK)
- [ ] Login con usuario semilla / piloto
- [ ] Simulador / ApiPerú (token en Azure)
- [ ] Verificar pagos Plin OCR (API contenedor)

---

## 7. Vercel vs Cloudflare

Se puede mantener Vercel en paralelo mientras migran. Cuando Cloudflare sea canónico:

1. Actualizar CORS Azure (quitar o dejar Vercel).
2. Actualizar referrers Maps.
3. Apuntar dominio/DNS a Pages.
4. Documentar URL canónica en [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md).
