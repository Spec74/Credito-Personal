import type { ColumnsType } from 'antd/es/table'
import type { RptCreditoMorosidadRow } from '../types/api'
import { formatFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas fila financiera — mismo orden que PDF legacy (rptCreditoMorosidad). */
export function buildMorosidadInformeColumns(): ColumnsType<RptCreditoMorosidadRow> {
  return [
    { title: 'Cred', dataIndex: 'creditoId', minWidth: 64, fixed: 'left' },
    { title: 'Artículo', dataIndex: 'articulo', ellipsis: true, minWidth: 120 },
    {
      title: 'F. desembolso',
      dataIndex: 'fechaDesembolso',
      minWidth: 100,
      render: formatFecha,
    },
    {
      title: 'F. vencimiento',
      dataIndex: 'fechaVcto',
      minWidth: 100,
      render: formatFecha,
    },
    {
      title: 'Crédito',
      dataIndex: 'montoCredito',
      minWidth: 92,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo crédito',
      dataIndex: 'saldoCredito',
      minWidth: 92,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Capital atraso',
      dataIndex: 'capitalAtrazo',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Gastos adm.',
      dataIndex: 'ga',
      minWidth: 88,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés atraso',
      dataIndex: 'interesAtrazo',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Mora',
      dataIndex: 'mora',
      minWidth: 80,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Importe libre',
      dataIndex: 'importeLibre',
      minWidth: 92,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Días atraso', dataIndex: 'diasAtrazo', minWidth: 82, align: 'right' },
    { title: 'Cuotas atraso', dataIndex: 'cuotasAtrazo', minWidth: 88, align: 'right' },
    {
      title: 'Deuda atraso',
      dataIndex: 'deudaAtrazo',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
  ]
}
