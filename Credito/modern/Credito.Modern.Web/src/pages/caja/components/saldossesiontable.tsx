import { useMemo } from 'react'
import { Button, Tag } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { PrinterOutlined } from '@ant-design/icons'
import type { SaldoCajaSesionPage, SaldoCajaSesionRow } from '../../../api/saldosCaja'
import {
  CredixDataTable,
  CredixTotalsRow,
  CredixWideTable,
  type CredixTotals,
} from '../../../components/credix'
import { useResponsiveColumns } from '../../../hooks/useResponsiveColumns'
import { formatFecha } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'
import { SALDOS_SESION_SCROLL } from './saldosTableLayout'

type Props = {
  pagina?: SaldoCajaSesionPage
  loading?: boolean
  /** Define a qué reporte apunta el botón de impresión (caja chica vs caja diario). */
  esCajaChica: boolean
  emptyText: string
  page: number
  pageSize: number
  onPageChange: (page: number, pageSize: number) => void
  onImprimir: (id: number, esCajaChica: boolean) => void
}

/**
 * Tabla de sesiones de caja (tabs diario, chica y bóveda). Pagina contra el servidor —como el
 * jqGrid del legado— para no traer todo el historial, y los totales vienen del servidor.
 */
export function SaldosSesionTable({
  pagina,
  loading,
  esCajaChica,
  emptyText,
  page,
  pageSize,
  onPageChange,
  onImprimir,
}: Props) {
  const columns = useMemo<ColumnsType<SaldoCajaSesionRow>>(
    () => [
      { title: 'N°', dataIndex: 'id', align: 'center', width: 76 },
      { title: 'Caja', dataIndex: 'caja', ellipsis: true, width: 150 },
      { title: 'Responsable', dataIndex: 'usuario', ellipsis: true, width: 190 },
      {
        title: 'Saldo ini.',
        dataIndex: 'saldoInicial',
        align: 'right',
        width: 112,
        render: formatMoney,
      },
      {
        title: 'Saldo final',
        dataIndex: 'saldoFinal',
        align: 'right',
        width: 118,
        render: (v: number) => <strong>{formatMoney(v)}</strong>,
      },
      {
        title: 'Inicio',
        dataIndex: 'fechaIniOperacion',
        align: 'center',
        width: 128,
        render: formatFecha,
        responsive: ['md'],
      },
      {
        title: 'Fin',
        dataIndex: 'fechaFinOperacion',
        align: 'center',
        width: 128,
        render: formatFecha,
        responsive: ['lg'],
      },
      {
        title: 'Cerrado',
        dataIndex: 'indCierre',
        align: 'center',
        width: 96,
        render: (v: boolean) => (v ? <Tag color="green">Sí</Tag> : <Tag>No</Tag>),
        responsive: ['md'],
      },
      {
        title: 'Bóveda',
        dataIndex: 'transBoveda',
        align: 'center',
        width: 96,
        render: (v: boolean) => (v ? <Tag color="blue">Sí</Tag> : <Tag>No</Tag>),
        responsive: ['lg'],
      },
      {
        title: '',
        key: 'pdf',
        align: 'center',
        fixed: 'right',
        width: 52,
        render: (_, row) => (
          <Button
            type="text"
            size="small"
            icon={<PrinterOutlined />}
            aria-label={`Imprimir saldo de la sesión ${row.id}`}
            onClick={() => onImprimir(row.id, esCajaChica)}
          />
        ),
      },
    ],
    [esCajaChica, onImprimir],
  )

  const visibles = useResponsiveColumns(columns)

  const totals = useMemo<CredixTotals>(
    () => ({
      id: <strong>TOTAL</strong>,
      saldoInicial: formatMoney(pagina?.totalSaldoInicial ?? 0),
      saldoFinal: <strong>{formatMoney(pagina?.totalSaldoFinal ?? 0)}</strong>,
    }),
    [pagina?.totalSaldoInicial, pagina?.totalSaldoFinal],
  )

  const rows = pagina?.rows ?? []

  return (
    <CredixWideTable>
      <CredixDataTable<SaldoCajaSesionRow>
        mode="operacion"
        className="caja-saldos-table"
        rowKey="id"
        tableLayout="fixed"
        scroll={SALDOS_SESION_SCROLL}
        loading={loading}
        dataSource={rows}
        columns={visibles}
        locale={{ emptyText }}
        pagination={{
          current: page,
          pageSize,
          total: pagina?.totalRecords ?? 0,
          showSizeChanger: true,
          onChange: onPageChange,
        }}
        summary={
          rows.length
            ? () => <CredixTotalsRow columns={visibles} totals={totals} />
            : undefined
        }
      />
    </CredixWideTable>
  )
}
