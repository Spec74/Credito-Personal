import { apiDownload, apiFetch } from './client'

export interface CierreGerencialPermisos {
  puedeConsultar: boolean
  puedeGestionarMetas: boolean
}

export interface AvanceMetaGerencialRow {
  orden: number | null
  usuarioId: number
  nombreUsuario: string
  nombreCompleto: string
  asesor: string
  supervisor: string
  mercado: string
  tipoCartera: string
  periodo: string
  capitalBase: number | null
  metaCapitalCierre: number | null
  capitalActual: number
  diferenciaCapital: number | null
  cumplimientoCapitalPct: number | null
  estadoCapital: string
  clientesBase: number | null
  metaClientesActivosCierre: number | null
  clientesActivosActual: number
  diferenciaClientes: number | null
  cumplimientoClientesPct: number | null
  estadoClientes: string
  vencidosBaseComparable: number | null
  metaVencidosMaximoCierre: number | null
  vencidosActual: number
  margenVencidos: number | null
  estadoVencidos: string
  clientesVencidosActual: number
  metaRecuperacionVencidosMes: number | null
  recuperacionVencidosActual: number | null
  estadoRecuperacion: string
  metaConfigurada: boolean
  fechaCalculo: string
  avanceNoOficial: boolean
  clientesNuevosMes: number
  montoCobradoMes: number
  desembolsosMes: number
  nroOperacionesMes: number
}

export interface CierreGerencialAvance {
  periodo: string
  fechaCalculo: string | null
  avanceNoOficial: boolean
  total: number
  filas: AvanceMetaGerencialRow[]
}

export interface MetaGerencialRow {
  usuarioId: number
  nombreUsuario: string
  nombreCompleto: string
  periodo: string
  capitalBase: number
  clientesActivosBase: number
  vencidosBaseComparable: number | null
  metaCapitalCierre: number | null
  metaClientesActivosCierre: number | null
  metaVencidosMaximoCierre: number | null
  metaRecuperacionVencidosMes: number | null
  configurada: boolean
  periodoCerrado: boolean
  puedeEditar: boolean
  fechaLimiteEdicion: string
  tipoCartera: string
  orden: number | null
}

export interface CierreGerencialMetas {
  periodo: string
  periodoCerrado: boolean
  puedeEditar: boolean
  fechaLimiteEdicion: string | null
  metas: MetaGerencialRow[]
}

function periodoQuery(periodo: string): string {
  // API espera DateTime; enviamos YYYY-MM-01
  const d = periodo.length >= 7 ? `${periodo.slice(0, 7)}-01` : periodo
  return encodeURIComponent(d)
}

export function fetchCierreGerencialPermisos(): Promise<CierreGerencialPermisos> {
  return apiFetch('/cierre-gerencial/permisos')
}

export function fetchCierreGerencialAvance(periodo: string): Promise<CierreGerencialAvance> {
  return apiFetch(`/cierre-gerencial/avance?periodo=${periodoQuery(periodo)}`)
}

export function fetchCierreGerencialMetas(periodo: string): Promise<CierreGerencialMetas> {
  return apiFetch(`/cierre-gerencial/metas?periodo=${periodoQuery(periodo)}`)
}

export function downloadCierreGerencialExcel(periodo: string): Promise<void> {
  const d = periodo.length >= 7 ? `${periodo.slice(0, 7)}-01` : periodo
  const stamp = d.replace(/-/g, '_')
  return apiDownload(
    `/cierre-gerencial/excel?periodo=${periodoQuery(periodo)}`,
    `cierre_gerencial_${stamp}.xlsx`,
  )
}

export function guardarCierreGerencialMetas(body: {
  periodo: string
  metas: Array<{
    usuarioId: number
    tipoCartera: string
    metaCapitalCierre: number | null
    metaClientesActivosCierre: number | null
    metaVencidosMaximoCierre: number | null
    metaRecuperacionVencidosMes: number | null
  }>
}): Promise<{ success: boolean; message: string; total: number }> {
  return apiFetch('/cierre-gerencial/metas', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
