/** Datos de ficha que ya existen; no inventa tope ni PLA. */

export type ClienteOriginacion = {
  bloqueado: boolean
  clienteEstado: boolean
  personaEstado: boolean
  topeCredito: number | null
}

export function motivoBloqueoOriginacion(c: ClienteOriginacion | null | undefined): string | null {
  if (!c) {
    return null
  }
  if (c.bloqueado) {
    return 'Cliente bloqueado'
  }
  if (!c.clienteEstado || !c.personaEstado) {
    return 'Cliente inactivo'
  }
  return null
}

export function topeCreditoVisible(tope: number | null | undefined): number | null {
  if (tope == null || !(tope > 0)) {
    return null
  }
  return tope
}

/** Aviso: el USP decide si admite el monto. La UI solo compara. */
export function montoSobreTope(
  monto: number | null | undefined,
  tope: number | null | undefined,
): boolean {
  if (monto == null || !(monto > 0)) {
    return false
  }
  const cap = topeCreditoVisible(tope)
  return cap != null && monto > cap
}
