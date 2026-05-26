import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Checkbox, DatePicker, Form, InputNumber } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCajaDiarioInformeCsv,
  downloadCajaDiarioInformePdf,
  fetchCajaDiarioInforme,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { CajaDiarioInformeParams, RptCajaDiarioRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { RangePicker } = DatePicker

type FormValues = {
  oficinaId: number
  rango: [Dayjs, Dayjs]
  soloMiGestor: boolean
}

function toParams(v: FormValues, usuarioId?: number): CajaDiarioInformeParams {
  const p: CajaDiarioInformeParams = {
    oficinaId: v.oficinaId,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
  if (v.soloMiGestor && usuarioId) {
    p.usuarioId = usuarioId
  }
  return p
}

export function CajaDiarioInformePage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  useEffect(() => {
    if (session) {
      form.setFieldsValue({ oficinaId: session.oficinaId })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) =>
      fetchCajaDiarioInforme(toParams(v, session?.usuarioId)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) =>
      downloadCajaDiarioInformeCsv(toParams(v, session?.usuarioId)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) =>
      downloadCajaDiarioInformePdf(toParams(v, session?.usuarioId)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptCajaDiarioRow> = [
    { title: 'Id', dataIndex: 'cajaDiarioId', width: 70 },
    { title: 'Oficina', dataIndex: 'oficina', width: 100, ellipsis: true },
    { title: 'Caja', dataIndex: 'caja', width: 100, ellipsis: true },
    { title: 'Agente', dataIndex: 'agente', width: 120, ellipsis: true },
    {
      title: 'Saldo ini.',
      dataIndex: 'saldoInicial',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Entradas',
      dataIndex: 'entradas',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Salidas',
      dataIndex: 'salidas',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo final',
      dataIndex: 'saldoFinal',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Inicio',
      dataIndex: 'fechaIniOperacion',
      width: 100,
      render: formatFecha,
    },
    {
      title: 'Fin',
      dataIndex: 'fechaFinOperacion',
      width: 100,
      render: formatFecha,
    },
  ]

  return (
    <CredixInformePage
      title="Informe caja diario"
      subtitle="Sesiones de caja diario por oficina y rango; sin gestor lista todas las cajas."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/informes">Informes</Link> },
        { title: 'Informe caja diario' },
      ]}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{
            oficinaId: session?.oficinaId ?? 0,
            rango: [dayjs().startOf('month'), dayjs()],
            soloMiGestor: false,
          }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true, message: 'Indique el rango' }]}>
            <RangePicker format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item name="soloMiGestor" valuePropName="checked">
            <Checkbox>Solo mi gestión</Checkbox>
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
      <CredixDataTable<RptCajaDiarioRow>
        rowKey="cajaDiarioId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Consulte para ver caja diario' }}
      />
    </CredixInformePage>
  )
}
