import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadCobroDiarioDetalleCsv,
  downloadCobroDiarioDetallePdf,
  fetchCobroDiarioDetalle,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import { canViewReporteCredito } from '../../utils/reporteCreditoAccess'
import type { CobroDiarioParams, GestorInformeParams, RptCobroDiarioDetalleRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

function toCobroDiarioParams(p: GestorInformeParams): CobroDiarioParams {
  if (!p.usuarioId || p.usuarioId < 1) {
    throw new Error('Indique un gestor válido')
  }
  return { oficinaId: p.oficinaId, usuarioId: p.usuarioId }
}

export function CobroDiarioDetallePage() {
  const { session } = useAuth()
  const [form] = Form.useForm<GestorInformeParams>()
  const puedeElegirGestor = canViewReporteCredito(session?.roles ?? [])

  useEffect(() => {
    if (session) {
      form.setFieldsValue({
        oficinaId: session.oficinaId,
        usuarioId: session.usuarioId,
      })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (p: GestorInformeParams) => fetchCobroDiarioDetalle(toCobroDiarioParams(p)),
  })

  const csv = useMutation({
    mutationFn: downloadCobroDiarioDetalleCsv,
  })

  const pdf = useMutation({
    mutationFn: downloadCobroDiarioDetallePdf,
  })

  const sinFilas = !consulta.data?.length
  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptCobroDiarioDetalleRow> = [
    { title: 'N°', dataIndex: 'nro', width: 55 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    { title: 'Forma pago', dataIndex: 'formaPago', width: 90 },
    {
      title: 'Monto crédito',
      dataIndex: 'montoCredito',
      width: 100,
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
      title: 'Monto total',
      dataIndex: 'montoTotal',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: '1.er pago',
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
      title: 'Saldo',
      dataIndex: 'saldo',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total pago',
      dataIndex: 'totalPago',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Días mora', dataIndex: 'diasAtrazoMora', width: 80, align: 'right' },
    { title: 'Pagos', dataIndex: 'pagos', ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Cobro diario (detalle)"
      subtitle="Detalle de cartera del día por gestor y oficina."
      breadcrumb={reportesCreditoBreadcrumb('Cobro diario detalle')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{
            oficinaId: session?.oficinaId ?? 0,
            usuarioId: session?.usuarioId ?? 0,
          }}
          onFinish={(v) => consulta.mutate(toCobroDiarioParams(v))}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          {puedeElegirGestor ? (
            <Form.Item
              name="usuarioId"
              label="Gestor"
              rules={[{ required: true, message: 'Seleccione un gestor' }]}
            >
              <GestorSelect legacyList size="middle" />
            </Form.Item>
          ) : (
            <Form.Item name="usuarioId" hidden>
              <InputNumber />
            </Form.Item>
          )}
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
          csvDisabled={sinFilas}
          pdfDisabled={sinFilas}
          onCsv={async () => csv.mutate(toCobroDiarioParams(await form.validateFields()))}
          onPdfTabular={async () => pdf.mutate(toCobroDiarioParams(await form.validateFields()))}
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
      <CredixDataTable<RptCobroDiarioDetalleRow>
        rowKey={(r, i) => String(r.nro ?? i)}
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 15 }}
        locale={{ emptyText: 'Ejecute Consultar para cargar datos' }}
      />
    </CredixInformePage>
  )
}
