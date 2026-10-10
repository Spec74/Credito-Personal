/** Rol LECTURA_SALDO: solo consulta, sin cierre ni asignación. */
export function esLecturaSaldoCaja(roles: string[]): boolean {
  return roles.some((r) => normalizeRole(r) === 'LECTURA_SALDO')
}

export function puedeOperarCierreSaldos(roles: string[]): boolean {
  return !esLecturaSaldoCaja(roles)
}

function normalizeRole(role: string): string {
  return role.trim().toUpperCase().replace(/\s+/g, '')
}

function roleMatches(normalized: string, expected: string): boolean {
  return normalized === expected || normalized.startsWith(expected)
}

/** Quien puede abrir/asignar cajas desde UI (admin / encargado). Cajero puro no. */
export function puedeAsignarCajaUi(roles: string[]): boolean {
  if (esLecturaSaldoCaja(roles)) return false
  return roles.some((role) => {
    const r = normalizeRole(role)
    return (
      roleMatches(r, 'ADMINISTRADOR') ||
      r === 'ADMIN' ||
      roleMatches(r, 'ENCARGADO')
    )
  })
}

/**
 * Cobro en bloque: atajo de Caja diario para campo (gestor/analista/caja).
 * No exige ítem de menú propio; AppShell lo suma a extraAllowedPaths.
 */
export function puedeCobroBloqueUi(roles: string[]): boolean {
  return roles.some((role) => {
    const r = normalizeRole(role)
    return (
      roleMatches(r, 'ANALISTA') ||
      roleMatches(r, 'GESTOR') ||
      roleMatches(r, 'CAJA') ||
      roleMatches(r, 'CAJERO') ||
      roleMatches(r, 'ADMINISTRADOR') ||
      r === 'ADMIN' ||
      roleMatches(r, 'ENCARGADO')
    )
  })
}

export function puedeAnularMovimientoCaja(roles: string[]): boolean {
  return roles.some((role) => {
    const r = normalizeRole(role)
    return (
      roleMatches(r, 'ADMINISTRADOR') ||
      r === 'ADMIN' ||
      roleMatches(r, 'ANULACION_MOV')
    )
  })
}
