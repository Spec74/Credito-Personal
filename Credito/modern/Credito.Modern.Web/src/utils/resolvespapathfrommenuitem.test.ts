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

  it('prioriza Simulador por etiqueta aunque la URL legacy sea generica', () => {
    expect(resolveSpaPathFromMenuItem('/Credito/Index', 'Simulador', null)).toBe(
      '/credito/simulador',
    )
  })

  it('resuelve Simular por etiqueta alternativa legacy', () => {
    expect(resolveSpaPathFromMenuItem('/Credito/Index', 'Simular crédito', null)).toBe(
      '/credito/simulador',
    )
  })

  it('resuelve Simulador por URL aunque la etiqueta venga generica', () => {
    expect(resolveSpaPathFromMenuItem('/Credito/Simulador', 'Crédito', null)).toBe(
      '/credito/simulador',
    )
  })

  it('resuelve Prendario por etiqueta aunque no venga modulo Credito', () => {
    expect(resolveSpaPathFromMenuItem(null, 'Crédito prendario', null)).toBe(
      '/credito/prendario',
    )
  })

  it('resuelve PRENDARIO - Nuevo a la alta y Listado al índice', () => {
    expect(resolveSpaPathFromMenuItem('Prendario/Create', 'PRENDARIO - Nuevo', 'PRENDARIO')).toBe(
      '/credito/prendario/nuevo',
    )
    expect(resolveSpaPathFromMenuItem('Prendario', 'PRENDARIO - Listado', 'PRENDARIO')).toBe(
      '/credito/prendario',
    )
  })

  it('resuelve Prendario por URL aunque la etiqueta venga generica', () => {
    expect(resolveSpaPathFromMenuItem('~/Credito/Prendario', 'Crédito', null)).toBe(
      '/credito/prendario',
    )
  })

  it('resuelve Tareas por etiqueta aunque el modulo venga vacio', () => {
    expect(resolveSpaPathFromMenuItem(null, 'Tareas', '')).toBe('/credito/tareas')
  })

  it('resuelve Tareas por URL aunque la etiqueta venga generica', () => {
    expect(resolveSpaPathFromMenuItem('/Credito/Tareas', 'Crédito', null)).toBe(
      '/credito/tareas',
    )
  })
})
