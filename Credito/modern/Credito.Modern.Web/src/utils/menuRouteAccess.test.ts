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

// Filas reales de MAESTRO.Menu creadas por deploy/sql/2026-09-prendario-modulo.sql.
const menuPrendarioListado: MenuItemDto = {
  ...menu('Prendario', 'PRENDARIO - Listado'),
  modulo: 'PRENDARIO',
}

const menuPrendarioNuevo: MenuItemDto = {
  ...menu('Prendario/Create', 'PRENDARIO - Nuevo'),
  modulo: 'PRENDARIO',
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

  // Prendario es exclusivo del rol ANALISTA: tener el hub de credito no debe alcanzar.
  it('hub credito no habilita prendario', () => {
    expect(hasMenuRouteAccess('/credito/prendario', [menu('/credito')])).toBe(false)
    expect(hasMenuRouteAccess('/credito/prendario', [menu('/credito/creditos')])).toBe(false)
    expect(hasMenuRouteAccess('/credito/prendario', [menu('/tareas')])).toBe(false)
  })

  it('menu prendario del analista habilita la ruta', () => {
    expect(hasMenuRouteAccess('/credito/prendario', [menuPrendarioListado])).toBe(true)
    expect(hasMenuRouteAccess('/credito/prendario/nuevo', [menuPrendarioNuevo])).toBe(true)
    expect(hasMenuRouteAccess('/credito/prendario/gestionar/8', [menuPrendarioListado])).toBe(true)
  })

  it('listado no alcanza para el alta, y el hub de credito no alcanza gestionar', () => {
    expect(hasMenuRouteAccess('/credito/prendario/nuevo', [menuPrendarioListado])).toBe(false)
    expect(hasMenuRouteAccess('/credito/prendario/gestionar/8', [menu('/credito')])).toBe(false)
    expect(hasMenuRouteAccess('/credito/prendario/gestionar/8', [menu('/credito/creditos')])).toBe(false)
  })
})
