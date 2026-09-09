import { describe, expect, it } from 'vitest'
import {
  buildClientesNuevoHref,
  buildPrendarioNuevoHref,
  labelClienteFicha,
  parseInternalPath,
  withSearchParam,
} from './internalReturnTo'

describe('parseInternalPath', () => {
  it('acepta rutas internas con query', () => {
    expect(parseInternalPath('/credito/prendario/nuevo')).toBe('/credito/prendario/nuevo')
    expect(parseInternalPath(' /clientes/nuevo?x=1 ')).toBe('/clientes/nuevo?x=1')
  })

  it('rechaza vacíos y URLs externas', () => {
    expect(parseInternalPath(null)).toBeNull()
    expect(parseInternalPath('')).toBeNull()
    expect(parseInternalPath('https://evil.example/x')).toBeNull()
    expect(parseInternalPath('//evil.example/x')).toBeNull()
    expect(parseInternalPath('\\clientes')).toBeNull()
  })
})

describe('withSearchParam', () => {
  it('agrega o reemplaza el parámetro', () => {
    expect(withSearchParam('/credito/prendario/nuevo', 'personaId', '9')).toBe(
      '/credito/prendario/nuevo?personaId=9',
    )
    expect(withSearchParam('/credito/prendario/nuevo?foo=1', 'personaId', '9')).toBe(
      '/credito/prendario/nuevo?foo=1&personaId=9',
    )
  })
})

describe('buildClientesNuevoHref', () => {
  it('lleva returnTo y DNI al alta de Clientes', () => {
    expect(
      buildClientesNuevoHref({ returnTo: '/credito/prendario/nuevo', dni: '74039360' }),
    ).toBe('/clientes/nuevo?returnTo=%2Fcredito%2Fprendario%2Fnuevo&dni=74039360')
  })
})

describe('buildPrendarioNuevoHref', () => {
  it('deja el cliente seleccionado al volver del alta', () => {
    expect(buildPrendarioNuevoHref(88, 'alta')).toBe(
      '/credito/prendario/nuevo?personaId=88&origen=alta',
    )
  })
})

describe('labelClienteFicha', () => {
  it('arma la etiqueta como el buscador de clientes', () => {
    expect(
      labelClienteFicha({
        numeroDocumento: '74039360',
        nombre: 'VICTOR',
        apePaterno: 'SULCA',
        apeMaterno: 'PAREDES',
      }),
    ).toBe('74039360 SULCA PAREDES, VICTOR')
  })
})
