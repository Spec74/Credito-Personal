import type { ColumnsType } from 'antd/es/table'
import type { RptClientesInactivosRow } from '../types/api'
import { formatFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas — mismo orden que RDLC / catálogo PDF (Tope, SBS, Depurado). */
export function buildClientesInactivosInformeColumns(): ColumnsType<RptClientesInactivosRow> {
  return [
    { title: 'Agente', dataIndex: 'agente', ellipsis: true, minWidth: 120 },
    { title: 'DNI', dataIndex: 'dni', minWidth: 100 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true, minWidth: 140 },
    { title: 'Dirección', dataIndex: 'direccion', ellipsis: true, minWidth: 160 },
    { title: 'Dir. ref.', dataIndex: 'direccionRef', ellipsis: true, minWidth: 100 },
    { title: 'Celular', dataIndex: 'celular', minWidth: 100 },
    { title: 'Cal.', dataIndex: 'calificacion', minWidth: 56, align: 'center' },
    { title: 'SBS', dataIndex: 'clasificacionRiesgoSBS', minWidth: 100, align: 'center' },
    { title: 'Depurado', dataIndex: 'depurado', minWidth: 80, align: 'center' },
    { title: 'Dir. negocio', dataIndex: 'direccionNegocio', ellipsis: true, minWidth: 140 },
    { title: 'Dir. neg. ref.', dataIndex: 'direccionNegocioRef', ellipsis: true, minWidth: 100 },
    {
      title: 'Monto crédito',
      dataIndex: 'montoCredito',
      minWidth: 110,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cant. créditos',
      dataIndex: 'totalCreditos',
      minWidth: 100,
      align: 'right',
    },
    {
      title: 'Fecha cancelación',
      dataIndex: 'fechaCancelacion',
      minWidth: 120,
      render: (v: string | null) => formatFecha(v),
    },
    {
      title: 'Días inact.',
      dataIndex: 'diasInactividad',
      minWidth: 90,
      align: 'right',
    },
    {
      title: 'Tope crédito',
      dataIndex: 'topeCredito',
      minWidth: 100,
      align: 'right',
      render: formatMoney,
    },
  ]
}
