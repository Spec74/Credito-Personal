/** Paridad `SaldosController` — rol LECTURA_SALDO (solo consulta, sin cierre ni asignación). */
export function esLecturaSaldoCaja(roles: string[]): boolean {
  return roles.some((r) => r.trim().toUpperCase() === 'LECTURA_SALDO')
}

export function puedeOperarCierreSaldos(roles: string[]): boolean {
  return !esLecturaSaldoCaja(roles)
}

function normalizeRole(role: string): string {
  return role.trim().toUpperCase().replace(/\s+/g, '')
}

export function puedeAnularMovimientoCaja(roles: string[]): boolean {
  const normalized = roles.map(normalizeRole)
  return (
    normalized.includes('ADMINISTRADOR') ||
    normalized.includes('ADMIN') ||
    normalized.includes('ANULACION_MOV')
  )
}
