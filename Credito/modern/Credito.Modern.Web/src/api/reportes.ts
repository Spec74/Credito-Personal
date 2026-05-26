import { apiFetch } from './client'

export interface ReporteExportPolitica {
  fase: string
  csvTabularCompleto: boolean
  catalogoModerno: string
  politicaEndpoint: string
  informesTextoRdlcSinCsv: Array<{
    id: string
    jsonEndpoint: string
    proc: string
    nota: string
  }>
  informesPdfPilotos: Array<{
    id: string
    jsonEndpoint: string
    csvEndpoint: string
    pdfEndpoint: string
    nota: string
  }>
  rdlcMotor: {
    estado: string
    legacyController: string
    catalogoEndpoint: string
    proximaRebanada: string
  }
}

export function fetchPoliticaExportacion(): Promise<ReporteExportPolitica> {
  return apiFetch<ReporteExportPolitica>('/reportes/politica-exportacion')
}

export interface ReporteCoberturaItem {
  catalogoId: string
  nombre: string
  area: string
  accionMvc: string
  nivelCobertura: string
  jsonEndpoint: string | null
  csvEndpoint: string | null
  pdfEndpoint: string | null
  nota: string
}

export interface ReporteCatalogoCobertura {
  totalCatalogo: number
  completoDatosJsonCsvPdf: number
  jsonSinExportTabular: number
  parcial: number
  soloMvc: number
  vistaIndice: number
  items: ReporteCoberturaItem[]
  informesAdicionalesApi: ReporteCoberturaItem[]
  informesTextoRdlc: ReporteCoberturaItem[]
}

export function fetchCatalogoCobertura(): Promise<ReporteCatalogoCobertura> {
  return apiFetch<ReporteCatalogoCobertura>('/reportes/catalogo-cobertura')
}

