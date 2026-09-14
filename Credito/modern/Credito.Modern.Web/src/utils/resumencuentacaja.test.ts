import { describe, expect, it } from 'vitest'
import { parseResumenCuentaCaja } from './resumenCuentaCaja'

describe('parseResumenCuentaCaja', () => {
  it('separa las cuentas del resumen de caja', () => {
    expect(parseResumenCuentaCaja('EFECTIVO = 1065.00  YAPE = 500.00')).toEqual([
      { cuenta: 'EFECTIVO', importe: 1065 },
      { cuenta: 'YAPE', importe: 500 },
    ])
  })

  it('admite denominaciones con espacios', () => {
    expect(parseResumenCuentaCaja('TARJETA DE CREDITO = 250.5')).toEqual([
      { cuenta: 'TARJETA DE CREDITO', importe: 250.5 },
    ])
  })

  it('admite importes negativos', () => {
    expect(parseResumenCuentaCaja('EFECTIVO = -40')).toEqual([
      { cuenta: 'EFECTIVO', importe: -40 },
    ])
  })

  it('devuelve vacío sin datos', () => {
    expect(parseResumenCuentaCaja(null)).toEqual([])
    expect(parseResumenCuentaCaja('   ')).toEqual([])
  })

  it('devuelve vacío si el formato no es el esperado (se mostrará el texto crudo)', () => {
    expect(parseResumenCuentaCaja('sin formato conocido')).toEqual([])
    expect(parseResumenCuentaCaja('EFECTIVO = 100  YAPE')).toEqual([])
  })
})
