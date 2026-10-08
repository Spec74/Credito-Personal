import { test, expect } from '@playwright/test'

/**
 * Smoke E2E mínimo. Sin CREDITO_E2E_USER / CREDITO_E2E_PASSWORD se omite el login real
 * y solo valida que la SPA arranca (útil en CI sin secretos).
 */
const base = process.env.CREDITO_E2E_BASE_URL ?? 'http://127.0.0.1:5173'
const user = process.env.CREDITO_E2E_USER
const pass = process.env.CREDITO_E2E_PASSWORD

test.describe('SPA smoke', () => {
  test('carga la pantalla de login', async ({ page }) => {
    await page.goto(`${base}/app/login`)
    await expect(page.locator('body')).toBeVisible()
    await expect(page.getByText(/iniciar sesión|usuario|contraseña|credix/i).first()).toBeVisible({
      timeout: 15_000,
    })
  })

  test('login → inicio → menú (requiere credenciales)', async ({ page }) => {
    test.skip(!user || !pass, 'Defina CREDITO_E2E_USER y CREDITO_E2E_PASSWORD')

    await page.goto(`${base}/app/login`)
    await page.getByLabel(/usuario/i).fill(user!)
    await page.getByLabel(/contraseña|clave/i).fill(pass!)
    await page.getByRole('button', { name: /ingresar|entrar|iniciar/i }).click()
    await expect(page).toHaveURL(/\/app\/(inicio|home)?/i, { timeout: 30_000 })
    await expect(page.locator('#credix-sidebar-nav, .credix-sidebar, .app-shell-drawer')).toBeVisible({
      timeout: 15_000,
    })
  })
})
