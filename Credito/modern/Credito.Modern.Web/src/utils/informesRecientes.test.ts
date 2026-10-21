/** @vitest-environment happy-dom */
import { afterEach, describe, expect, it } from 'vitest'
import {
  INFORMES_RECIENTES_KEY,
  rememberInforme,
  readInformesRecientes,
} from './informesRecientes'

describe('informes recientes', () => {
  afterEach(() => {
    localStorage.removeItem(INFORMES_RECIENTES_KEY)
  })

  it('guarda rutas de informe y ignora el hub', () => {
    rememberInforme('/informes/cobro-diario')
    rememberInforme('/informes')
    const rows = readInformesRecientes()
    expect(rows.map((r) => r.to)).toEqual(['/informes/cobro-diario'])
  })

  it('deja el último visitado primero y recorta a 6', () => {
    for (let i = 1; i <= 8; i += 1) {
      rememberInforme(`/informes/item-${i}`)
    }
    rememberInforme('/informes/item-3')
    const tos = readInformesRecientes().map((r) => r.to)
    expect(tos[0]).toBe('/informes/item-3')
    expect(tos).toHaveLength(6)
  })
})
