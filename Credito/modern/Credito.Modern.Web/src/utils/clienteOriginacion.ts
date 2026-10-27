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

/** Móvil peruano: 9 dígitos que empiezan con 9 (paridad PrendarioValidacion). */
export function celularPrendarioClienteOk(celular: string | null | undefined): boolean {
  const digits = (celular ?? '').replace(/\D/g, '')
  return digits.length === 9 && digits.startsWith('9')
}

export function motivoBloqueoPrendario(
  c: (ClienteOriginacion & { celular1?: string | null }) | null | undefined,
): string | null {
  const base = motivoBloqueoOriginacion(c)
  if (base) {
    return base
  }
  if (!c || !celularPrendarioClienteOk(c.celular1)) {
    return 'El celular del cliente es obligatorio (9 dígitos que empiezan con 9).'
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
