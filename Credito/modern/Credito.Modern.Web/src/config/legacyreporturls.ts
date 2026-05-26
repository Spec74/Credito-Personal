import {
  openCajaDiarioInformePdfInTab,
  openCentralRiesgoTxtInTab,
  openClientesBloqueadosPdfInTab,
  openClientesInactivosPdfGestorInTab,
  openClientesInactivosPdfInTab,
  openClientesNuevosMesCsvInTab,
  openClientesNuevosMesPdfGestorInTab,
  openClientesNuevosMesPdfInTab,
  openClientesTopeCreditoPdfInTab,
  openCobroDiarioCsvInTab,
  openCobroDiarioPdfInTab,
  openComprobantesCajaChicaCsvInTab,
  openComprobantesCajaChicaPdfInTab,
  openCreditoAprobacionCsvInTab,
  openCreditoAprobacionPdfInTab,
  openCreditoCondonadoCsvInTab,
  openCreditoCondonadoPdfInTab,
  openCreditoMorosidadPdfInTab,
  openCreditoObservadoCsvInTab,
  openCreditoObservadoPdfInTab,
  openCreditoRentabilidadCsvInTab,
  openCreditoRentabilidadPdfInTab,
  openCreditosActivosCsvInTab,
  openCreditosCierresCsvInTab,
  openCreditosCierresPdfInTab,
  openCreditosMorososPagadosCsvInTab,
  openCreditosMorososPagadosPdfInTab,
  openMovimientoCajaAnuladoPdfInTab,
  openReporteCreditoPdfInTab,
  openSaldoCarteraCajaDiarioPdfInTab,
} from '../api/creditoPlanes'
import { legacyDateToApi } from './legacyReportDate'

/** Parámetros para abrir el mismo informe que MVC (ReportViewer + .rdlc). */
export type LegacyReportFormat = 'PDF' | 'Excel'

export type CreditoVencidoLegacyFranja = 'todos' | 'menor60' | 'mayor60' | 'irrecuperable'

export type LegacyReportKey =
  | 'cobro-diario'
  | 'clientes-inactivos'
  | 'morosidad-gestor'

export interface LegacyReportSession {
  usuarioId: number
  oficinaId: number
}

export interface ClientesInactivosLegacyParams {
  oficinaId?: number
  usuarioId?: number
  fechaIni: string
  fechaFin: string
}

const LEGACY_ORIGIN =
  import.meta.env.VITE_LEGACY_ORIGIN ?? 'http://localhost:9080'

/** Navegación a pantallas MVC (no export PDF). */
function openUrl(path: string): void {
  const url = path.startsWith('http')
    ? path
    : `${LEGACY_ORIGIN}${path.startsWith('/') ? '' : '/'}${path}`
  const opened = window.open(url, '_blank', 'noopener,noreferrer')
  if (!opened) {
    throw new Error('Permita ventanas emergentes.')
  }
}

function gestorApi(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
): { oficinaId: number; usuarioId?: number } {
  return {
    oficinaId: oficinaId ?? 0,
    ...(usuarioId != null && usuarioId > 0 ? { usuarioId } : {}),
  }
}

function rangoApi(
  params: InformeRangoGestorLegacyParams,
): { oficinaId: number; usuarioId?: number; fechaIni: string; fechaFin: string } {
  return {
    oficinaId: params.oficinaId,
    usuarioId: params.usuarioId,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  }
}

/** Cobro diario (export API en pestaña). */
export function openLegacyCobroDiario(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
  formato: LegacyReportFormat = 'Excel',
): void {
  const p = gestorApi(oficinaId, usuarioId)
  if (formato === 'Excel') {
    void openCobroDiarioCsvInTab(p)
  } else {
    void openCobroDiarioPdfInTab(p)
  }
}

/** Morosidad gestor = cobro diario con filtro solo mora. */
export function openLegacyMorosidadGestor(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
  formato: LegacyReportFormat = 'PDF',
): void {
  const p = { ...gestorApi(oficinaId, usuarioId), soloMora: true as const }
  if (formato === 'Excel') {
    void openCobroDiarioCsvInTab(p)
  } else {
    void openCobroDiarioPdfInTab(p)
  }
}

/** Clientes inactivos con rango (equivale a ReporteClientesInactivosPagados). */
export function openLegacyClientesInactivos(
  params: ClientesInactivosLegacyParams,
  _formato: LegacyReportFormat = 'PDF',
): void {
  void openClientesInactivosPdfInTab({
    oficinaId: params.oficinaId ?? 0,
    usuarioId: params.usuarioId,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  })
}

/** Créditos vencidos por franja (bóveda / ReporteCreditoVencido). */
export function openLegacyClientesBloqueados(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
  _formato: LegacyReportFormat = 'PDF',
): void {
  void openClientesBloqueadosPdfInTab(gestorApi(oficinaId, usuarioId))
}

export function openLegacyClientesTopeCredito(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
  _formato: LegacyReportFormat = 'PDF',
): void {
  void openClientesTopeCreditoPdfInTab(gestorApi(oficinaId, usuarioId))
}

/** Créditos con observación (PEN/DES). */
export function openLegacyCreditoObservado(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
  formato: LegacyReportFormat = 'PDF',
): void {
  const p = gestorApi(oficinaId, usuarioId)
  if (formato === 'Excel') {
    void openCreditoObservadoCsvInTab(p)
  } else {
    void openCreditoObservadoPdfInTab(p)
  }
}

/** Clientes nuevos del mes con rango de fechas. */
export function openLegacyClientesNuevosMes(
  params: ClientesInactivosLegacyParams,
  formato: LegacyReportFormat = 'PDF',
): void {
  const p = {
    oficinaId: params.oficinaId ?? 0,
    usuarioId: params.usuarioId,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  }
  if (formato === 'Excel') {
    void openClientesNuevosMesCsvInTab(p)
  } else {
    void openClientesNuevosMesPdfInTab(p)
  }
}

/** Créditos condonados en rango (PAG + IndCondonacion). */
export function openLegacyCreditoCondonado(
  params: ClientesInactivosLegacyParams,
  formato: LegacyReportFormat = 'PDF',
): void {
  const p = {
    oficinaId: params.oficinaId ?? 0,
    usuarioId: params.usuarioId ?? 0,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  }
  if (formato === 'Excel') {
    void openCreditoCondonadoCsvInTab(p)
  } else {
    void openCreditoCondonadoPdfInTab(p)
  }
}

/** Stock general por oficina (usp_ReporteStock). */
export function openLegacyReporteStock(
  oficinaId: number,
  formato: LegacyReportFormat = 'PDF',
): void {
  const q = new URLSearchParams({
    pOficinaid: String(oficinaId),
    pTipoReporte: formato,
  })
  openUrl(`/Reporte/ReporteStock?${q}`)
}

/** Productos / movimientos anulados (sin filtro oficina en MVC). */
export function openLegacyStockAnulados(formato: LegacyReportFormat = 'PDF'): void {
  openUrl(`/Reporte/ReporteStockAnulados?pTipoReporte=${formato}`)
}

/** Pantalla operativa de bóveda (ingresos, egresos, cierre). */
export function openLegacyBovedaIndex(): void {
  openUrl('/Boveda/Index')
}

/** Movimiento de bóveda RDLC (mismo dataset que usp_RptMovimientoBoveda). */
export function openLegacyMovimientoBoveda(
  bovedaId: number,
  formato: LegacyReportFormat = 'PDF',
): void {
  const q = new URLSearchParams({
    pBovedaId: String(bovedaId),
    pTipo: formato,
  })
  openUrl(`/Reporte/ReporteMovimientoBoveda?${q}`)
}

/** Caja chica del usuario (sesión y movimientos). */
export function openLegacyCajaChicaIndex(): void {
  openUrl('/CajaChica/Index')
}

/** Cajas asignadas de la oficina (RDLC; MVC fija PDF en la acción). */
export function openLegacyCajasAsignadas(): void {
  openUrl('/Reporte/ReporteCajasAsignadas')
}

/** Saldo de una sesión de caja diario (paridad ImprimirSaldo en Saldos/Index). */
export function openLegacyReporteSaldoCaja(cajaDiarioId: number): void {
  openUrl(`/Reporte/ReporteSaldoCaja?pCajaDiarioId=${cajaDiarioId}`)
}

export interface CajaDiarioInformeLegacyParams {
  oficinaId: number
  usuarioId?: number
  fechaIni: string
  fechaFin: string
  formato?: LegacyReportFormat
}

export function openLegacyCajaDiarioInforme(
  params: CajaDiarioInformeLegacyParams,
): void {
  void openCajaDiarioInformePdfInTab({
    oficinaId: params.oficinaId,
    usuarioId: params.usuarioId,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  })
}

export interface CreditoRentabilidadLegacyParams {
  oficinaId: number
  estadoCredito: string
  fechaIni: string
  fechaFin: string
  indTodos?: boolean
  formato?: LegacyReportFormat
}

export function openLegacyCreditoRentabilidad(
  params: CreditoRentabilidadLegacyParams,
): void {
  const p = {
    oficinaId: params.oficinaId,
    estadoCredito: params.estadoCredito,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  }
  if (params.formato === 'Excel') {
    void openCreditoRentabilidadCsvInTab(p)
  } else {
    void openCreditoRentabilidadPdfInTab(p)
  }
}

export interface InformeRangoGestorLegacyParams {
  oficinaId: number
  usuarioId?: number
  fechaIni: string
  fechaFin: string
  formato?: LegacyReportFormat
}

export function openLegacyCreditoAprobacion(params: {
  oficinaId: number
  usuarioId?: number
  fecha: string
  formato?: LegacyReportFormat
}): void {
  const p = {
    oficinaId: params.oficinaId,
    usuarioId: params.usuarioId,
    fechaAprobacion: legacyDateToApi(params.fecha),
  }
  if (params.formato === 'Excel') {
    void openCreditoAprobacionCsvInTab(p)
  } else {
    void openCreditoAprobacionPdfInTab(p)
  }
}

export function openLegacyCreditosActivos(
  params: InformeRangoGestorLegacyParams,
): void {
  void openCreditosActivosCsvInTab(rangoApi(params))
}

export function openLegacyCreditosCierres(
  params: InformeRangoGestorLegacyParams,
): void {
  const p = rangoApi(params)
  if (params.formato === 'Excel') {
    void openCreditosCierresCsvInTab(p)
  } else {
    void openCreditosCierresPdfInTab(p)
  }
}

export function openLegacyCreditosMorososPagados(
  params: InformeRangoGestorLegacyParams,
): void {
  const p = rangoApi(params)
  if (params.formato === 'Excel') {
    void openCreditosMorososPagadosCsvInTab(p)
  } else {
    void openCreditosMorososPagadosPdfInTab(p)
  }
}

export function openLegacyComprobantesCajaAnulados(params: {
  oficinaId: number
  fechaIni: string
  fechaFin: string
  formato?: LegacyReportFormat
}): void {
  void openMovimientoCajaAnuladoPdfInTab({
    oficinaId: params.oficinaId,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  })
}

export function openLegacyComprobantesCajaChica(params: {
  fechaIni: string
  fechaFin: string
  formato?: LegacyReportFormat
}): void {
  const p = {
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  }
  if (params.formato === 'Excel') {
    void openComprobantesCajaChicaCsvInTab(p)
  } else {
    void openComprobantesCajaChicaPdfInTab(p)
  }
}

export function openLegacyReporteCredito(params: {
  oficinaId: number
  gestorId?: number
  estadoCredito: string
  fechaIni: string
  fechaFin: string
}): void {
  void openReporteCreditoPdfInTab({
    oficinaId: params.oficinaId,
    gestorId: params.gestorId,
    estadoCredito: params.estadoCredito,
    fechaIni: legacyDateToApi(params.fechaIni),
    fechaFin: legacyDateToApi(params.fechaFin),
  })
}

export function openLegacySaldoCarteraCajaDiario(params: {
  oficinaId: number
  usuarioId?: number
  anioIni: number
  mesIni: number
  anioFin: number
  mesFin: number
}): void {
  void openSaldoCarteraCajaDiarioPdfInTab({
    oficinaId: params.oficinaId,
    usuarioId: params.usuarioId,
    anioIni: params.anioIni,
    mesIni: params.mesIni,
    anioFin: params.anioFin,
    mesFin: params.mesFin,
  })
}

/** Pantalla Saldos (cierres masivos, grillas). */
export function openLegacySaldosIndex(): void {
  openUrl('/Saldos')
}

export function openLegacyListaPrecioInforme(params: {
  marcaId?: number
  indDescuento: boolean
  indPuntos: boolean
  formato?: LegacyReportFormat
}): void {
  const q = new URLSearchParams({
    pIndDescuento: String(params.indDescuento),
    pIndPuntos: String(params.indPuntos),
    pTipo: params.formato ?? 'PDF',
  })
  if (params.marcaId != null) {
    q.set('pMarcaId', String(params.marcaId))
  }
  openUrl(`/Reporte/ReporteListaPrecio?${q}`)
}

export function openLegacyKardex(params: {
  articuloId: number
  almacenId: number
}): void {
  const q = new URLSearchParams({
    pArticuloId: String(params.articuloId),
    pAlmacenId: String(params.almacenId),
  })
  openUrl(`/Reporte/ReporteKardex?${q}`)
}

export function openLegacyEntradaAlmacen(): void {
  openUrl('/Entrada')
}

export function openLegacySalidaAlmacen(): void {
  openUrl('/Salida')
}

export function openLegacyTransferenciaAlmacen(): void {
  openUrl('/Transferencia')
}

export function openLegacyVentaRapida(): void {
  openUrl('/VentaRapida')
}

export function openLegacyOrdenVentaIndex(ordenVentaId?: number, personaId?: number): void {
  const q = new URLSearchParams()
  if (ordenVentaId != null && ordenVentaId > 0) {
    q.set('id', String(ordenVentaId))
  }
  if (personaId != null && personaId > 0) {
    q.set('pPersonaId', String(personaId))
  }
  const suffix = q.toString() ? `?${q}` : ''
  openUrl(`/OrdenVenta/Index${suffix}`)
}

export function openLegacyOrdenVentaGrilla(): void {
  openUrl('/OrdenVenta/OrdenesVenta')
}

/** Archivo TXT DM007898 (mismo proc que la API central-riesgo-generar). */
export function openLegacyCentralRiesgoTxt(params: {
  oficinaId: number
  anio: number
  mes: number
}): void {
  void openCentralRiesgoTxtInTab(params)
}

export function openLegacyCanjearPuntos(): void {
  openUrl('/CanjearPuntos')
}

/** Maestro CRUD de cajas (CajaController.Index). */
export function openLegacyCajaMaestro(): void {
  openUrl('/Caja')
}

export function openLegacyMarcaMaestro(): void {
  openUrl('/Marca')
}

export function openLegacyModeloMaestro(): void {
  openUrl('/Modelo')
}

export function openLegacyTipoArticuloMaestro(): void {
  openUrl('/TipoArticulo')
}

export function openLegacyArticuloMaestro(): void {
  openUrl('/Articulo')
}

export function openLegacyOficinaMaestro(): void {
  openUrl('/Oficina')
}

export function openLegacyUsuarioMaestro(): void {
  openUrl('/Usuario')
}

export function openLegacyRolMaestro(): void {
  openUrl('/Rol')
}

export function openLegacyComision(): void {
  openUrl('/Comision')
}

export function openLegacyTareasCredito(): void {
  openUrl('/Credito/Tareas')
}

export function openLegacyReporteCreditoIndex(): void {
  openUrl('/Reporte/Credito')
}

export function openLegacyReporteVentaIndex(): void {
  openUrl('/Reporte/Venta')
}

export function openLegacyRentabilidadVenta(params: {
  oficinaId: number
  fechaIni: string
  fechaFin: string
  indContado: boolean
  indCredito: boolean
}): void {
  const q = new URLSearchParams({
    pFechaIni: params.fechaIni,
    pFechaFin: params.fechaFin,
    indContado: String(params.indContado),
    indCredito: String(params.indCredito),
    pOficinaId: String(params.oficinaId),
  })
  openUrl(`/Reporte/ReporteAvanceVenta?${q}`)
}

/** Pantalla operativa de bóveda (ingresos, egresos, cierre). */

/** Movimiento de bóveda RDLC (mismo dataset que usp_RptMovimientoBoveda). */

/** Caja chica del usuario (sesión y movimientos). */

/** Cajas asignadas de la oficina (RDLC; MVC fija PDF en la acción). */

/** Saldo de una sesión de caja diario (paridad ImprimirSaldo en Saldos/Index). */

export interface CreditoMorosidadLegacyParams {
  oficinaId: number
  hastaFecha: string
  diasAtrazoIni: number
  diasAtrazoFin: number
}

/** Morosidad por oficina y rango de días de atraso (usp_RptCreditoMorosidad). */
export function openLegacyCreditoMorosidad(
  params: CreditoMorosidadLegacyParams,
): void {
  void openCreditoMorosidadPdfInTab({
    oficinaId: params.oficinaId ?? 0,
    hastaFecha: legacyDateToApi(params.hastaFecha),
    diasAtrazoIni: params.diasAtrazoIni,
    diasAtrazoFin: params.diasAtrazoFin,
  })
}

/** Clientes nuevos del mes sin rango (mes actual en servidor). */
export function openLegacyClientesNuevosMesGestor(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
): void {
  void openClientesNuevosMesPdfGestorInTab({
    oficinaId: oficinaId ?? 0,
    usuarioId,
  })
}

/** Clientes inactivos sin rango de fechas (gestor). */
export function openLegacyClientesInactivosGestor(
  oficinaId: number | undefined,
  usuarioId: number | undefined,
): void {
  void openClientesInactivosPdfGestorInTab(gestorApi(oficinaId, usuarioId))
}

/** Excel cobranza pagos (misma exportación que CobranzaPagos.cshtml). */
export function openLegacyCobranzaPagosExcel(
  oficinaId: number | undefined,
  gestorId: number | undefined,
): void {
  const q = new URLSearchParams()
  if (gestorId != null && gestorId > 0) q.set('pGestorid', String(gestorId))
  if (oficinaId != null && oficinaId > 0) q.set('pOficinaid', String(oficinaId))
  openUrl(`/Reporte/ExportarCobranzaPagosExcel?${q}`)
}

/** Créditos con observación (PEN/DES). */

/** Morosidad por oficina y rango de días de atraso (usp_RptCreditoMorosidad). */

/** Clientes nuevos del mes sin rango (mes actual en servidor). */

/** Clientes inactivos sin rango de fechas (gestor). */

/** Excel cobranza pagos (misma exportación que CobranzaPagos.cshtml). */

export function openLegacyCreditoVencido(
  franja: CreditoVencidoLegacyFranja,
  formato: LegacyReportFormat = 'Excel',
): void {
  const q = new URLSearchParams({ pTipoReporte: formato })
  const nullStr = 'null'
  switch (franja) {
    case 'menor60':
      q.set('pVencidoMenor60', 'S')
      q.set('pVencidoMayor60', nullStr)
      q.set('pVencidoIrrecuperable', nullStr)
      break
    case 'mayor60':
      q.set('pVencidoMenor60', nullStr)
      q.set('pVencidoMayor60', 'S')
      q.set('pVencidoIrrecuperable', nullStr)
      break
    case 'irrecuperable':
      q.set('pVencidoMenor60', nullStr)
      q.set('pVencidoMayor60', nullStr)
      q.set('pVencidoIrrecuperable', 'S')
      break
    default:
      q.set('pVencidoMenor60', nullStr)
      q.set('pVencidoMayor60', nullStr)
      q.set('pVencidoIrrecuperable', nullStr)
      break
  }
  openUrl(`/Reporte/ReporteCreditoVencido?${q}`)
}

export function openLegacyReport(
  key: LegacyReportKey,
  session: LegacyReportSession,
  formato: LegacyReportFormat = 'Excel',
  extra?: Partial<ClientesInactivosLegacyParams>,
): void {
  switch (key) {
    case 'cobro-diario':
      openLegacyCobroDiario(session.oficinaId, session.usuarioId, formato)
      break
    case 'morosidad-gestor':
      openLegacyMorosidadGestor(session.oficinaId, session.usuarioId, formato)
      break
    case 'clientes-inactivos':
      if (!extra?.fechaIni || !extra.fechaFin || !extra.usuarioId || !extra.oficinaId) {
        throw new Error('fechaIni y fechaFin son obligatorias para clientes inactivos legacy')
      }
      openLegacyClientesInactivos(
        {
          usuarioId: extra.usuarioId,
          oficinaId: extra.oficinaId,
          fechaIni: extra.fechaIni,
          fechaFin: extra.fechaFin,
        },
        formato,
      )
      break
    default:
      break
  }
}
