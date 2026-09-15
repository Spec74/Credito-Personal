import type { ColumnsType } from 'antd/es/table'
import type { RptCreditoObservadoRow } from '../types/api'
import { formatInformeFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas clientes nuevos — mismo detalle que <c>rptCreditoObservado.rdlc</c> (ReporteClientesNuevosMes). */
export function buildClientesNuevosMesInformeColumns(): ColumnsType<RptCreditoObservadoRow> {
  return [
    { title: 'Of. Id', dataIndex: 'oficinaId', minWidth: 64, align: 'right' },
    { title: 'Oficina', dataIndex: 'oficina', ellipsis: true, minWidth: 100 },
    { title: 'Cred', dataIndex: 'creditoId', minWidth: 64, fixed: 'left' },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true, minWidth: 140 },
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
    { title: 'Ag. Id', dataIndex: 'agenteId', minWidth: 72, align: 'right' },
    { title: 'Agente', dataIndex: 'agente', ellipsis: true, minWidth: 120 },
    { title: 'Observación', dataIndex: 'observacion', ellipsis: true, minWidth: 160 },
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
