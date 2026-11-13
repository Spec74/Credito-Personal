import { apiDownload, apiFetch, apiOpenInTab } from './client'

export interface CentralRiesgoGenerarRow {
  anio: number | null
  mes: number | null
  creditoId: number
  periodo: string | null
  entidad: string | null
  tipoDoc: number
  numDoc: string | null
  razonSocial: string | null
  apePat: string | null
  apeMat: string | null
  nombres: string | null
  tipoPersona: number
  modalidadCredito: number
  deudaMenor30: string | null
  deudaMayor30: string | null
  calificacion: number
  diasAtrazo: number
  direccion: string | null
  celular: string | null
}

export interface CentralRiesgoParams {
  oficinaId: number
  anio: number
  mes: number
  /** Recalcula el snapshot de la sede/periodo (borra cache previo de esa oficina). */
  regenerar?: boolean
}

function centralRiesgoFileBase(p: CentralRiesgoParams): string {
  const mes = String(p.mes).padStart(2, '0')
  return `DM007898_O${p.oficinaId}_${p.anio}${mes}`
}

function queryCentralRiesgo(p: CentralRiesgoParams): string {
  const q = new URLSearchParams({
    oficinaId: String(p.oficinaId),
    anio: String(p.anio),
    mes: String(p.mes),
  })
  if (p.regenerar) {
    q.set('regenerar', 'true')
  }
  return q.toString()
}

export function fetchCentralRiesgoGenerar(
  params: CentralRiesgoParams,
): Promise<CentralRiesgoGenerarRow[]> {
  return apiFetch<CentralRiesgoGenerarRow[]>(
    `/credito/central-riesgo-generar?${queryCentralRiesgo(params)}`,
  )
}

export function downloadCentralRiesgoGenerarCsv(
  params: CentralRiesgoParams,
): Promise<void> {
  return apiDownload(
    `/credito/central-riesgo-generar-csv?${queryCentralRiesgo(params)}`,
    `${centralRiesgoFileBase(params)}.csv`,
  )
}

export function downloadCentralRiesgoGenerarPdf(
  params: CentralRiesgoParams,
): Promise<void> {
  return apiDownload(
    `/credito/central-riesgo-generar-pdf?${queryCentralRiesgo(params)}`,
    `${centralRiesgoFileBase(params)}.pdf`,
  )
}

export function downloadCentralRiesgoGenerarTxt(
  params: CentralRiesgoParams,
): Promise<void> {
  return apiDownload(
    `/credito/central-riesgo-generar-txt?${queryCentralRiesgo(params)}`,
    `${centralRiesgoFileBase(params)}.txt`,
  )
}

export function openCentralRiesgoTxtInTab(params: CentralRiesgoParams): Promise<void> {
  return apiOpenInTab(`/credito/central-riesgo-generar-txt?${queryCentralRiesgo(params)}`)
}
