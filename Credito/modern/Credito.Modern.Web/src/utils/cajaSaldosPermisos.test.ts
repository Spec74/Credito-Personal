import { describe, expect, it } from 'vitest'
import {
  esLecturaSaldoCaja,
  puedeAsignarCajaUi,
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
    expect(puedeAsignarCajaUi(['CAJERO'])).toBe(false)
    expect(puedeAsignarCajaUi(['CAJERO', 'LECTURA_SALDO'])).toBe(false)
  })
})
