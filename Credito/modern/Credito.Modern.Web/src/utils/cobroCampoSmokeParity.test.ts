/**
 * Paridad de humo automatizable (G1/C1–C3 a nivel código).
 * Flujo: Cartera tipa → Enviar revisa → Enviar lote → Enviados.
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

  it('C1: tipar / Añadir a Enviar ≠ cobrado; solo Enviar lote con completarImpagos false', () => {
    const page = read('pages/caja/CobroBloquePage.tsx')
    const card = read('pages/caja/components/CobroBloqueFieldCard.tsx')
    expect(page).toContain("completarImpagos: false")
    expect(page).toContain('anadirAEnviar')
    expect(page).toContain("tabPrincipal === 'por-enviar'")
    expect(page).toContain('Enviar lote')
    expect(card).toContain('Añadir a Enviar')
    expect(card).toContain('Quitar de Enviar')
    expect(card).not.toContain('Cobrar ya')
    expect(page).not.toContain('pagarCuotaImporteLibre')
  })

  it('C2: sin day-lock duro; dock solo en Enviar', () => {
    const page = read('pages/caja/CobroBloquePage.tsx')
    expect(page).toContain('clearCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('isCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('markCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('yaEjecutadoHoy')
    expect(page).toContain("tabPrincipal === 'por-enviar'")
    expect(page).toMatch(/actionDock[\s\S]*por-enviar/)
  })

  it('C3: ruta /caja/cobro + CompletarImpagos solo Diario', () => {
    const routes = read('routes/index.tsx')
    const cobranzas = read('pages/caja/cajaDiario/CobranzasTab.tsx')
    const api = read('api/cajaDiario.ts')
    expect(routes).toContain('path="/caja/cobro"')
    expect(routes).toContain('Navigate to="/caja/cobro"')
    expect(cobranzas).toContain('completarImpagos')
    expect(cobranzas).toContain('/caja/cobro')
    expect(api).toMatch(/completarImpagos:\s*body\.completarImpagos \?\? false/)
  })
})
