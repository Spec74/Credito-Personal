import type { ColumnsType } from 'antd/es/table'
import type { RptClientesTopeCreditoRow } from '../types/api'
import { formatMoney } from '../utils/formatMoney'

/** Columnas — mismo orden que <c>rptClienteTopeCredito.rdlc</c> / catálogo legacy. */
export function buildClientesTopeCreditoInformeColumns(): ColumnsType<RptClientesTopeCreditoRow> {
  return [
    { title: 'Agente', dataIndex: 'agente', ellipsis: true, minWidth: 120 },
    { title: 'N° documento', dataIndex: 'numeroDocumento', minWidth: 108 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true, minWidth: 140 },
    { title: 'Dirección', dataIndex: 'direccion', ellipsis: true, minWidth: 160 },
    { title: 'Dir. ref.', dataIndex: 'direccionRef', ellipsis: true, minWidth: 100 },
    { title: 'Celular', dataIndex: 'celular', minWidth: 100 },
    { title: 'Calificación', dataIndex: 'calificacion', minWidth: 96 },
    {
      title: 'Tope crédito',
      dataIndex: 'topeCredito',
      minWidth: 100,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Nota', dataIndex: 'nota', ellipsis: true, minWidth: 120 },
  ]
}
