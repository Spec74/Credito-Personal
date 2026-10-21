import { describe, expect, it } from 'vitest'
import { CLIENTE_CRUZADO_AVISO, esBusquedaPersonaGlobal } from './clienteCruzado'

describe('clienteCruzado', () => {
  it('la búsqueda de persona es global y no invita a duplicar', () => {
    expect(esBusquedaPersonaGlobal()).toBe(true)
    expect(CLIENTE_CRUZADO_AVISO).toMatch(/no cree un duplicado/i)
    expect(CLIENTE_CRUZADO_AVISO).toMatch(/otra oficina/i)
  })
})
