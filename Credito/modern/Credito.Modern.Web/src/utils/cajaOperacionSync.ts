import type { QueryClient } from '@tanstack/react-query'
import {
  loadCobroBloqueDraft,
  saveCobroBloqueDraft,
} from './cobroBloqueDraft'
import {
  clearCobroBloquePendingProcess,
  enqueueCobroBloqueProcess,
  loadCobroBloquePendingProcess,
} from './cobroBloqueOffline'
import { markCobroEnviados, type CobroEnviadoItem } from './cobroCola'

export type CajaOperacionCtx = {
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
}

/** Invalida sesión Diario + cartera Cobro (misma operación de caja). */
export function invalidateCajaOperacion(
  queryClient: QueryClient,
  ctx: Pick<CajaOperacionCtx, 'oficinaId' | 'cajaDiarioId'>,
): void {
  void queryClient.invalidateQueries({ queryKey: ['caja-diario-sesion'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-movimientos'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-saldo'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-resumen-ingreso'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-cxc'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-cxc-inline'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-desembolsos'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-impagos-validacion'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-creditos-gestor-des'] })
  if (ctx.oficinaId > 0) {
    void queryClient.invalidateQueries({
      queryKey: ['caja-diario-sesion', ctx.oficinaId],
    })
  }
  if (ctx.cajaDiarioId > 0) {
    void queryClient.invalidateQueries({
      queryKey: ['caja-movimientos', ctx.cajaDiarioId],
    })
  }
}

/** Quita créditos tipados del borrador local de Cobro. */
export function clearCobroDraftCreditos(
  args: CajaOperacionCtx,
  creditoIds: number[],
): void {
  if (args.usuarioId < 1 || args.cajaDiarioId < 1 || creditoIds.length === 0) return
  const { draft } = loadCobroBloqueDraft({
    usuarioId: args.usuarioId,
    oficinaId: args.oficinaId,
    cajaDiarioId: args.cajaDiarioId,
  })
  if (!draft) return
  const ids = new Set(creditoIds)
  let changed = false
  const nextEdits = { ...draft.edits }
  for (const id of ids) {
    const key = String(id)
    if (key in nextEdits) {
      delete nextEdits[key]
      changed = true
    }
  }
  if (!changed) return
  saveCobroBloqueDraft({
    ...draft,
    edits: nextEdits,
    updatedAt: new Date().toISOString(),
  })
}

/** Quita créditos de la cola offline pendiente (evita doble cobro). */
export function removeCreditosFromPendingProcess(
  args: Pick<CajaOperacionCtx, 'usuarioId' | 'cajaDiarioId'>,
  creditoIds: number[],
): void {
  if (args.usuarioId < 1 || args.cajaDiarioId < 1 || creditoIds.length === 0) return
  const pending = loadCobroBloquePendingProcess(args)
  if (!pending) return
  const ids = new Set(creditoIds)
  const resto = pending.planilla.filter((p) => !ids.has(p.creditoId))
  if (resto.length === pending.planilla.length) return
  if (resto.length === 0) {
    clearCobroBloquePendingProcess(args)
    return
  }
  enqueueCobroBloqueProcess({
    ...pending,
    conCobro: resto.length,
    total: resto.reduce((a, x) => a + x.montoPagar, 0),
    planilla: resto,
  })
}

/**
 * Tras un cobro en Caja Diario: refleja en Enviados de Cobro,
 * limpia tipados/offline y deja la cartera lista para refetch.
 */
export function syncDiarioPagoHaciaCobro(
  args: CajaOperacionCtx & {
    creditoId: number
    personaNombre?: string | null
    montoPagar: number
    tipoPagoId?: number
    movimientoCajaId?: number | null
  },
): void {
  if (args.usuarioId < 1 || args.cajaDiarioId < 1 || args.creditoId < 1) return
  if (!(args.montoPagar > 0)) return

  const item: Omit<CobroEnviadoItem, 'enviadoAt'> = {
    creditoId: args.creditoId,
    personaNombre: (args.personaNombre ?? '').trim() || `Crédito ${args.creditoId}`,
    montoPagar: args.montoPagar,
    tipoPagoId: args.tipoPagoId && args.tipoPagoId > 0 ? args.tipoPagoId : 1,
    movimientoCajaId: args.movimientoCajaId ?? null,
    origen: 'diario',
  }
  markCobroEnviados(
    {
      usuarioId: args.usuarioId,
      oficinaId: args.oficinaId,
      cajaDiarioId: args.cajaDiarioId,
    },
    [item],
  )
  clearCobroDraftCreditos(args, [args.creditoId])
  removeCreditosFromPendingProcess(args, [args.creditoId])
}
