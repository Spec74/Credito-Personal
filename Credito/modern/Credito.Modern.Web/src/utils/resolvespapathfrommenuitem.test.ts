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

  it('resuelve CONDONACION al listado de solicitudes pendientes', () => {
    expect(resolveSpaPathFromMenuItem('/Condonacion/Index', 'CONDONACION', 'CREDITO')).toBe(
      '/credito/condonaciones',
    )
    expect(resolveSpaPathFromMenuItem(null, 'Condonación', 'CREDITO')).toBe(
      '/credito/condonaciones',
    )
  })

  it('resuelve Bóveda e informe de movimientos sin mezclarlos', () => {
    expect(resolveSpaPathFromMenuItem('/Boveda/Index', 'BOVEDA', 'CREDITO')).toBe(
      '/tesoreria/boveda',
    )
    expect(resolveSpaPathFromMenuItem(null, 'BOVEDA', 'CREDITO')).toBe('/tesoreria/boveda')
    expect(
      resolveSpaPathFromMenuItem(null, 'Reporte movimiento bóveda', 'CREDITO'),
    ).toBe('/tesoreria/movimiento-boveda')
    expect(
      resolveSpaPathFromMenuItem('/Reporte/ReporteMovimientoBoveda', 'Informes', 'REPORTES'),
    ).toBe('/tesoreria/movimiento-boveda')
  })

  it('resuelve Dashboard/Admin y Dashboard/Gestor al inicio (tablero por rol)', () => {
    expect(
      resolveSpaPathFromMenuItem('Dashboard/Admin', 'DASHBOARD', 'REPORTES'),
    ).toBe('/inicio')
    expect(
      resolveSpaPathFromMenuItem('/Dashboard/Gestor', 'Dashboard gestor', 'REPORTES'),
    ).toBe('/inicio')
    expect(resolveSpaPathFromMenuItem(null, 'DASHBOARD', 'REPORTES')).toBe('/inicio')
  })

  it('no convierte padres de menú vacíos en hubs (el SP los une y ampliaría permisos)', () => {
    expect(resolveSpaPathFromMenuItem(null, 'CREDITO', '')).toBeNull()
    expect(resolveSpaPathFromMenuItem(null, 'SEGURIDAD', '')).toBeNull()
    expect(resolveSpaPathFromMenuItem(null, 'REPORTES', '')).toBeNull()
    expect(resolveSpaPathFromMenuItem(null, 'MANTENIMIENTO', '')).toBeNull()
  })

  it('resuelve hijos de seguridad y catálogo por etiqueta si falta URL', () => {
    expect(resolveSpaPathFromMenuItem(null, 'USUARIO', 'SEGURIDAD')).toBe('/admin/usuarios')
    expect(resolveSpaPathFromMenuItem(null, 'ROL', 'SEGURIDAD')).toBe('/admin/roles')
    expect(resolveSpaPathFromMenuItem(null, 'Marcas', 'MAESTRO')).toBe('/maestros/marcas')
    expect(resolveSpaPathFromMenuItem(null, 'Venta rápida', 'VENTAS')).toBe(
      '/ventas/venta-rapida',
    )
  })

  it('resuelve el índice de reportes de venta sin caer al hub de informes', () => {
    expect(resolveSpaPathFromMenuItem(null, 'VENTA', 'REPORTES')).toBe('/reportes/venta')
  })

  it('cubre los hijos del menú vivo CREDITO (oficina 1) sin null', () => {
    const hijos: Array<[string, string, string, string]> = [
      ['CreditoAprobar', 'APROBACION', 'CREDITO', '/credito/aprobar'],
      ['Boveda', 'BOVEDA', 'CREDITO', '/tesoreria/boveda'],
      ['Credito/CajaDiario', 'CAJA DIARIO', 'CREDITO', '/caja/diario'],
      ['CajaChica', 'CAJACHICA', 'CREDITO', '/caja/chica'],
      ['Cliente', 'CLIENTE', 'CREDITO', '/clientes'],
      ['Condonacion', 'CONDONACION', 'CREDITO', '/credito/condonaciones'],
      ['Credito/Creditos', 'CREDITOS', 'CREDITO', '/credito/consulta'],
      ['Saldos', 'SALDOS CAJA', 'CREDITO', '/caja/saldos'],
      ['Credito/Simulador', 'SIMULADOR', 'CREDITO', '/credito/simulador'],
      ['Tareas', 'TAREAS', 'CREDITO', '/credito/tareas'],
      ['VerificarPagos', 'VERIFICAR PAGOS', 'CREDITO', '/caja/verificar-pagos'],
      ['Caja', 'CAJA', 'MANTENIMIENTO', '/mantenimiento/cajas'],
      ['Oficina', 'OFICINA', 'MANTENIMIENTO', '/mantenimiento/oficinas'],
      ['Reporte/CobranzaPagos', 'COBRANZA', 'REPORTES', '/reportes/cobranza'],
      ['Reporte/Credito', 'CREDITO', 'REPORTES', '/reportes/credito'],
      ['Dashboard/Admin', 'DASHBOARD', 'REPORTES', '/inicio'],
      ['Rol', 'ROL', 'SEGURIDAD', '/admin/roles'],
      ['Usuario', 'USUARIO', 'SEGURIDAD', '/admin/usuarios'],
      ['Prendario', 'PRENDARIO - Listado', 'PRENDARIO', '/credito/prendario'],
      ['Prendario/Create', 'PRENDARIO - Nuevo', 'PRENDARIO', '/credito/prendario/nuevo'],
    ]
    for (const [url, label, mod, expected] of hijos) {
      expect(resolveSpaPathFromMenuItem(url, label, mod), `${label} (${url})`).toBe(expected)
    }
  })
})
