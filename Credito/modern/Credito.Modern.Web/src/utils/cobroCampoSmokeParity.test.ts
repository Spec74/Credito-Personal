/**
 * Paridad de humo automatizable (G1/C1–C3 a nivel código).
 * Flujo: Cartera tipa → Cobrar/Registrar → Enviados.
 */
import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = join(dirname(fileURLToPath(import.meta.url)), '..')

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

  it('C1: tipar ≠ cobrado; Cobrar/Registrar con completarImpagos false', () => {
    const page = read('pages/caja/CobroBloquePage.tsx')
    const card = read('pages/caja/components/CobroBloqueFieldCard.tsx')
    expect(page).toContain('completarImpagos: false')
    expect(page).toContain('cobrarFila')
    expect(page).toContain('confirmarEnviarTipados')
    expect(page).not.toContain("por-enviar")
    expect(page).not.toContain('anadirAEnviar')
    expect(card).toContain('Cobrar')
    expect(card).not.toContain('Añadir a Enviar')
    expect(card).not.toContain('Quitar de Enviar')
    expect(page).not.toContain('pagarCuotaImporteLibre')
  })

  it('C2: sin day-lock duro; dock en Cartera con tipados', () => {
    const page = read('pages/caja/CobroBloquePage.tsx')
    expect(page).toContain('clearCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('isCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('markCobroBloqueEjecutadoHoy')
    expect(page).not.toContain('yaEjecutadoHoy')
    expect(page).toMatch(/actionDock[\s\S]*tabPrincipal === 'cartera'/)
    expect(page).toContain("key: 'enviados'")
    expect(page).not.toContain("key: 'por-enviar'")
  })

  it('C3: ruta /caja/cobro + CompletarImpagos solo Diario', () => {
    const routes = read('routes/index.tsx')
    const cobranzas = read('pages/caja/cajaDiario/CobranzasTab.tsx')
    const api = read('api/cajaDiario.ts')
    expect(routes).toContain('/caja/cobro')
    expect(routes).toContain('CobroBloquePage')
    expect(cobranzas).toContain('completarImpagos')
    expect(cobranzas).toContain('/caja/cobro')
    expect(api).toMatch(/completarImpagos:\s*body\.completarImpagos \?\? false/)
  })
})
