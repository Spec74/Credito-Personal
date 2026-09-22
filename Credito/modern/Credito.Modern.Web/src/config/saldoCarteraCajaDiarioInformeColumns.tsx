import type { ColumnsType } from 'antd/es/table'
import type { RptSaldoCarteraCajaDiarioRow } from '../types/api'
import { formatFecha } from '../utils/formatFecha'
import { formatMoney } from '../utils/formatMoney'

function formatPct(v: number | null): string {
  return v != null ? `${v.toLocaleString('es-PE', { maximumFractionDigits: 2 })}%` : '—'
}

/** Columnas — matriz ini/fin de SaldoCarteraCols (sin AgenteId). */
export function buildSaldoCarteraCajaDiarioInformeColumns(): ColumnsType<RptSaldoCarteraCajaDiarioRow> {
  return [
    { title: 'Oficina', dataIndex: 'oficina', width: 110, ellipsis: true },
    { title: 'Caja', dataIndex: 'caja', width: 100, fixed: 'left', ellipsis: true },
    { title: 'Agente', dataIndex: 'agente', width: 120, ellipsis: true },
    {
      title: 'Cierre ini.',
      dataIndex: 'fechaCierreIni',
      width: 100,
      render: formatFecha,
    },
    {
      title: 'Desemb. ini.',
      dataIndex: 'salidasIni',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cobrado ini.',
      dataIndex: 'montoCobradoIni',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: '% cobro ini.',
      dataIndex: 'pocentajeCobroIni',
      width: 90,
      align: 'right',
      render: (v: number | null) => formatPct(v),
    },
    {
      title: 'Cart. s/mora ini.',
      dataIndex: 'saldoCarteraSinMoraIni',
      width: 110,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cli. s/mora ini.',
      dataIndex: 'nroClientesCarteraSinMoraIni',
      width: 100,
      align: 'right',
    },
    {
      title: 'Mora ini.',
      dataIndex: 'saldoMoraCarteraIni',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cli. mora ini.',
      dataIndex: 'nroClientesSaldoMoraCarteraIni',
      width: 100,
      align: 'right',
    },
    {
      title: 'Nuevos ini.',
      dataIndex: 'nroClientesNuevosIni',
      width: 90,
      align: 'right',
    },
    {
      title: 'Vencido ini.',
      dataIndex: 'saldoVencidoIni',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Morosid. ini.',
      dataIndex: 'saldoMorosidadIni',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cierre fin',
      dataIndex: 'fechaCierreFin',
      width: 100,
      render: formatFecha,
    },
    {
      title: 'Desemb. fin',
      dataIndex: 'salidasFin',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cobrado fin',
      dataIndex: 'montoCobradoFin',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: '% cobro fin',
      dataIndex: 'pocentajeCobroFin',
      width: 90,
      align: 'right',
      render: (v: number | null) => formatPct(v),
    },
    {
      title: 'Cart. s/mora fin',
      dataIndex: 'saldoCarteraSinMoraFin',
      width: 110,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cli. s/mora fin',
      dataIndex: 'nroClientesCarteraSinMoraFin',
      width: 100,
      align: 'right',
    },
    {
      title: 'Mora fin',
      dataIndex: 'saldoMoraCarteraFin',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cli. mora fin',
      dataIndex: 'nroClientesSaldoMoraCarteraFin',
      width: 100,
      align: 'right',
    },
    {
      title: 'Nuevos fin',
      dataIndex: 'nroClientesNuevosFin',
      width: 90,
      align: 'right',
    },
    {
      title: 'Vencido fin',
      dataIndex: 'saldoVencidoFin',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Morosid. fin',
      dataIndex: 'saldoMorosidadFin',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
  ]
}
