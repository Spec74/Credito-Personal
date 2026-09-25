import { describe, expect, it } from 'vitest'
import {
  debeMostrarDashboardAdmin,
  debeMostrarDashboardAnalista,
  esCreditoPerfilSoloBandeja,
  puedeOperarCreditoCompleto,
  puedeCompletarBienesPrendarioUi,
  esCreditoProductoPrendario,
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

describe('dashboard admin en inicio', () => {
  it('muestra el tablero gerencial al admin salvo mapa o tablero personal', () => {
    expect(debeMostrarDashboardAdmin(['ADMINISTRADOR'])).toBe(true)
    expect(debeMostrarDashboardAdmin(['ADMIN'])).toBe(true)
    expect(debeMostrarDashboardAdmin(['ADMINISTRADOR'], 'modulos')).toBe(false)
    expect(debeMostrarDashboardAdmin(['ADMINISTRADOR', 'ANALISTA'], 'analista')).toBe(false)
    expect(debeMostrarDashboardAdmin(['ADMINISTRADOR', 'ANALISTA'])).toBe(true)
    expect(debeMostrarDashboardAdmin(['ANALISTA'])).toBe(false)
    expect(debeMostrarDashboardAdmin(['CAJERO'])).toBe(false)
  })
})

describe('bienes prendario', () => {
  it('permite completar bienes a analista y admin', () => {
    expect(puedeCompletarBienesPrendarioUi(['ANALISTA'])).toBe(true)
    expect(puedeCompletarBienesPrendarioUi(['ADMINISTRADOR'])).toBe(true)
    expect(puedeCompletarBienesPrendarioUi(['APROBADOR 1'])).toBe(false)
    expect(puedeCompletarBienesPrendarioUi(['LECTURA', 'ANALISTA'])).toBe(false)
  })

  it('detecta producto prendario por flag o productoId 2', () => {
    expect(esCreditoProductoPrendario({ esPrendario: true, productoId: 1 })).toBe(true)
    expect(esCreditoProductoPrendario({ esPrendario: false, productoId: 2 })).toBe(true)
    expect(esCreditoProductoPrendario({ esPrendario: false, productoId: 1 })).toBe(false)
  })
})
