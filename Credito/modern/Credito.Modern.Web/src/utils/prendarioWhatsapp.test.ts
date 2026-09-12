import { describe, expect, it } from 'vitest'
import { celularPrendarioEsValido, urlWhatsAppPrendario } from './prendarioWhatsapp'

describe('urlWhatsAppPrendario', () => {
  it('antepone 51 a un celular peruano de 9 dígitos', () => {
    const url = urlWhatsAppPrendario('982137430', 'JUAN PEREZ', 80419)
    expect(url).toContain('https://wa.me/51982137430?text=')
    expect(decodeURIComponent(url ?? '')).toContain('crédito prendario N° 80419')
  })

  it('no arma enlace si falta celular', () => {
    expect(urlWhatsAppPrendario(null, 'JUAN PEREZ', 1)).toBeNull()
    expect(urlWhatsAppPrendario('123', 'JUAN PEREZ', 1)).toBeNull()
  })
})

describe('celularPrendarioEsValido', () => {
  it('acepta 9 dígitos y 51 + 9', () => {
    expect(celularPrendarioEsValido('982137430')).toBe(true)
    expect(celularPrendarioEsValido('51 982 137 430')).toBe(true)
    expect(celularPrendarioEsValido(null)).toBe(false)
    expect(celularPrendarioEsValido('123')).toBe(false)
  })
})
