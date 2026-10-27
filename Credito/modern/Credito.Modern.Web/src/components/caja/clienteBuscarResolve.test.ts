import { describe, expect, it } from 'vitest'
import {
  dniFromSearchTerm,
  etiquetaPersonaDocumento,
} from './clienteBuscarResolve'

describe('dniFromSearchTerm', () => {
  it('toma el DNI de 8 dígitos al inicio', () => {
    expect(dniFromSearchTerm('44684156')).toBe('44684156')
    expect(dniFromSearchTerm('44684156 JUAN PEREZ')).toBe('44684156')
  })

  it('no inventa DNI si el término no lo trae', () => {
    expect(dniFromSearchTerm('Juan Perez')).toBeUndefined()
    expect(dniFromSearchTerm('1234567')).toBeUndefined()
    expect(dniFromSearchTerm('123456789')).toBeUndefined()
  })
})

describe('etiquetaPersonaDocumento', () => {
  it('arma la etiqueta como el alta rápida', () => {
    expect(
      etiquetaPersonaDocumento({
        numeroDocumento: '44684156',
        nombre: 'JUAN',
        apePaterno: 'PEREZ',
        apeMaterno: 'LOPEZ',
      }),
    ).toBe('44684156 PEREZ LOPEZ, JUAN')
  })
})
