import { describe, expect, it } from 'vitest'
import { buildTipoPagoOptions } from './TipoPagoBrand'
import {
  esMarcaTipoPago,
  medioNombreMarca,
  medioNombrePorItemId,
  etiquetaSede,
  parseResumenBovedaTexto,
  resolveResumenCuentaVariant,
  medioEtiquetaUi,
} from './bovedaResumenCuentaParse'

const CATALOGO_MEDIOS = [
  { itemId: 1, denominacion: 'EFECTIVO', desCorta: null as string | null },
  { itemId: 2, denominacion: 'YAPE', desCorta: null },
  { itemId: 3, denominacion: 'INTERBANK', desCorta: null },
  { itemId: 4, denominacion: 'BCO CREDITO', desCorta: null },
  { itemId: 5, denominacion: 'BCO NACION', desCorta: null },
  { itemId: 6, denominacion: 'YAPE HUANTA', desCorta: 'OBSOLETO' },
  { itemId: 9, denominacion: 'PLIN', desCorta: null },
  { itemId: 10, denominacion: 'YAPE GERENTE', desCorta: 'CENTRAL' },
  { itemId: 11, denominacion: 'YAPE RENE', desCorta: 'CENTRAL' },
  { itemId: 12, denominacion: 'INTERBANK GERENTE', desCorta: 'CENTRAL' },
  { itemId: 13, denominacion: 'INTERBANK RENE', desCorta: 'CENTRAL' },
]

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
    expect(medioNombreMarca('YAPE RENE')).toBe('Yape René')
    expect(medioNombreMarca('Yape René')).toBe('Yape René')
    expect(medioNombreMarca('INTERBANK GERENTE')).toBe('Interbank Gerente')
    expect(medioNombreMarca('INTERBANK RENE')).toBe('Interbank René')
    expect(medioNombreMarca('Interbank René')).toBe('Interbank René')
    expect(medioNombreMarca('BCO CREDITO')).toBe('BCP')
    expect(medioNombreMarca('Banco de la Nación')).toBe('Banco de la Nación')
    expect(medioNombreMarca('PLIN')).toBe('Plin')
  })
})

describe('medioNombrePorItemId', () => {
  it('fija Gerente/René por ItemId', () => {
    expect(medioNombrePorItemId(10)).toBe('Yape Gerente')
    expect(medioNombrePorItemId(11)).toBe('Yape René')
    expect(medioNombrePorItemId(12)).toBe('Interbank Gerente')
    expect(medioNombrePorItemId(13)).toBe('Interbank René')
    expect(medioNombrePorItemId(9)).toBe('Plin')
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

describe('buildTipoPagoOptions', () => {
  it('Central: billeteras con nombre; sin genéricos 2/3', () => {
    const opts = buildTipoPagoOptions(CATALOGO_MEDIOS, { esCentral: true })
    expect(opts.map((o) => o.value)).toEqual([1, 10, 11, 12, 13, 4, 5, 9])
    expect(opts.map((o) => o.label)).toEqual([
      'Efectivo',
      'Yape Gerente',
      'Yape René',
      'Interbank Gerente',
      'Interbank René',
      'BCP',
      'Banco de la Nación',
      'Plin',
    ])
  })

  it('Sucursal: genéricos; sin billeteras CENTRAL', () => {
    const opts = buildTipoPagoOptions(CATALOGO_MEDIOS, { esCentral: false })
    expect(opts.map((o) => o.value)).toEqual([1, 2, 3, 4, 5, 9])
    expect(opts.map((o) => o.label)).toEqual([
      'Efectivo',
      'Yape',
      'Interbank',
      'BCP',
      'Banco de la Nación',
      'Plin',
    ])
  })
})
