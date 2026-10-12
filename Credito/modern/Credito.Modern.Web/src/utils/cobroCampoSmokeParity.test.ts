/**
 * Paridad de humo automatizable (G1/C1–C3 a nivel código).
 * Los pasos HTTPS con dispositivo real (G2–G5) siguen en SMOKE-SIGN-OFF.
 */
import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { join } from 'node:path'

const root = join(__dirname, '..')

function read(rel: string) {
  return readFileSync(join(root, rel), 'utf8')
}

describe('cobro campo smoke parity (código)', () => {
  it('G1: PC no renderiza captura GPS en tabla (solo FieldCard móvil)', () => {
    const page = read('pages/caja/CobroBloquePage.tsx')
    const card = read('pages/caja/components/CobroBloqueFieldCard.tsx')
    expect(page).toContain('clienteMapaHref')
    expect(page).toContain('Ubicación')
    expect(page).not.toContain('Registrar mi ubicación')
    expect(card).toContain('Registrar mi ubicación')
    expect(card).toMatch(/Solo móvil|fieldMode|faltan/)
  })

  it('C1: tipar ≠ cobrado — Enviar cola / Cobrar ya con completarImpagos false', () => {
    const page = read('pages/caja/CobroBloquePage.tsx')
    expect(page).toContain("completarImpagos: false")
    expect(page).toContain('Por enviar')
    expect(page).toContain('Enviados')
    expect(page).toContain('pagarCuotaImporteLibre')
    expect(page).not.toMatch(/CompletarImpagos:\s*true/)
  })

  it('C2: sin day-lock duro', () => {
    const page = read('pages/caja/CobroBloquePage.tsx')
    expect(page).toContain('clearCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('isCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('markCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('yaEjecutadoHoy')
  })

  it('C3: ruta /caja/cobro + redirect legacy + CompletarImpagos solo Diario', () => {
    const routes = read('routes/index.tsx')
    const cobranzas = read('pages/caja/cajaDiario/CobranzasTab.tsx')
    const api = read('api/cajaDiario.ts')
    expect(routes).toContain('path="/caja/cobro"')
    expect(routes).toContain('path="/caja/cobro-bloque"')
    expect(routes).toContain('Navigate to="/caja/cobro"')
    expect(cobranzas).toContain('completarImpagos')
    expect(cobranzas).toContain('/caja/cobro')
    expect(api).toMatch(/completarImpagos:\s*body\.completarImpagos \?\? false/)
  })
})
