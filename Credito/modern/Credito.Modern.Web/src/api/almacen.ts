import { apiDownload, apiFetch, apiOpenInTab } from './client'
import type { GenerarKardexParams, GenerarKardexRow } from '../types/api'

export interface ReporteStockRow {
  nro: number | null
  tipoArticulo: string | null
  articuloId: number
  articulo: string | null
  stock: number
  series: string | null
}

export interface RptStockAnuladoRow {
  movimientoId: number
  movimiento: string | null
  observacion: string | null
  fecha: string
  cantidad: number
  detalle: string | null
}

export function fetchReporteStock(oficinaId: number): Promise<ReporteStockRow[]> {
  return apiFetch<ReporteStockRow[]>(
    `/almacen/reporte-stock?oficinaId=${oficinaId}`,
  )
}

export function downloadReporteStockCsv(oficinaId: number): Promise<void> {
  return apiDownload(
    `/almacen/reporte-stock-csv?oficinaId=${oficinaId}`,
    'reporte-stock.csv',
  )
}

export function downloadReporteStockPdf(oficinaId: number): Promise<void> {
  return apiDownload(
    `/almacen/reporte-stock-pdf?oficinaId=${oficinaId}`,
    'reporte-stock.pdf',
  )
}

export function openReporteStockPdfInTab(oficinaId: number): Promise<void> {
  return apiOpenInTab(`/almacen/reporte-stock-pdf?oficinaId=${oficinaId}`)
}

export function openReporteStockCsvInTab(oficinaId: number): Promise<void> {
  return apiOpenInTab(`/almacen/reporte-stock-csv?oficinaId=${oficinaId}`)
}

export function fetchStockAnulados(): Promise<RptStockAnuladoRow[]> {
  return apiFetch<RptStockAnuladoRow[]>('/almacen/rpt-stock-anulados')
}

export function downloadStockAnuladosCsv(): Promise<void> {
  return apiDownload('/almacen/rpt-stock-anulados-csv', 'stock-anulados.csv')
}

export function downloadStockAnuladosPdf(): Promise<void> {
  return apiDownload('/almacen/rpt-stock-anulados-pdf', 'stock-anulados.pdf')
}

export function openStockAnuladosPdfInTab(): Promise<void> {
  return apiOpenInTab('/almacen/rpt-stock-anulados-pdf')
}

export function openStockAnuladosCsvInTab(): Promise<void> {
  return apiOpenInTab('/almacen/rpt-stock-anulados-csv')
}

function queryGenerarKardex(p: GenerarKardexParams): string {
  return new URLSearchParams({
    oficinaId: String(p.oficinaId),
    articuloId: String(p.articuloId),
    almacenId: String(p.almacenId),
  }).toString()
}

export function fetchGenerarKardex(
  params: GenerarKardexParams,
): Promise<GenerarKardexRow[]> {
  return apiFetch<GenerarKardexRow[]>(`/almacen/generar-kardex?${queryGenerarKardex(params)}`)
}

export function downloadGenerarKardexCsv(params: GenerarKardexParams): Promise<void> {
  return apiDownload(
    `/almacen/generar-kardex-csv?${queryGenerarKardex(params)}`,
    'kardex.csv',
  )
}

export function downloadGenerarKardexPdf(params: GenerarKardexParams): Promise<void> {
  return apiDownload(
    `/almacen/generar-kardex-pdf?${queryGenerarKardex(params)}`,
    'kardex.pdf',
  )
}

