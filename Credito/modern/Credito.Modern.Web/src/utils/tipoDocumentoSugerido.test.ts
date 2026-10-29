import { describe, expect, it } from 'vitest'
import {
  preferenciaComprobanteCliente,
  seriePorTipoDocumento,
  sugerirTipoDocumento,
} from './tipoDocumentoSugerido'

const catalogo = [
  { tipoDocumentoId: 1, denominacion: 'FACTURA' },
  { tipoDocumentoId: 2, denominacion: 'BOLETA' },
  { tipoDocumentoId: 3, denominacion: 'NOTA DE VENTA' },
]

describe('sugerirTipoDocumento', () => {
  it('elige nota de venta para ingreso interno', () => {
    expect(sugerirTipoDocumento(catalogo, 'nota')?.tipoDocumentoId).toBe(3)
  })

  it('elige boleta o factura según preferencia', () => {
    expect(sugerirTipoDocumento(catalogo, 'boleta')?.tipoDocumentoId).toBe(2)
    expect(sugerirTipoDocumento(catalogo, 'factura')?.tipoDocumentoId).toBe(1)
  })
})

describe('seriePorTipoDocumento', () => {
  it('arma serie corta por tipo', () => {
    expect(seriePorTipoDocumento('FACTURA')).toBe('F001')
    expect(seriePorTipoDocumento('Boleta')).toBe('B001')
    expect(seriePorTipoDocumento('Nota de venta')).toBe('NV01')
  })
})

describe('preferenciaComprobanteCliente', () => {
  it('RUC o jurídica pide factura; DNI boleta', () => {
    expect(preferenciaComprobanteCliente('20123456789', 'N')).toBe('factura')
    expect(preferenciaComprobanteCliente('74039361', 'N')).toBe('boleta')
    expect(preferenciaComprobanteCliente('123', 'J')).toBe('factura')
  })
})
