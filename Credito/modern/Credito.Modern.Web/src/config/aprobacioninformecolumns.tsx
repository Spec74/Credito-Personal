import type { ColumnsType } from 'antd/es/table'
import type { RptCreditoAprobacionRow } from '../types/api'
import { formatInformeFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas — mismo orden que PDF <c>rptCreditoAprobacion.rdlc</c> / catálogo legacy. */
export function buildAprobacionInformeColumns(): ColumnsType<RptCreditoAprobacionRow> {
  return [
    { title: 'Cred', dataIndex: 'creditoId', minWidth: 64, fixed: 'left' },
    { title: 'Oficina', dataIndex: 'oficina', ellipsis: true, minWidth: 100 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true, minWidth: 140 },
    {
      title: 'F. aprobación',
      dataIndex: 'fechaAprobacion',
      minWidth: 110,
      render: (v) => formatInformeFecha(v as string),
    },
    {
      title: 'Monto crédito',
      dataIndex: 'montoCredito',
      minWidth: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      minWidth: 88,
      align: 'right',
      render: formatMoney,
    },
    { title: 'N° cuotas', dataIndex: 'numeroCuotas', minWidth: 72, align: 'right' },
    {
      title: 'Desembolso',
      dataIndex: 'montoDesembolso',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Gestor', dataIndex: 'gestor', ellipsis: true, minWidth: 120 },
  ]
}
