import { useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { PrinterOutlined, SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadMovimientoBovedaTicketPdf,
  downloadRptMovimientoBovedaCsv,
  downloadRptMovimientoBovedaPdf,
  fetchBovedaAbierta,
  fetchRptMovimientoBoveda,
  type RptMovimientoBovedaRow,
} from '../../api/boveda'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

type FormValues = { bovedaId: number }

export function MovimientoBovedaPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()
  const oficinaId = session?.oficinaId ?? 0

  const bovedaParam = searchParams.get('bovedaId')
  const bovedaFromUrl =
    bovedaParam && !Number.isNaN(Number(bovedaParam))
      ? Number(bovedaParam)
      : undefined

  const bovedaAbierta = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0 && !bovedaFromUrl,
    retry: false,
  })

  useEffect(() => {
    const id = bovedaFromUrl ?? bovedaAbierta.data?.bovedaId
    if (id) {
      form.setFieldsValue({ bovedaId: id })
    }
  }, [bovedaFromUrl, bovedaAbierta.data, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchRptMovimientoBoveda(v.bovedaId),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadRptMovimientoBovedaCsv(v.bovedaId),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadRptMovimientoBovedaPdf(v.bovedaId),
  })

  const ticket = useMutation({
    mutationFn: downloadMovimientoBovedaTicketPdf,
    onSuccess: () => message.success('Ticket descargado'),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptMovimientoBovedaRow> = [
    { title: 'Id', dataIndex: 'movimientoBovedaId', width: 70 },
    {
      title: 'Fecha',
      dataIndex: 'fechaReg',
      width: 110,
      render: (v: string) => formatFecha(v),
    },
    { title: 'Operación', dataIndex: 'codOperacion', width: 90 },
    { title: 'Glosa', dataIndex: 'glosa', ellipsis: true },
    {
      title: 'Entrada',
      dataIndex: 'entrada',
      width: 100,
      align: 'right',
      render: (v: number | null) => (v != null ? formatMoney(v) : '—'),
    },
    {
      title: 'Salida',
      dataIndex: 'salida',
      width: 100,
      align: 'right',
      render: (v: number | null) => (v != null ? formatMoney(v) : '—'),
    },
    { title: 'Tipo pago', dataIndex: 'tipoPago', width: 100, ellipsis: true },
    { title: 'Agente', dataIndex: 'agente', width: 120, ellipsis: true },
    {
      title: '',
      key: 'ticket',
      width: 90,
      render: (_: unknown, row: RptMovimientoBovedaRow) => (
        <Button
          size="small"
          icon={<PrinterOutlined />}
          loading={
            ticket.isPending && ticket.variables === row.movimientoBovedaId
          }
          onClick={() => ticket.mutate(row.movimientoBovedaId)}
        >
          Ticket
        </Button>
      ),
    },
  ]

  return (
    <CredixInformePage
      title="Movimientos de bóveda"
      subtitle="Detalle de entradas y salidas por bóveda abierta o indicada."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/tesoreria">Tesorería</Link> },
        { title: 'Movimientos' },
      ]}
      stats={stats}
      filters={
        <Form form={form} layout="inline" onFinish={(v) => consulta.mutate(v)}>
          <Form.Item
            name="bovedaId"
            label="Bóveda"
            rules={[{ required: true, message: 'Indique bóveda' }]}
          >
            <InputNumber min={1} style={{ width: 120 }} />
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
          onCsv={async () => csv.mutate(await form.validateFields())}
          onPdfTabular={async () => pdf.mutate(await form.validateFields())}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            message="Error al consultar"
            description={
              consulta.error instanceof ApiError
                ? consulta.error.message
                : 'Error desconocido'
            }
            style={{ marginBottom: 16 }}
          />
        ) : null
      }
    >
      <CredixDataTable<RptMovimientoBovedaRow>
        rowKey="movimientoBovedaId"
        loading={consulta.isPending}
        dataSource={consulta.data ?? []}
        columns={columns}
        pagination={{ pageSize: 25, showSizeChanger: true }}
        locale={{ emptyText: 'Consulte para ver movimientos' }}
      />
    </CredixInformePage>
  )
}
