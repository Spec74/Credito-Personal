import type { ColumnsType, ColumnType } from 'antd/es/table'
import { formatFecha, formatInformeFecha } from './formatFecha'
import { formatMoney } from './formatMoney'

/** Campos que en API/CSV suelen traer `yyyy-MM-dd HH:mm:ss`. */
export const INFORME_DATETIME_FIELDS = new Set([
  'fechaIniOperacion',
  'fechaFinOperacion',
  'fechaAprobacion',
  'fechaReg',
  'fechaAnulacion',
  'fechaUltPago',
  'fechaCierre',
  'fechaMovimiento',
])

const TEXT_FIELDS =
  /^(cliente|oficina|agente|gestor|articulo|direccion|entidad|denominacion|observacion|motivo)$/i
const MONEY_FIELDS =
  /monto|saldo|importe|capital|interes|mora|deuda|entradas|salidas|desembolso|libre|^ga$/i

function dataIndexKey(dataIndex: ColumnType<unknown>['dataIndex']): string {
  if (dataIndex == null) {
    return ''
  }
  return Array.isArray(dataIndex) ? dataIndex.join('.') : String(dataIndex)
}

/**
 * Normaliza columnas de informes: anchos flexibles, fechas coherentes, montos alineados.
 * Usado por `CredixDataTable` en pantallas de reporte.
 */
export function enhanceInformeColumns<T extends object>(
  columns: ColumnsType<T> | undefined,
): ColumnsType<T> | undefined {
  if (!columns?.length) {
    return columns
  }

  return columns.map((col) => {
    if ('children' in col && col.children) {
      return col
    }
    const key = dataIndexKey((col as ColumnType<T>).dataIndex)
    const next: ColumnType<T> = { ...col }

    if (key && INFORME_DATETIME_FIELDS.has(key)) {
      next.render = (v) => formatInformeFecha(v as string)
      next.width = next.width ?? 138
      next.minWidth = next.minWidth ?? 128
    } else if (key && /fecha/i.test(key)) {
      if (!next.render) {
        next.render = (v) => formatFecha(v as string)
      }
      next.width = next.width ?? 108
      next.minWidth = next.minWidth ?? 96
    }

    if (key === 'creditoId' || key === 'cajaDiarioId' || key === 'nro') {
      next.width = next.width ?? 72
      next.align = next.align ?? 'center'
    }

    if (key && TEXT_FIELDS.test(key)) {
      next.ellipsis = next.ellipsis ?? true
      next.minWidth = next.minWidth ?? (next.fixed ? 140 : 96)
    }

    if (key && MONEY_FIELDS.test(key) && !next.render) {
      next.align = next.align ?? 'right'
      next.render = (v) => formatMoney(v as number)
      next.width = next.width ?? 96
      next.minWidth = next.minWidth ?? 88
    }

    if (key === 'celular' || key === 'numeroDocumento' || key === 'numDoc') {
      next.width = next.width ?? 108
      next.minWidth = next.minWidth ?? 100
    }

    return next
  })
}

/** Scroll horizontal sugerido según cantidad de columnas (evita `scroll.x` fijos arbitrarios). */
export function informeTableScrollX(columnCount: number): number {
  if (columnCount <= 8) {
    return 960
  }
  if (columnCount <= 12) {
    return 1100
  }
  if (columnCount <= 16) {
    return 1280
  }
  return 1500
}
