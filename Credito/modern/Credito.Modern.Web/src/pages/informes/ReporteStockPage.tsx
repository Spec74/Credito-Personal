import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Form, InputNumber } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadReporteStockCsv,
  downloadReporteStockPdf,
  fetchReporteStock,
  type ReporteStockRow,
} from '../../api/almacen'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import { reportesAlmacenBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { openReporteStockCsvInTab, openReporteStockPdfInTab } from '../../api/almacen'

type FormValues = { oficinaId: number }

export function ReporteStockPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  useEffect(() => {
    if (session?.oficinaId) {
      form.setFieldsValue({ oficinaId: session.oficinaId })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchReporteStock(v.oficinaId),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadReporteStockCsv(v.oficinaId),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadReporteStockPdf(v.oficinaId),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<ReporteStockRow> = [
    { title: 'Nº', dataIndex: 'nro', width: 70 },
    { title: 'Tipo', dataIndex: 'tipoArticulo', width: 100, ellipsis: true },
    { title: 'Id art.', dataIndex: 'articuloId', width: 80 },
    { title: 'Artículo', dataIndex: 'articulo', ellipsis: true },
    { title: 'Stock', dataIndex: 'stock', width: 80, align: 'right' },
    { title: 'Series', dataIndex: 'series', ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Stock general de productos"
      subtitle="Stock de artículos por oficina; la oficina debe coincidir con la del usuario en sesión."
      breadcrumb={reportesAlmacenBreadcrumb('Stock general')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{ oficinaId: session?.oficinaId ?? 0 }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              icon={<SearchOutlined />}
              htmlType="submit"
              loading={consulta.isPending}
            >
              Consultar
            </Button>
          </Form.Item>
        </Form>
      }
      exportBar={
        <InformeExportBar
          csvLoading={csv.isPending}
          pdfLoading={pdf.isPending}
          onCsv={async () => {
            const v = await form.validateFields()
            await openReporteStockCsvInTab(v.oficinaId)
          }}
          onPdfTabular={async () => {
            const v = await form.validateFields()
            await openReporteStockPdfInTab(v.oficinaId)
          }}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError ? consulta.error.message : 'Error en la consulta'
            }
          />
        ) : null
      }
    >
      <CredixDataTable<ReporteStockRow>
        rowKey={(r) => `${r.articuloId}-${r.nro ?? 0}`}
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 20 }}
        locale={{ emptyText: 'Ejecute Consultar para cargar datos' }}
      />
    </CredixInformePage>
  )
}
