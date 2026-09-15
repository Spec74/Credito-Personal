import { describe, expect, it } from 'vitest'
import { esLecturaSaldoCaja, puedeOperarCierreSaldos } from './cajaSaldosPermisos'

describe('cajaSaldosPermisos', () => {
  it('LECTURA_SALDO no puede asignar ni cerrar', () => {
    expect(esLecturaSaldoCaja(['LECTURA_SALDO'])).toBe(true)
    expect(puedeOperarCierreSaldos(['LECTURA_SALDO'])).toBe(false)
  })

  it('encargado y admin sí operan saldos', () => {
    expect(puedeOperarCierreSaldos(['ENCARGADO'])).toBe(true)
    expect(puedeOperarCierreSaldos(['ADMINISTRADOR'])).toBe(true)
  })
})
