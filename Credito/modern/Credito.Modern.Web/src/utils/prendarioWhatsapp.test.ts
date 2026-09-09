import { describe, expect, it } from 'vitest'
import { urlWhatsAppPrendario } from './prendarioWhatsapp'

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
