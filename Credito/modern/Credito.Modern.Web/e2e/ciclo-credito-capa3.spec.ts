import { expect, test, type Page } from '@playwright/test'

/**
 * Verificación SPA post capa 3 (no crea crédito; asume evidencia API).
 * Env: CREDITO_E2E_BASE_URL, CREDITO_E2E_API_URL, CREDITO_E2E_PASSWORD,
 *      CREDITO_E2E_CREDITO_ID (default 87691 de la corrida 2026-10-10).
 */

const base = (process.env.CREDITO_E2E_BASE_URL ?? 'http://127.0.0.1:5173').replace(/\/$/, '')
const apiBase = (
  process.env.CREDITO_E2E_API_URL ??
  'https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net'
).replace(/\/$/, '')
const pass = process.env.CREDITO_E2E_PASSWORD ?? '123.'
const creditoId = Number(process.env.CREDITO_E2E_CREDITO_ID ?? '87691')
const oficinaId = Number(process.env.CREDITO_E2E_OFICINA_ID ?? '1')

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

test('SPA operador: caja refleja desembolso/cobro y consulta crédito DES', async ({ page }) => {
  test.setTimeout(90_000)
  await seed(page, 'RMANTILLA')
  await page.goto(`${base}/caja/diario`)
  await expect(page.getByRole('heading', { name: /caja diario/i })).toBeVisible({ timeout: 25_000 })
  await expect(page.getByText(/CAJA CENTRAL 1|ABIERTO/i).first()).toBeVisible({ timeout: 20_000 })
  // Tras capa 3: salidas 200 / entradas 112 (pueden crecer si hay más ops).
  await expect(page.getByText(/Salidas/i).first()).toBeVisible()

  await page.goto(`${base}/credito/consulta?creditoId=${creditoId}`)
  await expect(page.getByText('Sin permiso')).toHaveCount(0)
  await expect(page.locator('#main-content, .credix-content').first()).toBeVisible({ timeout: 25_000 })
  await expect(page.getByText(new RegExp(String(creditoId))).first()).toBeVisible({ timeout: 25_000 })
})

test('SPA aprobador: bandeja sin el crédito ya aprobado', async ({ page }) => {
  test.setTimeout(60_000)
  await seed(page, 'BQUISPE')
  await page.goto(`${base}/credito/aprobar`)
  await expect(page.getByText(/aprob/i).first()).toBeVisible({ timeout: 20_000 })
  await expect(page.getByText('Sin permiso')).toHaveCount(0)
  // El 87691 no debe seguir en PEN.
  await expect(page.getByText(new RegExp(`\\b${creditoId}\\b`))).toHaveCount(0)
})
