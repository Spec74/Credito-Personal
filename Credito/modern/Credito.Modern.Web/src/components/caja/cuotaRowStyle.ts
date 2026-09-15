import type { CuotasPendientesRow } from '../../types/api'
import type { CuotaCobranzaRow } from './cuotasGridMerge'

/** Variantes de fila (paridad CSS legacy: field-pagado, field-pendiente, field-error). */
export type CuotaRowVariant =
  | 'pagada'
  | 'pendiente'
  | 'mora'
  | 'creada'
  | 'resumen'

export type CuotaVistaFiltro = 'todas' | 'cobrables' | 'mora'

const norm = (s: string | null | undefined) => (s ?? '').trim().toUpperCase()

export function isCuotaFilaResumen(row: CuotasPendientesRow): boolean {
  const planId = row.planPagoId
  const glosa = norm(row.glosa)
  if (planId != null && planId <= 0 && planId !== -1) {
    return true
  }
  return (
    glosa.includes('TOTAL') ||
    glosa.includes('PENDIENTE:') ||
    glosa.startsWith('TOTAL ')
  )
}

export function isCuotaFilaPagada(
  row: CuotasPendientesRow | CuotaCobranzaRow,
): boolean {
  const estadoPlan = 'estadoPlan' in row ? row.estadoPlan : null
  if ((estadoPlan ?? '').trim().toUpperCase() === 'PAG') {
    return true
  }
  const planId = row.planPagoId
  const glosa = norm(row.glosa)
  if (planId === -1 || glosa.includes('PAGADO') || glosa.includes('PAGADA')) {
    return true
  }
  return false
}

export function isCuotaFilaCreada(row: CuotasPendientesRow): boolean {
  const glosa = norm(row.glosa)
  return glosa.includes('CREADO') || glosa.startsWith('CRE ')
}

export function getCuotaRowVariant(row: CuotasPendientesRow): CuotaRowVariant {
  if (isCuotaFilaResumen(row)) {
    return 'resumen'
  }
  if (isCuotaFilaPagada(row)) {
    return 'pagada'
  }
  if (isCuotaFilaCreada(row)) {
    return 'creada'
  }

  const mora = (row.importeMora ?? 0) > 0
  const atraso = (row.diasAtrazo ?? 0) > 0
  if (mora || atraso) {
    return 'mora'
  }

  return 'pendiente'
}

export function cuotaRowClassName(row: CuotasPendientesRow): string {
  const variant = getCuotaRowVariant(row)
  const selectable = isCuotaSelectable(row)
  return [
    'caja-cuota-row',
    `caja-cuota-row--${variant}`,
    selectable ? '' : 'caja-cuota-row--locked',
  ]
    .filter(Boolean)
    .join(' ')
}

/** Solo cuotas que el cajero puede cobrar en esta pantalla (paridad operativa caja). */
export function isCuotaSelectable(row: CuotasPendientesRow): boolean {
  const planId = row.planPagoId
  if (planId == null || planId <= 0) {
    return false
  }
  const variant = getCuotaRowVariant(row)
  return variant === 'pendiente' || variant === 'mora'
}

export function filtrarCuotasPorVista(
  rows: CuotasPendientesRow[],
  filtro: CuotaVistaFiltro,
): CuotasPendientesRow[] {
  if (filtro === 'todas') {
    return rows
  }
  if (filtro === 'cobrables') {
    return rows.filter(
      (r) => isCuotaSelectable(r) || getCuotaRowVariant(r) === 'resumen',
    )
  }
  return rows.filter(
    (r) => getCuotaRowVariant(r) === 'mora' || getCuotaRowVariant(r) === 'resumen',
  )
}

export function contarCuotasCobrables(rows: CuotasPendientesRow[]): number {
  return rows.filter(isCuotaSelectable).length
}

export function contarCuotasConMora(rows: CuotasPendientesRow[]): number {
  return rows.filter((r) => getCuotaRowVariant(r) === 'mora').length
}

export type CuotaEstadoUi = {
  label: string
  antColor: 'default' | 'success' | 'warning' | 'error' | 'processing' | 'blue'
}

export function getCuotaEstadoUi(row: CuotasPendientesRow): CuotaEstadoUi {
  switch (getCuotaRowVariant(row)) {
    case 'pagada':
      return { label: 'Pagada', antColor: 'success' }
    case 'mora':
      return { label: 'Con mora', antColor: 'error' }
    case 'creada':
      return { label: 'Creada', antColor: 'warning' }
    case 'resumen':
      return { label: 'Resumen', antColor: 'blue' }
    default:
      return { label: 'Pendiente', antColor: 'processing' }
  }
}

export function cuotaRowKey(
  row: CuotasPendientesRow | CuotaCobranzaRow,
  index?: number,
): string | number {
  if (row.planPagoId != null && row.planPagoId > 0) {
    return row.planPagoId
  }
  return `summary-${index ?? 0}-${row.glosa ?? ''}`
}

export function obtenerClavesCuotasPagadas(
  rows: (CuotasPendientesRow | CuotaCobranzaRow)[],
): number[] {
  return rows
    .filter(isCuotaFilaPagada)
    .map((r) => r.planPagoId)
    .filter((id): id is number => id != null && id > 0)
}
