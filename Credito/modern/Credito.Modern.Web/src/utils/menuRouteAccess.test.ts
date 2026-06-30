import { describe, expect, it } from 'vitest'
import { hasMenuRouteAccess } from './menuRouteAccess'
import type { MenuItemDto } from '../types/api'

function menu(spaPath: string, denominacion = 'Menu'): MenuItemDto {
  return {
    menuId: Math.floor(Math.random() * 100_000),
    denominacion,
    modulo: null,
    url: spaPath,
    icono: null,
    indPadre: false,
    orden: null,
    referencia: null,
  }
}

describe('menuRouteAccess', () => {
  it('hub admin no habilita rutas administrativas exactas', () => {
    expect(hasMenuRouteAccess('/admin/usuarios', [menu('/administracion')])).toBe(false)
    expect(hasMenuRouteAccess('/admin/roles', [menu('/administracion')])).toBe(false)
    expect(hasMenuRouteAccess('/mantenimiento/oficinas', [menu('/administracion')])).toBe(false)
  })

  it('menu administrativo exacto habilita su ruta', () => {
    expect(hasMenuRouteAccess('/admin/usuarios', [menu('/usuario')])).toBe(true)
    expect(hasMenuRouteAccess('/admin/roles', [menu('/rol')])).toBe(true)
    expect(hasMenuRouteAccess('/mantenimiento/oficinas', [menu('/oficina')])).toBe(true)
  })

  it('hub caja no habilita operaciones sensibles exactas', () => {
    expect(hasMenuRouteAccess('/caja/asignar', [menu('/caja')])).toBe(false)
    expect(hasMenuRouteAccess('/caja/saldos', [menu('/caja')])).toBe(false)
    expect(hasMenuRouteAccess('/caja/verificar-pagos', [menu('/caja')])).toBe(false)
  })

  it('menu caja exacto habilita operaciones asignadas', () => {
    expect(hasMenuRouteAccess('/caja/asignar', [menu('/caja/asignar')])).toBe(true)
    expect(hasMenuRouteAccess('/caja/saldos', [menu('/saldos')])).toBe(true)
    expect(hasMenuRouteAccess('/caja/verificar-pagos', [menu('/verificarpagos')])).toBe(true)
  })

  it('hub credito no habilita aprobacion ni parametros de simulador', () => {
    expect(hasMenuRouteAccess('/credito/aprobar', [menu('/credito')])).toBe(false)
    expect(hasMenuRouteAccess('/credito/parametros-simulador', [menu('/credito')])).toBe(false)
  })
})
