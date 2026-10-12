# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: ciclo-capa4-extras.spec.ts >> ADMIN: bóveda + condonaciones + clientes
- Location: e2e\ciclo-capa4-extras.spec.ts:48:1

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByRole('heading', { name: /cliente/i }).first()
Expected: visible
Timeout: 20000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" getByRole('heading', { name: /cliente/i }).first() with timeout 20000ms
  - waiting for getByRole('heading', { name: /cliente/i }).first()

```

```yaml
- text: The server is configured with a public base URL of /app/ - did you mean to visit
- link "/app/clientes":
  - /url: /app/clientes
- text: instead?
```

# Test source

```ts
  1  | import { expect, test, type Page } from '@playwright/test'
  2  | 
  3  | /**
  4  |  * SPA post capa 4: bóveda, condonaciones, prendario, cliente.
  5  |  * Env: CREDITO_E2E_BASE_URL, CREDITO_E2E_API_URL, CREDITO_E2E_PASSWORD
  6  |  *      CREDITO_E2E_PRENDARIO_ID (default 87693)
  7  |  */
  8  | 
  9  | const base = (process.env.CREDITO_E2E_BASE_URL ?? 'http://127.0.0.1:5173').replace(/\/$/, '')
  10 | const apiBase = (
  11 |   process.env.CREDITO_E2E_API_URL ??
  12 |   'https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net'
  13 | ).replace(/\/$/, '')
  14 | const pass = process.env.CREDITO_E2E_PASSWORD ?? '123.'
  15 | const oficinaId = Number(process.env.CREDITO_E2E_OFICINA_ID ?? '1')
  16 | const prendarioId = Number(process.env.CREDITO_E2E_PRENDARIO_ID ?? '87693')
  17 | 
  18 | async function seed(page: Page, user: string) {
  19 |   const res = await fetch(`${apiBase}/api/v1/auth/login`, {
  20 |     method: 'POST',
  21 |     headers: { 'Content-Type': 'application/json' },
  22 |     body: JSON.stringify({ nombreUsuario: user, clave: pass, oficinaId }),
  23 |   })
  24 |   if (!res.ok) throw new Error(`login ${user} ${res.status}`)
  25 |   const tokens = (await res.json()) as {
  26 |     accessToken: string
  27 |     refreshToken: string
  28 |     expiresInSeconds: number
  29 |   }
  30 |   await page.goto(`${base}/login`)
  31 |   await page.evaluate(
  32 |     ({ access, refresh, expiresIn }) => {
  33 |       const expiresAt = String(Date.now() + Math.max(60, expiresIn) * 1000)
  34 |       localStorage.setItem('credito.access', access)
  35 |       localStorage.setItem('credito.accessExpiresAt', expiresAt)
  36 |       localStorage.setItem('credito.refreshPersist', '1')
  37 |       localStorage.setItem('credito.refresh', refresh)
  38 |       sessionStorage.setItem('credito.refresh', refresh)
  39 |     },
  40 |     {
  41 |       access: tokens.accessToken,
  42 |       refresh: tokens.refreshToken,
  43 |       expiresIn: tokens.expiresInSeconds,
  44 |     },
  45 |   )
  46 | }
  47 | 
  48 | test('ADMIN: bóveda + condonaciones + clientes', async ({ page }) => {
  49 |   test.setTimeout(90_000)
  50 |   await seed(page, 'RMANTILLA')
  51 | 
  52 |   await page.goto(`${base}/tesoreria/boveda`)
  53 |   await expect(page.getByText('Sin permiso')).toHaveCount(0)
  54 |   await expect(page.getByText(/boveda|bóveda|saldo/i).first()).toBeVisible({ timeout: 25_000 })
  55 | 
  56 |   await page.goto(`${base}/credito/condonaciones`)
  57 |   await expect(page.getByText('Sin permiso')).toHaveCount(0)
  58 |   await expect(page.getByText(/condon/i).first()).toBeVisible({ timeout: 20_000 })
  59 | 
  60 |   await page.goto(`${base}/clientes`)
> 61 |   await expect(page.getByRole('heading', { name: /cliente/i }).first()).toBeVisible({
     |                                                                         ^ Error: expect(locator).toBeVisible() failed
  62 |     timeout: 20_000,
  63 |   })
  64 | })
  65 | 
  66 | test('GESTOR: prendario listado refleja crédito desembolsado', async ({ page }) => {
  67 |   test.setTimeout(60_000)
  68 |   await seed(page, 'YCERVANTES')
  69 |   await page.goto(`${base}/credito/prendario`)
  70 |   await expect(page.getByText('Sin permiso')).toHaveCount(0)
  71 |   await expect(page.getByText(/prendario/i).first()).toBeVisible({ timeout: 20_000 })
  72 |   await expect(page.getByText(new RegExp(String(prendarioId))).first()).toBeVisible({
  73 |     timeout: 25_000,
  74 |   })
  75 | })
  76 | 
```