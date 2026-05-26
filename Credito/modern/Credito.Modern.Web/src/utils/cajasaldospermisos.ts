/** Paridad `SaldosController` — rol LECTURA_SALDO (solo consulta, sin cierre ni asignación). */
export function esLecturaSaldoCaja(roles: string[]): boolean {
  return roles.some((r) => r.trim().toUpperCase() === 'LECTURA_SALDO')
}

export function puedeOperarCierreSaldos(roles: string[]): boolean {
  return !esLecturaSaldoCaja(roles)
}
