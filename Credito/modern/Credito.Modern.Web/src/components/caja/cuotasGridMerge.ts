import { fetchCuotasPendientes } from '../../api/cajaDiario'
import { fetchEstadoPlanPago } from '../../api/creditoPlanes'
import type { CuotasPendientesRow, EstadoPlanPagoCuota } from '../../types/api'

/** Fila unificada para la grilla de cobranzas (plan completo + montos de cobro). */
export type CuotaCobranzaRow = CuotasPendientesRow & {
  estadoPlan?: string | null
  numero?: number | null
}

export function mergePlanYCobranza(
  plan: EstadoPlanPagoCuota[],
  pendientes: CuotasPendientesRow[],
): CuotaCobranzaRow[] {
  const pendByPlan = new Map<number, CuotasPendientesRow>()
  const summaryRows: CuotasPendientesRow[] = []

  for (const p of pendientes) {
    if (p.planPagoId != null && p.planPagoId > 0) {
      pendByPlan.set(p.planPagoId, p)
    } else {
      summaryRows.push(p)
    }
  }

  const fromPlan: CuotaCobranzaRow[] = plan.map((c) => {
    const pen = pendByPlan.get(c.planPagoId)
    const estado = (c.estado ?? '').trim().toUpperCase()
    const pagada = estado === 'PAG'
    return {
      planPagoId: c.planPagoId,
      glosa: pen?.glosa ?? `CUOTA ${c.numero}`,
      fechaVencimiento: pen?.fechaVencimiento ?? c.fechaVencimiento,
      amortizacion: pen?.amortizacion ?? c.amortizacion,
      interes: pen?.interes ?? c.interes,
      gastosAdm: pen?.gastosAdm ?? c.gastosAdm,
      cuota: pen?.cuota ?? c.cuota,
      diasAtrazo: pen?.diasAtrazo ?? c.diasAtrazo,
      importeMora: pen?.importeMora ?? c.importeMora,
      descuento: pen?.descuento ?? c.descuento,
      cargo: pen?.cargo ?? c.cargo,
      pagoLibre: pen?.pagoLibre ?? c.pagoLibre,
      pagoCuota: pen?.pagoCuota ?? (pagada ? c.pagoCuota : c.cuota),
      estadoPlan: c.estado,
      numero: c.numero,
    }
  })

  const summaryMapped: CuotaCobranzaRow[] = summaryRows.map((s) => ({
    ...s,
    estadoPlan: null,
    numero: null,
  }))

  return [...fromPlan, ...summaryMapped]
}

function pendientesComoFilas(
  pendientes: CuotasPendientesRow[],
): CuotaCobranzaRow[] {
  return pendientes.map((p) => ({
    ...p,
    estadoPlan: null,
    numero: null,
  }))
}

export async function loadCuotasCobranzaGrid(
  creditoId: number,
): Promise<CuotaCobranzaRow[]> {
  const pendientes = await fetchCuotasPendientes(creditoId)
  try {
    const plan = await fetchEstadoPlanPago(creditoId)
    if (plan.length > 0) {
      return mergePlanYCobranza(plan, pendientes)
    }
  } catch {
    /* plan no disponible: mostrar al menos lo que devuelve caja */
  }
  return pendientesComoFilas(pendientes)
}
