import { expect, test, type Page } from '@playwright/test'

/**
 * SPA post capa 4: bóveda, condonaciones, prendario, cliente.
 * Env: CREDITO_E2E_BASE_URL, CREDITO_E2E_API_URL, CREDITO_E2E_PASSWORD
 *      CREDITO_E2E_PRENDARIO_ID (default 87693)
 */

const base = (process.env.CREDITO_E2E_BASE_URL ?? 'http://127.0.0.1:5173').replace(/\/$/, '')
const apiBase = (
  process.env.CREDITO_E2E_API_URL ??
  'https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net'
).replace(/\/$/, '')
const pass = process.env.CREDITO_E2E_PASSWORD ?? '123.'
const oficinaId = Number(process.env.CREDITO_E2E_OFICINA_ID ?? '1')
const prendarioId = Number(process.env.CREDITO_E2E_PRENDARIO_ID ?? '87693')

async function seed(page: Page, user: string) {
  const res = await fetch(`${apiBase}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario: user, clave: pass, oficinaId }),
  })
  if (!res.ok) throw new Error(`login ${user} ${res.status}`)
  const tokens = (await res.json()) as {
    accessToken: string
    refreshToken: string
    expiresInSeconds: number
  }
  await page.goto(`${base}/login`)
  await page.evaluate(
    ({ access, refresh, expiresIn }) => {
      const expiresAt = String(Date.now() + Math.max(60, expiresIn) * 1000)
      localStorage.setItem('credito.access', access)
      localStorage.setItem('credito.accessExpiresAt', expiresAt)
      localStorage.setItem('credito.refreshPersist', '1')
      localStorage.setItem('credito.refresh', refresh)
      sessionStorage.setItem('credito.refresh', refresh)
    },
    {
      access: tokens.accessToken,
      refresh: tokens.refreshToken,
      expiresIn: tokens.expiresInSeconds,
    },
  )
}

test('ADMIN: bóveda + condonaciones + clientes', async ({ page }) => {
  test.setTimeout(90_000)
  await seed(page, 'RMANTILLA')

  await page.goto(`${base}/tesoreria/boveda`)
  await expect(page.getByText('Sin permiso')).toHaveCount(0)
  await expect(page.getByText(/boveda|bóveda|saldo/i).first()).toBeVisible({ timeout: 25_000 })

  await page.goto(`${base}/credito/condonaciones`)
  await expect(page.getByText('Sin permiso')).toHaveCount(0)
  await expect(page.getByText(/condon/i).first()).toBeVisible({ timeout: 20_000 })

  await page.goto(`${base}/clientes`)
  await expect(page.getByRole('heading', { name: /cliente/i }).first()).toBeVisible({
    timeout: 20_000,
  })
})

test('GESTOR: prendario listado refleja crédito desembolsado', async ({ page }) => {
  test.setTimeout(60_000)
  await seed(page, 'YCERVANTES')
  await page.goto(`${base}/credito/prendario`)
  await expect(page.getByText('Sin permiso')).toHaveCount(0)
  await expect(page.getByText(/prendario/i).first()).toBeVisible({ timeout: 20_000 })
  await expect(page.getByText(new RegExp(String(prendarioId))).first()).toBeVisible({
    timeout: 25_000,
  })
})
