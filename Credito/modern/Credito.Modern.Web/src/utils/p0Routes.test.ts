import { describe, expect, it } from 'vitest'
import { buildRoleHomeActions } from './roleHomeActions'
import { P0_ROLE_HOMES, P0_ROUTES } from './p0Routes'

describe('T-A9 P0', () => {
  it('lista las rutas críticas del spec', () => {
    expect(P0_ROUTES).toContain('/caja/cobro')
    expect(P0_ROUTES).toContain('/credito/simulador')
    expect(P0_ROUTES).toContain('/credito/prendario/nuevo')
    expect(P0_ROUTES).toContain('/inicio')
  })

  it('Hoy de cada rol apunta a rutas P0 o de red ya existentes', () => {
    const cajero = buildRoleHomeActions(['CAJERO']).map((a) => a.to)
    expect(cajero).toEqual(expect.arrayContaining([...P0_ROLE_HOMES.cajero]))

    const analista = buildRoleHomeActions(['ANALISTA']).map((a) => a.to)
    expect(analista).toEqual(expect.arrayContaining([...P0_ROLE_HOMES.analista]))

    const aprobador = buildRoleHomeActions(['APROBADOR 1']).map((a) => a.to)
    expect(aprobador).toEqual(expect.arrayContaining([...P0_ROLE_HOMES.aprobador]))

    const encargado = buildRoleHomeActions(['ENCARGADO']).map((a) => a.to)
    expect(encargado).toEqual(expect.arrayContaining([...P0_ROLE_HOMES.encargado]))

    const admin = buildRoleHomeActions(['ADMINISTRADOR']).map((a) => a.to)
    expect(admin).toEqual(expect.arrayContaining([...P0_ROLE_HOMES.admin]))
  })
})
