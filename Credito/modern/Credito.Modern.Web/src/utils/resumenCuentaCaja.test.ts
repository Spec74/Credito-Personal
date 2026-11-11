import { describe, expect, it } from 'vitest'
import {
  digitalesDesdeResumenCuenta,
  efectivoDesdeResumenCuenta,
  parseResumenCuentaCaja,
} from './resumenCuentaCaja'

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

  it('omite pares rotos sin descartar los válidos (paridad C#)', () => {
    expect(parseResumenCuentaCaja('sin formato conocido')).toEqual([])
    expect(parseResumenCuentaCaja('EFECTIVO = 100  YAPE')).toEqual([
      { cuenta: 'EFECTIVO', importe: 100 },
    ])
  })

  it('quita prefijo RESUMEN …:', () => {
    expect(parseResumenCuentaCaja('RESUMEN CAJA DIARIO: EFECTIVO = 10.00  YAPE = 5.00')).toEqual([
      { cuenta: 'EFECTIVO', importe: 10 },
      { cuenta: 'YAPE', importe: 5 },
    ])
  })

  it('efectivoDesdeResumenCuenta ignora digitales', () => {
    expect(
      efectivoDesdeResumenCuenta('EFECTIVO = 1000.00  YAPE = 150.00  INTERBANK = 150.00'),
    ).toBe(1000)
    expect(efectivoDesdeResumenCuenta('YAPE = 150.00')).toBe(0)
  })

  it('digitalesDesdeResumenCuenta suma medios no efectivo', () => {
    expect(
      digitalesDesdeResumenCuenta('EFECTIVO = 1000.00  YAPE = 150.00  INTERBANK = 150.00'),
    ).toBe(300)
  })

  it('suma efectivo en céntimos sin drift', () => {
    expect(efectivoDesdeResumenCuenta('EFECTIVO = 0.1  EFECTIVO = 0.2')).toBe(0.3)
  })
})
