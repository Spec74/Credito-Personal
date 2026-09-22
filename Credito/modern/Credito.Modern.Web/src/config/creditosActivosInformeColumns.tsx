import type { ColumnsType } from 'antd/es/table'
import type { RptCreditosActivosRow } from '../types/api'
import { formatFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas — mismo orden que CreditosActivosCols / catálogo PDF. */
export function buildCreditosActivosInformeColumns(): ColumnsType<RptCreditosActivosRow> {
  return [
    { title: 'Nº', dataIndex: 'nro', width: 55 },
    { title: 'Estado', dataIndex: 'estado', width: 110 },
    { title: 'Agente', dataIndex: 'agente', width: 110, ellipsis: true },
    { title: 'Crédito', dataIndex: 'creditoId', width: 75 },
    { title: 'Código', dataIndex: 'codigo', width: 80 },
    { title: 'Cliente', dataIndex: 'cliente', width: 140, ellipsis: true },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Forma pago', dataIndex: 'formaPago', width: 85 },
    { title: 'Cuotas', dataIndex: 'numeroCuotas', width: 60 },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Monto int.',
      dataIndex: 'montoInteres',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total crédito',
      dataIndex: 'montoCreditoTotal',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Gastos adm.',
      dataIndex: 'montoGastosAdm',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cent. riesgo',
      dataIndex: 'centralRiesgo',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: '1er pago',
      dataIndex: 'fechaPrimerPago',
      width: 100,
      render: formatFecha,
    },
    {
      title: 'Vencimiento',
      dataIndex: 'fechaVencimiento',
      width: 100,
      render: formatFecha,
    },
    { title: 'Cuotas pag.', dataIndex: 'nroCuotasPagado', width: 80 },
    {
      title: 'Pagado',
      dataIndex: 'pagado',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Int. pagado',
      dataIndex: 'interesPagado',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Cuotas pend.', dataIndex: 'nroCuotasPen', width: 85 },
    {
      title: 'Saldo cap.',
      dataIndex: 'saldoCapital',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo int.',
      dataIndex: 'saldoInteres',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo',
      dataIndex: 'saldo',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Días mora', dataIndex: 'diasAtrazo', width: 75 },
    {
      title: 'Mora',
      dataIndex: 'mora',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
  ]
}
