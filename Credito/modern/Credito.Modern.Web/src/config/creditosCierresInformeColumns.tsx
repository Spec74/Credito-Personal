import type { ColumnsType } from 'antd/es/table'
import type { RptCreditosCierresRow } from '../types/api'
import { formatFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

/** Columnas — mismo orden que catálogo PDF CreditosCierres. */
export function buildCreditosCierresInformeColumns(): ColumnsType<RptCreditosCierresRow> {
  return [
    { title: 'Crédito', dataIndex: 'creditoId', width: 75 },
    { title: 'Estado', dataIndex: 'estado', width: 100 },
    { title: 'Agente', dataIndex: 'agente', width: 120, ellipsis: true },
    { title: 'Código', dataIndex: 'codigo', width: 80 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Forma pago', dataIndex: 'formaPago', width: 90 },
    { title: 'Cuotas', dataIndex: 'numeroCuotas', width: 65 },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 85,
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
    {
      title: 'Σ capital',
      dataIndex: 'sumAmortizacion',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Σ interés',
      dataIndex: 'sumInteres',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Σ cuota',
      dataIndex: 'sumCuota',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Σ n° cuota', dataIndex: 'sumNroCuota', width: 85, align: 'right' },
  ]
}
