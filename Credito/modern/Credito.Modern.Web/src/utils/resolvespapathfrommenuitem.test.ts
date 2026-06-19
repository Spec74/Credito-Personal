import { describe, expect, it } from 'vitest'
import { resolveSpaPathFromMenuItem } from './resolveSpaPathFromMenuItem'

describe('resolveSpaPathFromMenuItem', () => {
  it('mapea /Tareas/Index al listado de tareas', () => {
    expect(resolveSpaPathFromMenuItem('/Tareas/Index', 'Tareas', 'CREDITO')).toBe(
      '/credito/tareas',
    )
  })

  it('resuelve Tareas por denominación sin depender del hub', () => {
    expect(resolveSpaPathFromMenuItem(null, 'Tareas', 'CREDITO')).toBe('/credito/tareas')
  })

  it('resuelve Cliente del módulo Crédito a /clientes', () => {
    expect(resolveSpaPathFromMenuItem(null, 'Cliente', 'CREDITO')).toBe('/clientes')
  })

  it('mantiene hub /credito solo para ítems genéricos del módulo', () => {
    expect(resolveSpaPathFromMenuItem(null, 'Credito', 'CREDITO')).toBe('/credito')
  })

  it('resuelve Crédito > Créditos directamente al flujo legacy modernizado', () => {
    expect(resolveSpaPathFromMenuItem('/Credito', 'Creditos', 'CREDITO')).toBe(
      '/credito/consulta',
    )
  })

  it('conserva personaId al abrir Creditos desde una URL legacy', () => {
    expect(
      resolveSpaPathFromMenuItem('/Credito/Creditos?pPersonaId=123', 'Creditos', 'CREDITO'),
    ).toBe('/credito/consulta?personaId=123')
  })
})
