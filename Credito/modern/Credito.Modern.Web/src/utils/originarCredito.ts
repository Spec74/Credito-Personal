import { crearCredito, crearSolicitudCredito } from '../api/creditoPlanes'
import { crearSolicitudPrendaria, guardarBienesPrendario } from '../api/prendario'
import type { PrendaItem } from '../api/creditoGestion'
import { codigoFormaPago } from './creditoFechas'

const IND_GASTOS_ADM = 'ADE' as const

export type OriginarCreditoInput = {
  oficinaId: number
  personaId: number
  productoId: number
  esPrendario: boolean
  solicitudCreditoId?: number | null
  monto: number
  modalidad: string
  nroCuotas: number
  interesMensual: number
  fechaPrimerPago: string
  gastosAdm: number
  observacion?: string | null
  indCentralRiesgo: boolean
  prendas?: PrendaItem[]
}

export type OriginarCreditoResult = {
  solicitudCreditoId: number
  mensaje: string
}

/** Crea la solicitud (si falta) y genera el crédito con el USP. Sin pasar por el simulador. */
export async function originarCredito(input: OriginarCreditoInput): Promise<OriginarCreditoResult> {
  let solicitudId = input.solicitudCreditoId ?? 0
  if (solicitudId < 1) {
    const creada = input.esPrendario
      ? await crearSolicitudPrendaria({
          oficinaId: input.oficinaId,
          personaId: input.personaId,
        })
      : await crearSolicitudCredito({
          oficinaId: input.oficinaId,
          personaId: input.personaId,
        })
    solicitudId = creada.solicitudCreditoId
  }

  if (input.esPrendario && input.prendas && input.prendas.length > 0) {
    await guardarBienesPrendario({
      oficinaId: input.oficinaId,
      creditoId: solicitudId,
      prendas: input.prendas,
      fechaRemate: null,
    })
  }

  const generado = await crearCredito({
    oficinaId: input.oficinaId,
    solicitudCreditoId: solicitudId,
    productoId: input.esPrendario ? 2 : input.productoId,
    tipoCuota: 'F',
    montoInicial: 0,
    montoGastosAdm: input.gastosAdm,
    indGastosAdm: IND_GASTOS_ADM,
    montoCredito: input.monto,
    modalidad: input.esPrendario ? 'M' : codigoFormaPago(input.modalidad),
    numeroCuotas: input.nroCuotas,
    interesMensual: input.interesMensual,
    fechaPrimerPago: `${input.fechaPrimerPago.slice(0, 10)}T00:00:00`,
    observacion: input.observacion ?? null,
    indCentralRiesgo: input.indCentralRiesgo,
    prenda: null,
  })

  return { solicitudCreditoId: solicitudId, mensaje: generado.mensaje ?? '' }
}

export {
  addDaysIso,
  addMonthsIso,
  fechaPrimerPagoPorModalidad,
} from './creditoFechas'
