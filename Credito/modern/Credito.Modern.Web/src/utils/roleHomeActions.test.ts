import { describe, expect, it } from 'vitest'
import { buildRoleHomeActions } from './roleHomeActions'

describe('buildRoleHomeActions', () => {
  it('deja al aprobador 1 solo con la bandeja', () => {
    const actions = buildRoleHomeActions(['APROBADOR 1'])
    expect(actions).toEqual([{ to: '/credito/aprobar', label: 'Aprobar créditos', primary: true }])
  })

  it('prioriza caja del día al cajero', () => {
    const actions = buildRoleHomeActions(['CAJERO'])
    expect(actions[0]).toEqual({ to: '/caja/diario', label: 'Caja del día', primary: true })
    expect(actions.some((a) => a.to === '/caja/cobro')).toBe(true)
  })

  it('ofrece originación al analista', () => {
    const tos = buildRoleHomeActions(['ANALISTA']).map((a) => a.to)
    expect(tos).toContain('/credito/prendario/nuevo')
    expect(tos).toContain('/credito/consulta')
    expect(tos).toContain('/credito/tareas')
  })

  it('incluye red, fondeo y aprobación para administrador', () => {
    const tos = buildRoleHomeActions(['ADMINISTRADOR']).map((a) => a.to)
    expect(tos).toContain('/credito/aprobar')
    expect(tos).toContain('/tesoreria/red')
    expect(tos).toContain('/tesoreria/fondear-sucursales')
    expect(tos).toContain('/informes')
    expect(tos).not.toContain('/ventas/venta-rapida')
    expect(tos.length).toBeLessThanOrEqual(6)
  })

  it('no deja que caja+analista tapen informes y red del administrador', () => {
    const tos = buildRoleHomeActions([
      'ADMINISTRADOR',
      'ANALISTA',
      'CAJERO',
      'APROBADOR 1',
    ]).map((a) => a.to)
    expect(tos[0]).toBe('/caja/diario')
    expect(tos).toContain('/informes')
    expect(tos).toContain('/tesoreria/red')
    expect(tos).toContain('/credito/aprobar')
    expect(tos.length).toBeLessThanOrEqual(6)
  })

  it('no inventa rutas: fallback de consulta', () => {
    const actions = buildRoleHomeActions([])
    expect(actions[0].to).toBe('/credito/consulta')
  })
})
