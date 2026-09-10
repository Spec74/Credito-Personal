import { describe, expect, it } from 'vitest'
import {
  debeMostrarDashboardAnalista,
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

describe('dashboard analista en inicio', () => {
  it('muestra el tablero al analista y no al admin', () => {
    expect(debeMostrarDashboardAnalista(['ANALISTA'])).toBe(true)
    expect(debeMostrarDashboardAnalista(['ADMINISTRADOR'])).toBe(false)
    expect(debeMostrarDashboardAnalista(['ADMINISTRADOR', 'ANALISTA'])).toBe(false)
    expect(debeMostrarDashboardAnalista(['ADMINISTRADOR', 'ANALISTA'], 'analista')).toBe(true)
    expect(debeMostrarDashboardAnalista(['CAJERO'])).toBe(false)
  })
})
