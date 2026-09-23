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

  it('menu boveda habilita el informe de movimientos de la misma oficina', () => {
    expect(hasMenuRouteAccess('/tesoreria/boveda', [menu('/boveda')])).toBe(true)
    expect(hasMenuRouteAccess('/tesoreria/movimiento-boveda', [menu('/boveda')])).toBe(true)
    expect(hasMenuRouteAccess('/tesoreria/movimiento-boveda', [menu('/tesoreria')])).toBe(true)
  })

  it('caja diario no habilita informe de movimiento boveda', () => {
    expect(hasMenuRouteAccess('/tesoreria/movimiento-boveda', [menu('/cajadiario')])).toBe(false)
  })

  it('hub admin habilita la pantalla reservada de comisiones', () => {
    expect(hasMenuRouteAccess('/admin/comisiones', [menu('/administracion')])).toBe(true)
    expect(hasMenuRouteAccess('/admin/comisiones', [menu('/comision')])).toBe(true)
  })

  it('el padre SEGURIDAD del SP no habilita usuarios ni roles', () => {
    const padre: MenuItemDto = {
      ...menu('', 'SEGURIDAD'),
      modulo: null,
      url: null,
      indPadre: true,
    }
    expect(hasMenuRouteAccess('/admin/usuarios', [padre])).toBe(false)
    expect(hasMenuRouteAccess('/admin/roles', [padre])).toBe(false)
  })

  it('el padre CREDITO del SP no habilita consulta (simulador sí: acceso rápido)', () => {
    const padre: MenuItemDto = {
      ...menu('', 'CREDITO'),
      modulo: null,
      url: null,
      indPadre: true,
    }
    expect(hasMenuRouteAccess('/credito/consulta', [padre])).toBe(false)
    expect(hasMenuRouteAccess('/credito/simulador', [padre])).toBe(true)
  })

  it('accesos rápidos del layout siempre habilitan comisiones y simulador', () => {
    expect(hasMenuRouteAccess('/admin/comisiones', [])).toBe(true)
    expect(hasMenuRouteAccess('/credito/simulador', [])).toBe(true)
  })

  it('menu Creditos (consulta) habilita ficha por persona', () => {
    expect(hasMenuRouteAccess('/credito/consulta', [menu('/credito/consulta')])).toBe(true)
    expect(hasMenuRouteAccess('/credito/persona/7942', [menu('/credito/consulta')])).toBe(true)
    expect(hasMenuRouteAccess('/credito/persona/7942', [menu('/clientes')])).toBe(false)
  })

  it('hub reportes credito no habilita cierre gerencial sin ACL extra', () => {
    const menuReportesCredito: MenuItemDto = {
      ...menu('Reporte/Credito', 'CREDITO'),
      modulo: 'REPORTES',
    }
    expect(hasMenuRouteAccess('/informes/cierre-gerencial', [menuReportesCredito])).toBe(false)
    expect(hasMenuRouteAccess('/informes/cobro-diario', [menuReportesCredito])).toBe(true)
  })

  it('extraAllowedPaths habilita cierre gerencial (UsuarioConsultaIds)', () => {
    expect(
      hasMenuRouteAccess('/informes/cierre-gerencial', [], ['/informes/cierre-gerencial']),
    ).toBe(true)
  })
})
