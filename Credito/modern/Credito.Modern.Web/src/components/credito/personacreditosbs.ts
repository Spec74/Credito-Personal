/** Paridad clases `.riesgo-sbs-*` en `Creditos.cshtml`. */
export function sbsRiesgoClassName(codigo: string | null | undefined): string {
  if (codigo == null || codigo === '') {
    return ''
  }
  return `credito-sbs-riesgo credito-sbs-riesgo--${codigo}`
}

export function calificacionTagColor(
  calificacion: string,
): 'success' | 'warning' | 'error' | 'default' {
  switch ((calificacion ?? '').trim().toUpperCase()) {
    case 'A':
      return 'success'
    case 'B':
      return 'warning'
    case 'C':
      return 'error'
    default:
      return 'default'
  }
}
