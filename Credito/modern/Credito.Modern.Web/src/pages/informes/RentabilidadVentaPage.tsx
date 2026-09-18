import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Checkbox, Form, InputNumber } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadRentabilidadVentaCsv,
  downloadRentabilidadVentaPdf,
  fetchRentabilidadVenta,
  type RentabilidadVentaParams,
  type RptRentabilidadVentaRow,
} from '../../api/ventas'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'

import { useInformeStats } from '../../hooks/useInformeStats'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'


type FormValues = {
  oficinaId: number
  rango: [Dayjs, Dayjs]
  indContado: boolean
  indCredito: boolean
}

function toParams(v: FormValues): RentabilidadVentaParams {
  return {
    oficinaId: v.oficinaId,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
    indContado: v.indContado,
    indCredito: v.indCredito,
  }
}

export function RentabilidadVentaPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  useEffect(() => {
    if (session) {
      form.setFieldsValue({
        oficinaId: session.oficinaId,
        indContado: true,
        indCredito: true,
      })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchRentabilidadVenta(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadRentabilidadVentaCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadRentabilidadVentaPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptRentabilidadVentaRow> = [
    { title: 'Nro', dataIndex: 'nro', width: 55 },
    { title: 'Código', dataIndex: 'codigo', width: 80 },
    { title: 'Artículo', dataIndex: 'articulo', ellipsis: true },
    { title: 'OV', dataIndex: 'ordenVentaId', width: 70 },
    {
      title: 'F. salida',
      dataIndex: 'fechaSal',
      width: 95,
      render: (v) => (v ? formatFecha(v) : '—'),
    },
    {
      title: 'P. salida',
      dataIndex: 'precioSal',
      width: 90,
      align: 'right',
      render: (v) => (v != null ? formatMoney(v) : '—'),
    },
    { title: 'Modalidad', dataIndex: 'modalidad', width: 90 },
    {
      title: 'Rentab.',
      dataIndex: 'rentabilidad',
      width: 90,
      align: 'right',
      render: (v) => (v != null ? formatMoney(v) : '—'),
    },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Rentabilidad de ventas"
      subtitle="Rentabilidad por ventas en el periodo; contado y/o crédito según filtros."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/informes">Informes</Link> },
        { title: 'Rentabilidad ventas' },
      ]}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{
            rango: [dayjs().startOf('month'), dayjs()],
            indContado: true,
            indCredito: true,
            oficinaId: session?.oficinaId ?? 0,
          }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true }]}>
            <CredixRangePicker format="DD/MM/YYYY" style={{ maxWidth: 320 }} />
          </Form.Item>
          <Form.Item name="indContado" valuePropName="checked">
            <Checkbox>Contado</Checkbox>
          </Form.Item>
          <Form.Item name="indCredito" valuePropName="checked">
            <Checkbox>Crédito</Checkbox>
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
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError ? consulta.error.message : 'Error en la consulta'
            }
          />
        ) : null
      }
    >
      <CredixDataTable<RptRentabilidadVentaRow>
        rowKey={(r) => `${r.ordenVentaId}-${r.nro}`}
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Consulte para ver rentabilidad de ventas' }}
      />
    </CredixInformePage>
  )
}
