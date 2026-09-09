import type { PrendarioCategoria } from '../api/prendario'
import { getCreditoEstadoMeta } from './creditoEstados'

const CARTERA: Record<Exclude<PrendarioCategoria, 'Otro'>, { color: string; label: string; hint: string }> = {
  Vigente: { color: 'green', label: 'Vigente', hint: 'Desembolsado y al día.' },
  PorVencer: { color: 'gold', label: 'Por vencer', hint: 'Desembolsado; vence en 3 días o menos.' },
  Vencido: { color: 'red', label: 'Vencido', hint: 'Desembolsado y vencido, aún no rematado.' },
  Rematado: { color: 'magenta', label: 'Rematado', hint: 'Fecha de remate ya venció.' },
  SinBienes: {
    color: 'default',
    label: 'Sin bienes',
    hint: 'Producto prendario sin indicador de bienes en custodia.',
  },
}

export type PrendarioSituacionUi = {
  label: string
  color: string
  hint: string
}

/**
 * Situación del listado: la cartera (vigente/vencido/…) solo aplica a desembolsados.
 * Antes del desembolso se muestra el estado del ciclo (solicitud, pendiente, aprobado).
 */
export function situacionPrendario(row: {
  categoria: PrendarioCategoria
  estado: string
}): PrendarioSituacionUi {
  if (row.categoria !== 'Otro') {
    return CARTERA[row.categoria]
  }

  const meta = getCreditoEstadoMeta(row.estado)
  if (meta) {
    return {
      label: meta.label,
      color: meta.color,
      hint: meta.descripcion,
    }
  }

  return {
    label: row.estado.trim() || 'En trámite',
    color: 'blue',
    hint: 'Aún no desembolsado; las alertas de vencimiento no aplican.',
  }
}
