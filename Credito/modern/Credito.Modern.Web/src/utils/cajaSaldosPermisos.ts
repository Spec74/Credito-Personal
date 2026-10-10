/** Rol LECTURA_SALDO: solo consulta, sin cierre ni asignación. */
export function esLecturaSaldoCaja(roles: string[]): boolean {
  return roles.some((r) => r.trim().toUpperCase() === 'LECTURA_SALDO')
}

export function puedeOperarCierreSaldos(roles: string[]): boolean {
  return !esLecturaSaldoCaja(roles)
}

function normalizeRole(role: string): string {
  return role.trim().toUpperCase().replace(/\s+/g, '')
}

/** Quien puede abrir/asignar cajas desde UI (admin / encargado). Cajero puro no. */
export function puedeAsignarCajaUi(roles: string[]): boolean {
  if (esLecturaSaldoCaja(roles)) return false
  const normalized = roles.map(normalizeRole)
  return (
    normalized.includes('ADMINISTRADOR') ||
    normalized.includes('ADMIN') ||
    normalized.includes('ENCARGADO')
  )
}

export function puedeAnularMovimientoCaja(roles: string[]): boolean {
  const normalized = roles.map(normalizeRole)
  return (
    normalized.includes('ADMINISTRADOR') ||
    normalized.includes('ADMIN') ||
    normalized.includes('ANULACION_MOV')
  )
}
