import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCreditoCondonadoCsv,
  downloadCreditoCondonadoPdf,
  fetchCreditoCondonado,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'

import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { CreditoCondonadoParams, RptCreditoCondonadoRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'


type FormValues = {
  oficinaId: number
  usuarioId: number
  rango: [Dayjs, Dayjs]
}

function mesCalendarioActual(): [Dayjs, Dayjs] {
  return [dayjs().startOf('month'), dayjs().endOf('month')]
}

function toParams(v: FormValues): CreditoCondonadoParams {
  return {
    oficinaId: v.oficinaId,
    usuarioId: v.usuarioId,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
}

export function CreditoCondonadoPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    usuarioId: session?.usuarioId ?? 0,
    rango: mesCalendarioActual(),
  }

  useEffect(() => {
    if (session) {
      form.setFieldsValue({
        oficinaId: session.oficinaId,
        usuarioId: session.usuarioId,
      })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCreditoCondonado(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoCondonadoCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoCondonadoPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptCreditoCondonadoRow> = [
    { title: 'Oficina', dataIndex: 'oficina', width: 110, ellipsis: true },
    { title: 'Crédito', dataIndex: 'creditoId', width: 80 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    { title: 'Agente', dataIndex: 'agente', width: 120, ellipsis: true },
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
      title: 'Monto crédito',
      dataIndex: 'montoCredito',
      width: 105,
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
      title: 'Monto condonado',
      dataIndex: 'montoCondonado',
      width: 115,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Observación', dataIndex: 'observacion', ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Créditos condonados"
      subtitle="Créditos pagados con condonación en el periodo; por defecto el mes calendario actual."
      breadcrumb={reportesCreditoBreadcrumb('Créditos condonados')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={defaultValues}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="usuarioId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item
            name="rango"
            rules={[
              { required: true, message: 'Indique el periodo' },
              {
                validator: (_, value: [Dayjs, Dayjs] | undefined) => {
                  if (!value?.[0] || !value[1]) {
                    return Promise.resolve()
                  }
                  if (value[1].isBefore(value[0], 'day')) {
                    return Promise.reject(
                      new Error('La fecha final no puede ser anterior a la inicial'),
                    )
                  }
                  return Promise.resolve()
                },
              },
            ]}
          >
            <CredixRangePicker format="DD/MM/YYYY" allowClear={false} />
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
      <CredixDataTable<RptCreditoCondonadoRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 15 }}
        locale={{ emptyText: 'Ejecute Consultar para cargar datos' }}
      />
    </CredixInformePage>
  )
}
