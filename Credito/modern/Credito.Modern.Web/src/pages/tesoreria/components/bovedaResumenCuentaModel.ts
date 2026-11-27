import type { BovedaMedioMarca } from '../../../api/boveda'
import {
  CENTRAL_WALLET_IDS,
  TIPO_PAGO_EFECTIVO,
  TIPO_PAGO_INTERBANK,
  TIPO_PAGO_INTERBANK_GERENTE,
  TIPO_PAGO_INTERBANK_RENE,
  TIPO_PAGO_YAPE,
  TIPO_PAGO_YAPE_GERENTE,
  TIPO_PAGO_YAPE_RENE,
  ordenTipoPago,
} from '../../../utils/tipoPago'

export function casiCero(n: number): boolean {
  return Math.abs(Number(n) || 0) < 0.01
}

export function etiquetaBilletera(tipoPagoId: number): string | null {
  switch (Number(tipoPagoId)) {
    case TIPO_PAGO_YAPE_GERENTE:
    case TIPO_PAGO_INTERBANK_GERENTE:
      return 'Gerente'
    case TIPO_PAGO_YAPE_RENE:
    case TIPO_PAGO_INTERBANK_RENE:
      return 'René'
    default:
      return null
  }
}

/** Hijos de ledger bajo el genérico de familia (vista Central). */
export const FAMILY_CHILDREN: Readonly<Record<number, readonly number[]>> = {
  [TIPO_PAGO_YAPE]: [TIPO_PAGO_YAPE_GERENTE, TIPO_PAGO_YAPE_RENE],
  [TIPO_PAGO_INTERBANK]: [TIPO_PAGO_INTERBANK_GERENTE, TIPO_PAGO_INTERBANK_RENE],
}

export type BovedaMedioCard = {
  marca: BovedaMedioMarca
  /** Billeteras anidadas (solo cuando agruparFamilias). */
  hijos: BovedaMedioMarca[]
  /** Cobros aún en caja para este tipo (o suma de familia). */
  enCaja: number
}

function sortCards(cards: BovedaMedioCard[]): BovedaMedioCard[] {
  return [...cards].sort(
    (a, b) => ordenTipoPago(a.marca.tipoPagoId) - ordenTipoPago(b.marca.tipoPagoId),
  )
}

/**
 * Arma tarjetas de medios.
 * Central (`agruparFamilias`): Yape/Interbank agrupan Gerente·René sin duplicar el ledger.
 * Sucursal: lista plana (sin billeteras 10–13).
 */
export function buildBovedaMedioCards(
  marcas: BovedaMedioMarca[],
  enCajas: BovedaMedioMarca[] | null | undefined,
  agruparFamilias: boolean,
): BovedaMedioCard[] {
  const byId = new Map(marcas.map((m) => [Number(m.tipoPagoId), m]))
  const enCajaById = new Map(
    (enCajas ?? []).map((m) => [Number(m.tipoPagoId), Number(m.total) || 0]),
  )

  if (!agruparFamilias) {
    return sortCards(
      marcas.map((marca) => ({
        marca,
        hijos: [],
        enCaja: enCajaById.get(Number(marca.tipoPagoId)) ?? 0,
      })),
    )
  }

  const used = new Set<number>()
  const cards: BovedaMedioCard[] = []

  for (const parentId of [TIPO_PAGO_YAPE, TIPO_PAGO_INTERBANK]) {
    const childIds = FAMILY_CHILDREN[parentId] ?? []
    const hijos = childIds
      .map((id) => byId.get(id))
      .filter((m): m is BovedaMedioMarca => !!m)
      .sort((a, b) => ordenTipoPago(a.tipoPagoId) - ordenTipoPago(b.tipoPagoId))
    const parent = byId.get(parentId)
    if (!parent && hijos.length === 0) continue

    const marca: BovedaMedioMarca = parent ?? {
      tipoPagoId: parentId,
      marca: parentId === TIPO_PAGO_YAPE ? 'Yape' : 'Interbank',
      variant: parentId === TIPO_PAGO_YAPE ? 'yape' : 'interbank',
      total: hijos.reduce((s, h) => s + Number(h.total), 0),
      sedes: [],
    }

    used.add(parentId)
    for (const id of childIds) used.add(id)

    const enCaja =
      (enCajaById.get(parentId) ?? 0) +
      childIds.reduce((s, id) => s + (enCajaById.get(id) ?? 0), 0)

    cards.push({ marca, hijos, enCaja })
  }

  for (const marca of marcas) {
    const id = Number(marca.tipoPagoId)
    if (used.has(id)) continue
    if (CENTRAL_WALLET_IDS.includes(id)) continue
    cards.push({
      marca,
      hijos: [],
      enCaja: enCajaById.get(id) ?? 0,
    })
  }

  return sortCards(cards)
}

/** Filtra medios en cero (efectivo siempre queda). */
export function filterBovedaMedioCards(
  cards: BovedaMedioCard[],
  mostrarCeros: boolean,
): BovedaMedioCard[] {
  if (mostrarCeros) return cards
  return cards.filter((card) => {
    const hijosSum = card.hijos.reduce((s, h) => s + Math.abs(Number(h.total)), 0)
    if (!casiCero(card.marca.total) || hijosSum > 0.01) return true
    if (!casiCero(card.enCaja)) return true
    if (Number(card.marca.tipoPagoId) === TIPO_PAGO_EFECTIVO) return true
    return false
  })
}
