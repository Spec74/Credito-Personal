import { memo, useMemo, useState, type Key } from 'react'
import { Segmented, Tag, Typography, type TableProps } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import type { CuotaCobranzaRow } from './cuotasGridMerge'
import { formatMoney } from '../../utils/formatMoney'
import { formatFecha } from '../../utils/formatFecha'
import {
  contarCuotasCobrables,
  contarCuotasConMora,
  cuotaRowClassName,
  cuotaRowKey,
  filtrarCuotasPorVista,
  getCuotaEstadoUi,
  getCuotaRowVariant,
  isCuotaFilaResumen,
  isCuotaSelectable,
  obtenerClavesCuotasPagadas,
  type CuotaVistaFiltro,
} from './cuotaRowStyle'
import { CajaCuotasLegend } from './CajaCuotasLegend'
import { CredixDataTable } from '../credix'

const { Text } = Typography

function MoraCell({ value, atraso }: { value: number | null; atraso: number | null }) {
  const mora = (value ?? 0) > 0
  const late = (atraso ?? 0) > 0
  if (!mora && !late) {
    return <span>{formatMoney(value)}</span>
  }
  return (
    <span className="caja-cuota-mora-cell">
      {formatMoney(value)}
      {late ? (
        <small className="caja-cuota-atraso-badge">{atraso} d</small>
      ) : null}
    </span>
  )
}

const baseColumns: ColumnsType<CuotaCobranzaRow> = [
  {
    title: 'Estado',
    key: 'estado',
    width: 1,
    render: (_, row) => {
      const { label, antColor } = getCuotaEstadoUi(row)
      return (
        <Tag color={antColor} className="caja-cuota-estado-tag">
          {label}
        </Tag>
      )
    },
  },
  {
    title: 'N°',
    dataIndex: 'numero',
    width: 1,
    align: 'center',
    render: (n: number | null | undefined, row) =>
      n ?? (row.planPagoId != null && row.planPagoId > 0 ? row.planPagoId : '—'),
  },
  {
    title: 'Cuota',
    dataIndex: 'glosa',
    ellipsis: true,
    width: 1,
  },
  {
    title: 'Vence',
    dataIndex: 'fechaVencimiento',
    width: 1,
    render: (v: string | null) => formatFecha(v),
  },
  {
    title: 'Importe',
    dataIndex: 'cuota',
    width: 1,
    align: 'right',
    render: formatMoney,
  },
  {
    title: 'Mora',
    dataIndex: 'importeMora',
    width: 1,
    align: 'right',
    render: (v: number | null, row) => (
      <MoraCell value={v} atraso={row.diasAtrazo} />
    ),
  },
  {
    title: 'A pagar',
    dataIndex: 'pagoCuota',
    width: 1,
    align: 'right',
    render: (v: number | null, row) => {
      const pagada = getCuotaRowVariant(row) === 'pagada'
      return (
        <strong
          className={
            pagada ? 'caja-cuota-pagar caja-cuota-pagar--done' : 'caja-cuota-pagar'
          }
        >
          {formatMoney(v)}
        </strong>
      )
    },
  },
]

export type CajaCuotasTableProps = {
  data: CuotaCobranzaRow[]
  loading?: boolean
  selectedKeys?: Key[]
  onSelectionChange?: (keys: Key[]) => void
  showLegend?: boolean
  showVistaFiltro?: boolean
  pageSize?: number
  extraColumns?: ColumnsType<CuotaCobranzaRow>
  compact?: boolean
}

function CajaCuotasTableInner({
  data,
  loading,
  selectedKeys,
  onSelectionChange,
  showLegend = true,
  showVistaFiltro = true,
  pageSize = 25,
  extraColumns,
  compact,
}: CajaCuotasTableProps) {
  const [vista, setVista] = useState<CuotaVistaFiltro>('todas')

  const cobrables = useMemo(() => contarCuotasCobrables(data), [data])
  const conMora = useMemo(() => contarCuotasConMora(data), [data])
  const pagadas = useMemo(() => obtenerClavesCuotasPagadas(data), [data])
  const pagadasSet = useMemo(() => new Set(pagadas), [pagadas])

  const tableData = useMemo(() => filtrarCuotasPorVista(data, vista), [data, vista])

  const columns = useMemo(
    () => (extraColumns ? [...baseColumns, ...extraColumns] : baseColumns),
    [extraColumns],
  )

  const selectedRowKeys = useMemo(() => {
    const user = (selectedKeys ?? []).filter(
      (k): k is number => typeof k === 'number' && !pagadasSet.has(k),
    )
    return [...user, ...pagadas]
  }, [selectedKeys, pagadas, pagadasSet])

  const rowSelection: TableProps<CuotaCobranzaRow>['rowSelection'] =
    onSelectionChange
      ? {
          selectedRowKeys,
          columnWidth: 40,
          preserveSelectedRowKeys: true,
          onChange: (keys) => {
            const userKeys = keys.filter(
              (k) => typeof k === 'number' && !pagadasSet.has(k),
            )
            const valid = userKeys.filter((k) =>
              data.some((c) => c.planPagoId === k && isCuotaSelectable(c)),
            )
            onSelectionChange(valid)
          },
          getCheckboxProps: (record) => ({
            disabled: !isCuotaSelectable(record),
          }),
        }
      : undefined

  return (
    <div className="caja-cuotas-table-wrap">
      {showLegend ? <CajaCuotasLegend /> : null}

      <div className="caja-cuotas-table-toolbar">
        <Text type="secondary" className="caja-cuotas-table-hint">
          Plan completo del crédito: pagadas (check bloqueado), pendientes y con mora
          seleccionables. Totales al final cuando el sistema los incluye.
        </Text>
        {showVistaFiltro ? (
          <Segmented<CuotaVistaFiltro>
            size="small"
            value={vista}
            onChange={setVista}
            options={[
              { label: `Todas (${data.length})`, value: 'todas' },
              { label: `Cobrables (${cobrables})`, value: 'cobrables' },
              { label: `Con mora (${conMora})`, value: 'mora' },
            ]}
          />
        ) : null}
      </div>

      <CredixDataTable<CuotaCobranzaRow>
        mode="operacion"
        mobileCards={false}
        className="caja-cuotas-table caja-cuotas-table--auto"
        tableLayout="auto"
        rowKey={(row, index) => cuotaRowKey(row, index)}
        rowClassName={cuotaRowClassName}
        columns={columns}
        dataSource={tableData}
        loading={loading}
        rowSelection={rowSelection}
        pagination={{
          pageSize,
          showSizeChanger: true,
          pageSizeOptions: ['10', '25', '50', '100'],
          showTotal: (t) => `${t} fila(s)`,
        }}
        scroll={{ x: 980, y: compact ? 280 : 380 }}
        onRow={(row) =>
          isCuotaFilaResumen(row) ? { 'aria-label': 'Fila de resumen' } : {}
        }
      />
    </div>
  )
}

export const CajaCuotasTable = memo(CajaCuotasTableInner)
