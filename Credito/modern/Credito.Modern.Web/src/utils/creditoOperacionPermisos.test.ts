import { describe, expect, it } from 'vitest'
import {
  esCreditoPerfilSoloBandeja,
  puedeOperarCreditoCompleto,
} from './creditoOperacionPermisos'

describe('creditoOperacionPermisos', () => {
  it('no trata como solo bandeja a analista con aprobadores', () => {
    const roles = ['ANALISTA', 'APROBADOR 1', 'APROBADOR 2']

    expect(esCreditoPerfilSoloBandeja(roles)).toBe(false)
    expect(puedeOperarCreditoCompleto(roles)).toBe(true)
  })

  it('mantiene APROBADOR 1 puro como solo bandeja', () => {
    expect(esCreditoPerfilSoloBandeja(['APROBADOR 1'])).toBe(true)
  })
})
