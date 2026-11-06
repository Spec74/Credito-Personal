import { moneyEquals, roundMoney, sumMoney } from './money'
import type { CobroBloqueRowDraft } from './cobroBloqueDraft'
import { esTipoPagoDigital } from './tipoPago'

export type PlanillaPayloadItem = {
  creditoId: number
  montoPagar: number
  tipoPagoId: number
  fechaHoraTrans: string
}

/** Expande un tipado a 1..N ítems de planilla (mixto = varios medios del mismo crédito). */
export function expandRowToPlanillaItems(
  creditoId: number,
  edit: CobroBloqueRowDraft,
): PlanillaPayloadItem[] {
  const medios = edit.medios?.filter((m) => roundMoney(m.importe) > 0) ?? []
  if (medios.length >= 2) {
    return medios.map((m) => ({
      creditoId,
      montoPagar: roundMoney(m.importe),
      tipoPagoId: m.tipoPagoId,
      fechaHoraTrans: m.fechaHoraTrans ?? '',
    }))
  }
  return [
    {
      creditoId,
      montoPagar: roundMoney(edit.montoPagar),
      tipoPagoId: edit.tipoPagoId,
      fechaHoraTrans: edit.fechaHoraTrans ?? '',
    },
  ]
}

export function validateRowMixtoMoney(edit: CobroBloqueRowDraft): string | null {
  const medios = edit.medios?.filter((m) => roundMoney(m.importe) > 0) ?? []
  if (medios.length < 2) return null
  const suma = sumMoney(medios.map((m) => m.importe))
  const esperado = roundMoney(edit.montoPagar)
  if (!moneyEquals(suma, esperado)) {
    return `La suma de medios (${suma.toFixed(2)}) debe coincidir al céntimo con el monto tipado (${esperado.toFixed(2)}).`
  }
  for (const m of medios) {
    if (esTipoPagoDigital(m.tipoPagoId) && !(m.fechaHoraTrans ?? '').trim()) {
      return 'Cada medio digital/transferencia (Yape, Plin, BCP, Interbank, Nación…) requiere fecha/hora del voucher.'
    }
  }
  return null
}

/** Colapsa N ítems del mismo crédito a uno (para cola «enviados»). */
export function collapsePlanillaByCredito(
  planilla: PlanillaPayloadItem[],
): PlanillaPayloadItem[] {
  const map = new Map<number, PlanillaPayloadItem>()
  for (const p of planilla) {
    const prev = map.get(p.creditoId)
    if (!prev) {
      map.set(p.creditoId, { ...p })
      continue
    }
    map.set(p.creditoId, {
      ...prev,
      montoPagar: sumMoney([prev.montoPagar, p.montoPagar]),
    })
  }
  return [...map.values()]
}
