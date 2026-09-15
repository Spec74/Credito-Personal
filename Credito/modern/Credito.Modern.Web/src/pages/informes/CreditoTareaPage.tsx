import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Button, Select } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadRptCreditoTareaCsv,
  downloadRptCreditoTareaPdf,
  fetchRptCreditoTarea,
} from '../../api/creditoPlanes'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { CreditoTareaReportParams, RptCreditoTareaRow } from '../../types/api'

const ESTADOS = [
  { value: 'PEN', label: 'Pendiente (default legacy)' },
  { value: 'COM', label: 'Completada' },
  { value: '', label: 'Todos' },
]

export function CreditoTareaPage() {
  const [estado, setEstado] = useState('PEN')

  const params: CreditoTareaReportParams = {
    estado: estado || undefined,
  }

  const consulta = useMutation({
    mutationFn: () => fetchRptCreditoTarea(params),
  })

  const csv = useMutation({
    mutationFn: () => downloadRptCreditoTareaCsv(params),
  })

  const pdf = useMutation({
    mutationFn: () => downloadRptCreditoTareaPdf(params),
  })

  const stats = useInformeStats(consulta)
  const columns: ColumnsType<RptCreditoTareaRow> = [
    { title: 'N°', dataIndex: 'nro', width: 50 },
    { title: 'Tarea', dataIndex: 'tareaId', width: 70 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    { title: 'Analista', dataIndex: 'analista', width: 120, ellipsis: true },
    { title: 'Subtareas', dataIndex: 'subtareasResumen', width: 90 },
    { title: 'Estado', dataIndex: 'estado', width: 80 },
    {
      title: 'Detalle subtareas',
      dataIndex: 'detalleSubtareas',
      ellipsis: true,
      render: (v: string) => (
        <span style={{ whiteSpace: 'pre-wrap' }}>{v}</span>
      ),
    },
  ]

  return (
    <CredixInformePage
      title="Reporte de tareas"
      subtitle="Tareas de crédito por estado; pendiente por defecto como en el informe legacy."
      breadcrumb={reportesCreditoBreadcrumb('Tareas de crédito')}
      stats={stats}
      filters={
        <>
          <Select
            value={estado}
            onChange={setEstado}
            options={ESTADOS}
            style={{ width: 220, marginRight: 8 }}
          />
          <Button
            type="primary"
            icon={<SearchOutlined />}
            onClick={() => consulta.mutate()}
            loading={consulta.isPending}
          >
            Consultar
          </Button>
        </>
      }
      exportBar={
        <InformeExportBar
          csvLoading={csv.isPending}
          pdfLoading={pdf.isPending}
          onCsv={() => csv.mutate()}
          onPdfTabular={() => pdf.mutate()}
        />
      }
    >
      <CredixDataTable<RptCreditoTareaRow>
        rowKey="tareaId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 20 }}
        locale={{ emptyText: 'Consulte para ver tareas' }}
      />
    </CredixInformePage>
  )
}
