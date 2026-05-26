import { apiDownload, apiFetch } from './client'
import type { ListaPrecioInformeParams, RptListaPrecioGeneralRow } from '../types/api'

function queryListaPrecioInforme(p: ListaPrecioInformeParams): string {
  const q = new URLSearchParams({
    indDescuento: String(p.indDescuento),
    indPuntos: String(p.indPuntos),
  })
  if (p.marcaId != null && p.marcaId > 0) {
    q.set('marcaId', String(p.marcaId))
  }
  return q.toString()
}

export function fetchListaPrecioInforme(
  params: ListaPrecioInformeParams,
): Promise<RptListaPrecioGeneralRow[]> {
  return apiFetch<RptListaPrecioGeneralRow[]>(
    `/ventas/rpt-lista-precio?${queryListaPrecioInforme(params)}`,
  )
}

export function downloadListaPrecioInformeCsv(
  params: ListaPrecioInformeParams,
): Promise<void> {
  return apiDownload(
    `/ventas/rpt-lista-precio-csv?${queryListaPrecioInforme(params)}`,
    'lista-precios-informe.csv',
  )
}

export function downloadListaPrecioInformePdf(
  params: ListaPrecioInformeParams,
): Promise<void> {
  return apiDownload(
    `/ventas/rpt-lista-precio-pdf?${queryListaPrecioInforme(params)}`,
    'lista-precios-informe.pdf',
  )
}
