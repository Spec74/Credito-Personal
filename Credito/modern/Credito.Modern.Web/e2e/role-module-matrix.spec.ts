import { expect, test, type Page } from '@playwright/test'

/**
 * Matriz E2E rol × módulo contra SPA (prod o preview).
 *
 * Env:
 *   CREDITO_E2E_BASE_URL  (default http://127.0.0.1:5173)
 *   CREDITO_E2E_OFICINA_ID (default 1)
 *   CREDITO_E2E_PASSWORD   (default 123. — solo entornos de prueba)
 *
 * No guarda secretos en el repo. Rotar claves tras corridas en prod.
 */

const base = (process.env.CREDITO_E2E_BASE_URL ?? 'http://127.0.0.1:5173').replace(/\/$/, '')
const apiBase = (
  process.env.CREDITO_E2E_API_URL ??
  'https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net'
).replace(/\/$/, '')
const oficinaId = Number(process.env.CREDITO_E2E_OFICINA_ID ?? '1')
const defaultPass = process.env.CREDITO_E2E_PASSWORD ?? '123.'

type Profile = {
  id: string
  user: string
  pass?: string
  /** Rutas que el menú del rol debe permitir (carga de pantalla). */
  allowed: string[]
  /** Rutas que deben mostrar 403 Sin permiso. */
  forbidden: string[]
}

/** Un usuario representativo por perfil de rol real en prod. */
const PROFILES: Profile[] = [
  {
    id: 'ADMIN',
    user: 'RMANTILLA',
    allowed: [
      '/inicio',
      '/credito',
      '/credito/consulta',
      '/credito/simulador',
      '/credito/tareas',
      '/credito/aprobar',
      '/credito/condonaciones',
      '/credito/prendario',
      '/credito/prendario/nuevo',
      '/clientes',
      '/caja',
      '/caja/diario',
      '/caja/saldos',
      '/caja/chica',
      '/caja/verificar-pagos',
      '/tesoreria/boveda',
      '/admin',
      '/admin/usuarios',
      '/admin/roles',
      '/mantenimiento/oficinas',
      '/mantenimiento/cajas',
      '/reportes/credito',
      '/reportes/cobranza',
      '/informes',
      '/informes/cobro-diario',
      '/informes/saldo-cartera',
    ],
    forbidden: [],
  },
  {
    id: 'GESTOR_CAJA',
    user: 'YCERVANTES',
    allowed: [
      '/inicio',
      '/credito',
      '/credito/consulta',
      '/credito/simulador',
      '/credito/tareas',
      '/credito/prendario',
      '/credito/prendario/nuevo',
      '/clientes',
      '/caja/diario',
    ],
    forbidden: [
      '/admin/usuarios',
      '/admin/roles',
      '/tesoreria/boveda',
      '/credito/aprobar',
      '/caja/saldos',
      '/caja/chica',
      '/credito/condonaciones',
    ],
  },
  {
    id: 'APROBADOR_REPORTE',
    user: 'BQUISPE',
    allowed: [
      '/inicio',
      '/credito/consulta',
      '/credito/aprobar',
      '/caja/diario',
      '/caja/saldos',
      '/caja/verificar-pagos',
      '/reportes/credito',
      '/reportes/cobranza',
      '/clientes',
      '/credito/prendario',
      '/credito/tareas',
    ],
    forbidden: ['/admin/usuarios', '/tesoreria/boveda', '/caja/chica'],
  },
  {
    id: 'CAJERO_ENCARGADO',
    user: 'JVILLALOBOS',
    allowed: [
      '/inicio',
      '/caja/diario',
      '/caja/saldos',
      '/caja/verificar-pagos',
      '/reportes/credito',
      '/credito/consulta',
      '/clientes',
      '/credito/prendario',
      '/credito/tareas',
      '/credito/simulador',
    ],
    forbidden: ['/admin/usuarios', '/tesoreria/boveda', '/credito/aprobar'],
  },
]

type LoginTokens = {
  accessToken: string
  refreshToken: string
  expiresInSeconds: number
  usuarioId: number
  oficinaId: number
  usuarioOficinaId?: number
}

async function apiLogin(user: string, pass: string): Promise<LoginTokens> {
  const res = await fetch(`${apiBase}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      nombreUsuario: user,
      clave: pass,
      oficinaId,
    }),
  })
  if (!res.ok) {
    const body = await res.text()
    throw new Error(`Login API ${res.status} for ${user}: ${body.slice(0, 200)}`)
  }
  return (await res.json()) as LoginTokens
}

/** Sesión real vía JWT (evita bloqueos de IP/UI del login headless en prod). */
async function seedSession(page: Page, user: string, pass: string) {
  const tokens = await apiLogin(user, pass)
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
  await expect(page).toHaveURL(/\/inicio/i, { timeout: 45_000 })
  await expect(page.getByRole('button', { name: /salir/i })).toBeVisible({ timeout: 25_000 })
}

async function assertPageLoads(page: Page, path: string) {
  await page.goto(`${base}${path}`)
  await expect(page).not.toHaveURL(/\/login/i, { timeout: 20_000 })
  await expect(page.locator('#main-content, .credix-content, main').first()).toBeVisible({
    timeout: 25_000,
  })
  await expect(page.getByText('Sin permiso')).toHaveCount(0)
  // Evitar crash de error boundary genérico.
  await expect(page.getByText(/unexpected application error|algo salió mal/i)).toHaveCount(0)
}

async function assertForbidden(page: Page, path: string) {
  await page.goto(`${base}${path}`)
  await expect(page).not.toHaveURL(/\/login/i, { timeout: 20_000 })
  await expect(page.getByText('Sin permiso')).toBeVisible({ timeout: 25_000 })
}

test.describe.configure({ mode: 'serial' })

for (const profile of PROFILES) {
  test.describe(`Perfil ${profile.id} (${profile.user})`, () => {
    test(`login + módulos permitidos + ACL denegados`, async ({ page }) => {
      test.setTimeout(180_000)
      const pass = profile.pass ?? defaultPass
      await seedSession(page, profile.user, pass)

      const failures: string[] = []
      for (const path of profile.allowed) {
        try {
          await assertPageLoads(page, path)
        } catch (e) {
          failures.push(`ALLOW ${path}: ${(e as Error).message?.split('\n')[0] ?? e}`)
        }
      }
      for (const path of profile.forbidden) {
        try {
          await assertForbidden(page, path)
        } catch (e) {
          failures.push(`DENY ${path}: ${(e as Error).message?.split('\n')[0] ?? e}`)
        }
      }

      expect(failures, failures.join('\n')).toEqual([])
    })
  })
}
