import type { ColumnsType } from 'antd/es/table'
import type { RptCreditoObservadoRow } from '../types/api'
import { formatInformeFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas — mismo orden que PDF <c>rptCreditoObservado.rdlc</c> / catálogo legacy. */
export function buildCreditoObservadoInformeColumns(): ColumnsType<RptCreditoObservadoRow> {
  return [
    { title: 'Crédito', dataIndex: 'creditoId', minWidth: 72, fixed: 'left' },
    { title: 'Oficina', dataIndex: 'oficina', ellipsis: true, minWidth: 120 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true, minWidth: 160 },
    {
      title: 'F. 1er pago',
      dataIndex: 'fechaPrimerPago',
      minWidth: 100,
      render: (v) => formatInformeFecha(v as string),
    },
    {
      title: 'F. vcto',
      dataIndex: 'fechaVencimiento',
      minWidth: 100,
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
    { title: 'Agente', dataIndex: 'agente', ellipsis: true, minWidth: 140 },
    { title: 'Observación', dataIndex: 'observacion', ellipsis: true, minWidth: 180 },
    {
      title: 'Trámite adm.',
      dataIndex: 'tramiteAdm',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cent. riesgo',
      dataIndex: 'centralRiesgo',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
  ]
}
