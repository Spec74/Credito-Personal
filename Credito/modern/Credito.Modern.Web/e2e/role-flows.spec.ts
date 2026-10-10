import { expect, test, type Page } from '@playwright/test'

/**
 * Flujos funcionales por perfil (capa 2 del E2E).
 * Requiere JWT API + SPA Vercel/local. Ver role-module-matrix.spec.ts.
 */

const base = (process.env.CREDITO_E2E_BASE_URL ?? 'http://127.0.0.1:5173').replace(/\/$/, '')
const apiBase = (
  process.env.CREDITO_E2E_API_URL ??
  'https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net'
).replace(/\/$/, '')
const oficinaId = Number(process.env.CREDITO_E2E_OFICINA_ID ?? '1')
const defaultPass = process.env.CREDITO_E2E_PASSWORD ?? '123.'

async function seedSession(page: Page, user: string, pass = defaultPass) {
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
  await page.goto(`${base}/inicio`)
  await expect(page.getByRole('button', { name: /salir/i })).toBeVisible({ timeout: 25_000 })
}

async function gotoOk(page: Page, path: string) {
  await page.goto(`${base}${path}`)
  await expect(page.getByText('Sin permiso')).toHaveCount(0)
  await expect(page.locator('#main-content, .credix-content').first()).toBeVisible({ timeout: 25_000 })
}

test.describe.configure({ mode: 'serial' })

test('ADMIN: seguridad + tesorería + aprobación con UI útil', async ({ page }) => {
  test.setTimeout(120_000)
  await seedSession(page, 'RMANTILLA')

  await gotoOk(page, '/admin/usuarios')
  await expect(page.getByRole('heading', { name: /usuario/i }).first()).toBeVisible({ timeout: 20_000 })
  await expect(page.locator('.ant-table, table, .ant-list').first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/admin/roles')
  await expect(page.getByRole('heading', { name: /rol/i }).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/mantenimiento/oficinas')
  await expect(page.getByText(/oficina/i).first()).toBeVisible()

  await gotoOk(page, '/tesoreria/boveda')
  await expect(page.getByText(/boveda|bóveda/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/credito/aprobar')
  await expect(page.getByText(/aprob/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/credito/condonaciones')
  await expect(page.getByText(/condon/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/reportes/cobranza')
  await expect(page.getByText(/cobranza|pago/i).first()).toBeVisible({ timeout: 20_000 })
})

test('GESTOR: cartera + clientes + simulador + prendario', async ({ page }) => {
  test.setTimeout(120_000)
  await seedSession(page, 'YCERVANTES')

  await gotoOk(page, '/clientes')
  await expect(page.getByRole('heading', { name: /cliente/i }).first()).toBeVisible({ timeout: 20_000 })
  await expect(
    page.getByPlaceholder(/buscar|dni|nombre/i).or(page.locator('input').first()),
  ).toBeVisible({ timeout: 15_000 })

  await gotoOk(page, '/credito/consulta')
  await expect(page.getByText(/cr[eé]dito|consulta/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/credito/simulador')
  await expect(page.getByText(/simulador/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/credito/tareas')
  await expect(page.getByText(/tarea/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/credito/prendario')
  await expect(page.getByText(/prendario/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/caja/diario')
  await expect(page.getByRole('heading', { name: /caja diario/i })).toBeVisible({ timeout: 20_000 })
})

test('APROBADOR: cola de aprobación + saldos + reportes', async ({ page }) => {
  test.setTimeout(90_000)
  await seedSession(page, 'BQUISPE')

  await gotoOk(page, '/credito/aprobar')
  await expect(page.getByText(/aprob/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/caja/saldos')
  await expect(page.getByText(/saldo/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/caja/verificar-pagos')
  await expect(page.getByText(/verificar|pago/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/reportes/credito')
  await expect(page.getByText(/reporte|cr[eé]dito|informe/i).first()).toBeVisible({ timeout: 20_000 })
})

test('CAJERO: sesión caja + operaciones del día', async ({ page }) => {
  test.setTimeout(90_000)
  await seedSession(page, 'JVILLALOBOS')

  await gotoOk(page, '/caja/diario')
  await expect(page.getByRole('heading', { name: /caja diario/i })).toBeVisible({ timeout: 20_000 })
  await expect(page.getByText(/ABIERTO|CAJA CENTRAL/i).first()).toBeVisible({ timeout: 20_000 })
  await expect(page.getByRole('button', { name: /ruta del cobrador/i })).toBeVisible()
  await expect(page.getByRole('button', { name: /cobros del d[ií]a/i })).toBeVisible()
  await expect(page.getByRole('tab', { name: /cobranzas/i })).toBeVisible()

  await gotoOk(page, '/caja/verificar-pagos')
  await expect(page.getByText(/verificar|pago/i).first()).toBeVisible({ timeout: 20_000 })

  await gotoOk(page, '/caja/saldos')
  await expect(page.getByText(/saldo/i).first()).toBeVisible({ timeout: 20_000 })
})
