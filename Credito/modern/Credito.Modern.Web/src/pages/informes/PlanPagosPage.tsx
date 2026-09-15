import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Button, InputNumber } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadRptPlanPagosCsv,
  downloadRptPlanPagosPdf,
  fetchRptPlanPagos,
} from '../../api/creditoPlanes'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { RptPlanPagosRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

export function PlanPagosPage() {
  const [creditoId, setCreditoId] = useState<number | null>(null)

  const consulta = useMutation({
    mutationFn: (id: number) => fetchRptPlanPagos(id),
  })

  const csv = useMutation({
    mutationFn: (id: number) => downloadRptPlanPagosCsv(id),
  })

  const pdf = useMutation({
    mutationFn: (id: number) => downloadRptPlanPagosPdf(id),
  })

  const consultar = () => {
    if (creditoId != null && creditoId >= 1) {
      consulta.mutate(creditoId)
    }
  }

  const stats = useInformeStats(consulta)
  const columns: ColumnsType<RptPlanPagosRow> = [
    { title: 'N°', dataIndex: 'numero', width: 55 },
    {
      title: 'Capital',
      dataIndex: 'capital',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Fecha pago',
      dataIndex: 'fechaPago',
      width: 105,
      render: formatFecha,
    },
    {
      title: 'Amort.',
      dataIndex: 'amortizacion',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 90,
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
      title: 'Cuota',
      dataIndex: 'cuota',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
  ]

  return (
    <CredixInformePage
      title="Plan de pagos (crédito)"
      subtitle="Cuotas del plan de pagos de un crédito; indique el número de crédito y consulte."
      breadcrumb={reportesCreditoBreadcrumb('Plan de pagos')}
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
    >
      <CredixDataTable<RptPlanPagosRow>
        rowKey="numero"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 24 }}
        locale={{ emptyText: 'Indique crédito y consulte' }}
      />
    </CredixInformePage>
  )
}
