/**
 * Ancho mínimo para que Ant Design active el scroll horizontal y las columnas `fixed`.
 * `max-content` + `table-layout: auto` no ancla la última columna; hace falta un `x` numérico
 * y `table-layout: fixed`.
 */
export const SALDOS_ASIGNADAS_SCROLL_X = 1280
export const SALDOS_SESION_SCROLL_X = 1180

/** Alto del cuerpo: la barra horizontal queda dentro del viewport, no al final de la página. */
export const SALDOS_TABLE_BODY_Y = 'min(52vh, 560px)'

export const SALDOS_ASIGNADAS_SCROLL = {
  x: SALDOS_ASIGNADAS_SCROLL_X,
  y: SALDOS_TABLE_BODY_Y,
} as const

export const SALDOS_SESION_SCROLL = {
  x: SALDOS_SESION_SCROLL_X,
  y: SALDOS_TABLE_BODY_Y,
} as const
