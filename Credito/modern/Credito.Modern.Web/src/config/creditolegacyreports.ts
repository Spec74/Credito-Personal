const LEGACY_ORIGIN =
  import.meta.env.VITE_LEGACY_ORIGIN ?? 'http://localhost:9080'

function openLegacyPath(path: string): void {
  const url = `${LEGACY_ORIGIN}${path.startsWith('/') ? path : `/${path}`}`
  const opened = window.open(url, '_blank', 'noopener,noreferrer')
  if (!opened) {
    throw new Error('Permita ventanas emergentes para abrir el informe legacy.')
  }
}

/** Paridad `Reporte/ReporteEstadoCredito` (RDLC). */
export function openLegacyReporteEstadoCredito(creditoId: number): void {
  openLegacyPath(`/Reporte/ReporteEstadoCredito?pCreditoId=${creditoId}`)
}

/** Paridad `Reporte/ReportePlanPagos` (rptSimuladorPlanPago.rdlc). */
export function openLegacyReportePlanPagos(creditoId: number): void {
  openLegacyPath(`/Reporte/ReportePlanPagos?pCreditoId=${creditoId}`)
}

/** Paridad `Reporte/ReporteCreditoMovimiento` (RDLC movimientos del crédito). */
export function openLegacyReporteCreditoMovimiento(creditoId: number): void {
  openLegacyPath(`/Reporte/ReporteCreditoMovimiento?pCreditoId=${creditoId}`)
}

/** Paridad `Reporte/ReporteCliente` desde cabecera MVC. */
export function openLegacyReporteCliente(personaId: number): void {
  openLegacyPath(`/Reporte/ReporteCliente?pPersonaId=${personaId}`)
}
