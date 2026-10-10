import { describe, expect, it } from 'vitest'
import {
  esLecturaSaldoCaja,
  puedeAsignarCajaUi,
  puedeCobroBloqueUi,
  puedeOperarCierreSaldos,
} from './cajaSaldosPermisos'

describe('cajaSaldosPermisos', () => {
  it('LECTURA_SALDO no puede asignar ni cerrar', () => {
    expect(esLecturaSaldoCaja(['LECTURA_SALDO'])).toBe(true)
    expect(puedeOperarCierreSaldos(['LECTURA_SALDO'])).toBe(false)
    expect(puedeAsignarCajaUi(['LECTURA_SALDO'])).toBe(false)
  })

  it('encargado y admin sí operan saldos', () => {
    expect(puedeOperarCierreSaldos(['ENCARGADO'])).toBe(true)
    expect(puedeOperarCierreSaldos(['ADMINISTRADOR'])).toBe(true)
  })

  it('admin y encargado pueden asignar caja; cajero no', () => {
    expect(puedeAsignarCajaUi(['ADMINISTRADOR'])).toBe(true)
    expect(puedeAsignarCajaUi(['ADMIN'])).toBe(true)
    expect(puedeAsignarCajaUi(['ENCARGADO'])).toBe(true)
    expect(puedeAsignarCajaUi(['Administrador General'])).toBe(true)
    expect(puedeAsignarCajaUi(['ENCARGADO DE CAJA'])).toBe(true)
    expect(puedeAsignarCajaUi(['CAJERO'])).toBe(false)
    expect(puedeAsignarCajaUi(['ANALISTA', 'CAJA'])).toBe(false)
    expect(puedeAsignarCajaUi(['CAJERO', 'LECTURA_SALDO'])).toBe(false)
  })

  it('gestor/analista/caja pueden entrar a cobro en bloque', () => {
    expect(puedeCobroBloqueUi(['ANALISTA'])).toBe(true)
    expect(puedeCobroBloqueUi(['GESTOR'])).toBe(true)
    expect(puedeCobroBloqueUi(['CAJA'])).toBe(true)
    expect(puedeCobroBloqueUi(['CAJA CENTRAL'])).toBe(true)
    expect(puedeCobroBloqueUi(['ADMINISTRADOR'])).toBe(true)
    expect(puedeCobroBloqueUi(['LECTURA'])).toBe(false)
  })
})
