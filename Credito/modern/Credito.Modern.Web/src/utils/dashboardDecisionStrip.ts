export type DashboardDecisionTone = 'success' | 'warning' | 'danger' | 'info'

export type DashboardDecisionItem = {
  id: string
  tone: DashboardDecisionTone
  titulo: string
  mensaje: string
  /** Ruta SPA opcional (prioridad accionable). */
  to?: string
  cta?: string
}

export type DashboardDecisionInput = {
  pctMoraCartera: number
  coberturaMes: number | null
  flujoNetoHoy: number
  cobradoHoy: number
  cobradoAyer: number
  creditosPorVencerSemana: number
  clientesMora: number
}

/**
 * Prioridades gerenciales a partir de indicadores ya calculados.
 * Orden: riesgo → liquidez → cobertura → oportunidad.
 */
export function buildDashboardDecisions(input: DashboardDecisionInput): DashboardDecisionItem[] {
  const items: DashboardDecisionItem[] = []

  if (input.pctMoraCartera >= 20) {
    items.push({
      id: 'mora-alta',
      tone: 'danger',
      titulo: 'Mora elevada',
      mensaje: `${input.pctMoraCartera.toFixed(1)}% de la cartera está en mora (${input.clientesMora} clientes). Priorice visitas y acuerdos de pago.`,
      to: '/informes/cobro-diario?soloMora=1',
      cta: 'Abrir cobro diario',
    })
  } else if (input.pctMoraCartera >= 10) {
    items.push({
      id: 'mora-media',
      tone: 'warning',
      titulo: 'Mora en vigilancia',
      mensaje: `${input.pctMoraCartera.toFixed(1)}% en mora. Contenga el deterioro antes del cierre de mes.`,
      to: '/informes/cobro-diario?soloMora=1',
      cta: 'Ver mora',
    })
  }

  if (input.flujoNetoHoy < 0) {
    items.push({
      id: 'flujo-negativo',
      tone: 'warning',
      titulo: 'Flujo neto negativo hoy',
      mensaje: 'Las salidas de caja superan las entradas. Revise egresos y transferencias antes del cierre.',
      to: '/caja/saldos',
      cta: 'Ir a saldos',
    })
  }

  if (input.coberturaMes != null && input.coberturaMes < 70) {
    items.push({
      id: 'cobertura-baja',
      tone: 'warning',
      titulo: 'Cobertura débil del mes',
      mensaje: `Cobrado vs desembolsado: ${input.coberturaMes.toFixed(0)}%. La colocación está adelantada a la recuperación.`,
      to: '/caja/diario',
      cta: 'Caja diario',
    })
  } else if (input.coberturaMes != null && input.coberturaMes >= 100) {
    items.push({
      id: 'cobertura-sana',
      tone: 'success',
      titulo: 'Cobertura saludable',
      mensaje: `La cobranza del mes cubre el desembolso (${input.coberturaMes.toFixed(0)}%). Mantenga el ritmo.`,
    })
  }

  if (input.cobradoAyer > 0 && input.cobradoHoy < input.cobradoAyer * 0.6) {
    items.push({
      id: 'cobranza-rezagada',
      tone: 'info',
      titulo: 'Cobranza bajo el ritmo de ayer',
      mensaje: 'Hoy va significativamente por debajo de ayer. Revise la cola de campo y cuotas del día.',
      to: '/caja/cobro',
      cta: 'Cobro campo',
    })
  }

  if (input.creditosPorVencerSemana >= 5) {
    items.push({
      id: 'por-vencer',
      tone: 'info',
      titulo: 'Vencimientos próximos',
      mensaje: `${input.creditosPorVencerSemana} créditos vencen en 7 días. Prevenga mora con contacto anticipado.`,
      to: '/informes/cobro-diario',
      cta: 'Agenda de cobro',
    })
  }

  if (items.length === 0) {
    items.push({
      id: 'estable',
      tone: 'success',
      titulo: 'Operación estable',
      mensaje: 'Sin alertas críticas en mora, flujo ni cobertura. Use los gráficos para afinar colocación.',
    })
  }

  return items.slice(0, 4)
}
