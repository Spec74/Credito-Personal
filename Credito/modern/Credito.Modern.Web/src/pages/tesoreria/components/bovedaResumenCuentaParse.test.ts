import { describe, expect, it } from 'vitest'
import {
  esMarcaTipoPago,
  medioNombreMarca,
  etiquetaSede,
  parseResumenBovedaTexto,
  resolveResumenCuentaVariant,
  medioEtiquetaUi,
} from './bovedaResumenCuentaParse'

describe('resolveResumenCuentaVariant', () => {
  it('reconoce marcas y sedes', () => {
    expect(resolveResumenCuentaVariant('YAPE')).toEqual({
      etiqueta: 'Yape',
      variant: 'yape',
    })
    expect(resolveResumenCuentaVariant('YAPE HUANTA')).toEqual({
      etiqueta: 'Yape Huanta',
      variant: 'yape',
    })
    expect(resolveResumenCuentaVariant('BCP PICHARI')).toMatchObject({
      variant: 'bcp',
    })
  })
})

describe('medioEtiquetaUi', () => {
  it('arma aria y visible', () => {
    expect(medioEtiquetaUi('BCP PICHARI')).toMatchObject({
      aria: 'BCP Pichari',
      visible: 'Pichari',
    })
  })
})

describe('esMarcaTipoPago', () => {
  it('Central: billeteras sí; genéricos 2/3 no', () => {
    expect(esMarcaTipoPago({ itemId: 10, denominacion: 'YAPE GERENTE', desCorta: 'CENTRAL' }, true)).toBe(
      true,
    )
    expect(esMarcaTipoPago({ itemId: 12, denominacion: 'INTERBANK GERENTE', desCorta: 'CENTRAL' }, true)).toBe(
      true,
    )
    expect(esMarcaTipoPago({ itemId: 2, denominacion: 'YAPE' }, true)).toBe(false)
    expect(esMarcaTipoPago({ itemId: 3, denominacion: 'INTERBANK' }, true)).toBe(false)
    expect(esMarcaTipoPago({ itemId: 9, denominacion: 'PLIN' }, true)).toBe(true)
  })

  it('Sucursal: genéricos sí; billeteras CENTRAL no', () => {
    expect(esMarcaTipoPago({ itemId: 2, denominacion: 'YAPE' }, false)).toBe(true)
    expect(esMarcaTipoPago({ itemId: 3, denominacion: 'INTERBANK' }, false)).toBe(true)
    expect(esMarcaTipoPago({ itemId: 10, denominacion: 'YAPE GERENTE', desCorta: 'CENTRAL' }, false)).toBe(
      false,
    )
    expect(esMarcaTipoPago({ itemId: 13, denominacion: 'INTERBANK RENE', desCorta: 'CENTRAL' }, false)).toBe(
      false,
    )
    expect(esMarcaTipoPago({ itemId: 6, denominacion: 'YAPE HUANTA' }, false)).toBe(false)
    expect(esMarcaTipoPago({ itemId: 9, denominacion: 'PLIN' }, false)).toBe(true)
  })
})

describe('medioNombreMarca', () => {
  it('nombra marca y billeteras persona', () => {
    expect(medioNombreMarca('EFECTIVO')).toBe('Efectivo')
    expect(medioNombreMarca('YAPE')).toBe('Yape')
    expect(medioNombreMarca('YAPE HUANTA')).toBe('Yape')
    expect(medioNombreMarca('YAPE GERENTE')).toBe('Yape Gerente')
    expect(medioNombreMarca('YAPE RENE')).toBe('Yape Rene')
    expect(medioNombreMarca('INTERBANK GERENTE')).toBe('Interbank Gerente')
    expect(medioNombreMarca('INTERBANK RENE')).toBe('Interbank Rene')
    expect(medioNombreMarca('BCO CREDITO')).toBe('BCP')
    expect(medioNombreMarca('PLIN')).toBe('Plin')
  })
})

describe('etiquetaSede', () => {
  it('acorta Oficina.Denominacion', () => {
    expect(etiquetaSede('OFICINA PRINCIPAL', true)).toBe('Central')
    expect(etiquetaSede('SUCURSAL HUANTA', false)).toBe('Huanta')
  })
})

describe('parseResumenBovedaTexto', () => {
  it('parsea líneas de resumen', () => {
    const r = parseResumenBovedaTexto(
      'RESUMEN BOVEDA: EFECTIVO = 10.50  YAPE GERENTE = 20.00  INTERBANK RENE = 5.25',
    )
    expect(r.items.length).toBe(3)
    expect(r.items[0].monto).toBe(10.5)
  })
})
