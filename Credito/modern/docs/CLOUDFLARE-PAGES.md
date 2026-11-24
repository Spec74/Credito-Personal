# Cloudflare Pages — SPA Credito.Modern.Web

**Fecha:** 2026-11-24 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md) · Piloto Azure: [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md)

Despliegue del frontend en **Cloudflare Pages** (alternativa / sustituto de Vercel). La API sigue en Azure App Service contenedor.

---

## 1. Qué hay en el repo

| Archivo | Uso |
|---------|-----|
| `Credito.Modern.Web/public/_redirects` | SPA fallback (`/* → /index.html`) en Pages |
| `Credito.Modern.Web/wrangler.toml` | Workers + Assets (`dist`, SPA `not_found_handling`) |
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

## 2. Crear proyecto en Cloudflare (Git — Workers + Assets)

El dashboard actual usa **Workers** con comando de deploy `npx wrangler deploy` (no Pages clásico). El `wrangler.toml` del repo ya declara `[assets]` → `./dist` + SPA.

1. Workers & Pages → **Create** → **Continue with GitHub** → `Spec74/Credito-Personal`.
2. Configuración:

| Campo | Valor |
|--------|--------|
| **Nombre** | `credito-personal` |
| **Root directory** | `Credito/modern/Credito.Modern.Web` |
| **Build command** | `npm run build` |
| **Deploy command** | `npx wrangler deploy` (default del wizard) |
| **Branch** | `Eber-migracion` |

3. **Environment variables** (Production y Preview):

```text
VITE_BASE_URL=/
VITE_API_BASE_URL=https://api-credito-ocr-eug7aeh6dxcxcyh6.westcentralus-01.azurewebsites.net/api/v1
VITE_GOOGLE_MAPS_API_KEY=<misma clave Maps>
NODE_VERSION=22
```

4. Deploy. URL típica: `https://credito-personal.<subdominio>.workers.dev` (o custom).

**Pages clásico** (opcional): enlace “Continúa a Pages”; deploy `npx wrangler pages deploy dist`; output `dist`.

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
npx wrangler deploy
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
