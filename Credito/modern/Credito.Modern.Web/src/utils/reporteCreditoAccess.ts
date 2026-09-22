/** Roles con acceso a la rejilla Reportes → Crédito del MVC. */
const REPORTE_CREDITO_ROLES = [
  'ADMIN',
  'APROBADOR',
  'PARCIAL',
  'REPORTEPARCIAL',
  'ADMINISTRADOR',
]

const REPORTE_CREDITO_ADMIN_ROLES = ['ADMIN', 'ADMINISTRADOR']

function roleMatchesReporteCredito(role: string): boolean {
  const upper = role.trim().toUpperCase()
  if (REPORTE_CREDITO_ROLES.includes(upper)) {
    return true
  }
  if (upper.startsWith('APROBADOR')) {
    return true
  }
  // MAESTRO.Rol.Denominacion = REPORTEPARCIAL → ViewBag.rol = PARCIAL en MVC.
  return upper.includes('PARCIAL') && !upper.includes('IMPARCIAL')
}

export function canViewReporteCredito(roles: string[]): boolean {
  return roles.some(roleMatchesReporteCredito)
}

export function canViewReporteCreditoAdmin(roles: string[]): boolean {
  const upper = roles.map((r) => r.trim().toUpperCase())
  return REPORTE_CREDITO_ADMIN_ROLES.some((r) => upper.includes(r))
}

export function canViewReporteCreditoAprobador(roles: string[]): boolean {
  return roles.some((r) => r.trim().toUpperCase().startsWith('APROBADOR')) || canViewReporteCreditoAdmin(roles)
}
