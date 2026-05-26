import type { ColumnsType } from 'antd/es/table'
import type { RptCobroDiarioRow } from '../types/api'
import { formatInformeFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas — mismo orden que PDF <c>rptCobroDiario.rdlc</c>; minWidth para scroll en móvil. */
export function buildCobroDiarioInformeColumns(): ColumnsType<RptCobroDiarioRow> {
  return [
    { title: 'Nro', dataIndex: 'nro', minWidth: 56, align: 'right' },
    { title: 'Ord.', dataIndex: 'orden', minWidth: 56, align: 'right' },
    { title: 'Cred', dataIndex: 'creditoId', minWidth: 64, fixed: 'left' },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true, minWidth: 140 },
    { title: 'Celular', dataIndex: 'celular', minWidth: 100 },
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
    {
      title: 'Cuota plan',
      dataIndex: 'cuotaPlan',
      minWidth: 92,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo',
      dataIndex: 'saldo',
      minWidth: 88,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Días atraso', dataIndex: 'diasAtrazo', minWidth: 88, align: 'right' },
    { title: 'Cuotas pend.', dataIndex: 'nroCuotasPen', minWidth: 92, align: 'right' },
    {
      title: 'Cuota total',
      dataIndex: 'cuotaTotal',
      minWidth: 92,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Dirección', dataIndex: 'direccion', ellipsis: true, minWidth: 160 },
    {
      title: 'F. pago',
      dataIndex: 'fechaPago',
      minWidth: 100,
      render: (v) => formatInformeFecha(v as string | null),
    },
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
      title: 'Mora',
      dataIndex: 'mora',
      minWidth: 88,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Monto total',
      dataIndex: 'montoTotal',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Negocio', dataIndex: 'negocio', ellipsis: true, minWidth: 100 },
    { title: 'Forma pago', dataIndex: 'formaPago', ellipsis: true, minWidth: 96 },
    {
      title: 'Tope crédito',
      dataIndex: 'topeCredito',
      minWidth: 96,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Clasif. SBS', dataIndex: 'clasificacionRiesgoSbs', ellipsis: true, minWidth: 96 },
  ]
}
