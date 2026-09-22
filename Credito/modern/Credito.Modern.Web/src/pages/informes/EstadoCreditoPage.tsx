import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Button, Descriptions, InputNumber } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadRptEstadoCreditoCsv,
  downloadRptEstadoCreditoPdf,
  fetchRptEstadoCredito,
} from '../../api/creditoPlanes'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { EstadoPlanPagoCuota } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

export function EstadoCreditoPage() {
  const [creditoId, setCreditoId] = useState<number | null>(null)

  const consulta = useMutation({
    mutationFn: (id: number) => fetchRptEstadoCredito(id),
  })

  const csv = useMutation({
    mutationFn: (id: number) => downloadRptEstadoCreditoCsv(id),
  })

  const pdf = useMutation({
    mutationFn: (id: number) => downloadRptEstadoCreditoPdf(id),
  })

  const consultar = () => {
    if (creditoId != null && creditoId >= 1) {
      consulta.mutate(creditoId)
    }
  }

  const stats = useInformeStats({ ...consulta, data: consulta.data?.cuotas })
  const cab = consulta.data?.cabecera

  const columns: ColumnsType<EstadoPlanPagoCuota> = [
    { title: 'N°', dataIndex: 'numero', width: 50 },
    {
      title: 'Capital',
      dataIndex: 'capital',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Amortización',
      dataIndex: 'amortizacion',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Gastos adm.',
      dataIndex: 'gastosAdm',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Vencimiento',
      dataIndex: 'fechaVencimiento',
      width: 105,
      render: formatFecha,
    },
    {
      title: 'Cuota',
      dataIndex: 'cuota',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 70 },
    {
      title: 'Días atraso',
      dataIndex: 'diasAtrazo',
      width: 90,
      render: (v: number | null) => (v != null ? v : '—'),
    },
    {
      title: 'Imp. mora',
      dataIndex: 'importeMora',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Descuento',
      dataIndex: 'descuento',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cargo',
      dataIndex: 'cargo',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Pago libre',
      dataIndex: 'pagoLibre',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'F. pago cuota',
      dataIndex: 'fechaPagoCuota',
      width: 110,
      render: formatFecha,
    },
    {
      title: 'Pago cuota',
      dataIndex: 'pagoCuota',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
  ]

  return (
    <CredixInformePage
      title="Estado de crédito"
      subtitle="Cabecera y cuotas del plan de pagos de un crédito; indique el número de crédito."
      breadcrumb={reportesCreditoBreadcrumb('Estado crédito')}
      stats={stats}
      filters={
        <>
          <InputNumber
            min={1}
            placeholder="Crédito ID"
            value={creditoId ?? undefined}
            onChange={(v) => setCreditoId(v ?? null)}
            style={{ width: 140, marginRight: 8 }}
          />
          <Button
            type="primary"
            icon={<SearchOutlined />}
            onClick={consultar}
            loading={consulta.isPending}
            disabled={creditoId == null || creditoId < 1}
          >
            Consultar
          </Button>
        </>
      }
      exportBar={
        creditoId != null && creditoId >= 1 ? (
          <InformeExportBar
            csvLoading={csv.isPending}
            pdfLoading={pdf.isPending}
            onCsv={() => csv.mutate(creditoId)}
            onPdfTabular={() => pdf.mutate(creditoId)}
          />
        ) : null
      }
      panelTitle="Plan de pagos"
    >
      {cab ? (
        <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }} style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Crédito">{cab.creditoId}</Descriptions.Item>
          <Descriptions.Item label="Producto">{cab.producto}</Descriptions.Item>
          <Descriptions.Item label="Cliente">{cab.cliente}</Descriptions.Item>
          <Descriptions.Item label="Modalidad">{cab.modalidad}</Descriptions.Item>
          <Descriptions.Item label="Cuotas">{cab.numeroCuotas}</Descriptions.Item>
          <Descriptions.Item label="Estado">{cab.estado}</Descriptions.Item>
          <Descriptions.Item label="Monto crédito">
            {formatMoney(cab.montoCredito)}
          </Descriptions.Item>
          <Descriptions.Item label="Total (monto + intereses)">
            {formatMoney(cab.total)}
          </Descriptions.Item>
          <Descriptions.Item label="Analista">{cab.analista}</Descriptions.Item>
        </Descriptions>
      ) : null}
      <CredixDataTable<EstadoPlanPagoCuota>
        rowKey="planPagoId"
        columns={columns}
        dataSource={consulta.data?.cuotas ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 24 }}
        scroll={{ x: 1400 }}
        locale={{ emptyText: 'Indique crédito y consulte' }}
      />
    </CredixInformePage>
  )
}
