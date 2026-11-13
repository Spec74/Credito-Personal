import { useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, Select } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCreditoRentabilidadCsv,
  downloadCreditoRentabilidadPdf,
  fetchCreditoRentabilidad,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'

import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { CreditoRentabilidadParams, RptCreditoRentabilidadRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { CREDITO_ESTADO_REPORTE_OPTIONS } from '../../utils/creditoEstados'
import { OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { useEsCentral } from '../../hooks/useEsCentral'
import { readUrlDay, resolveInformeOficinaId } from '../../utils/informeUrlParams'


type FormValues = {
  oficinaId: number
  estadoCredito: string
  rango: [Dayjs, Dayjs]
}

function toParams(v: FormValues): CreditoRentabilidadParams {
  return {
    oficinaId: v.oficinaId,
    estadoCredito: v.estadoCredito,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
}

export function CreditoRentabilidadPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  const { esCentral } = useEsCentral()
  const [searchParams] = useSearchParams()

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCreditoRentabilidad(toParams(v)),
  })

  useEffect(() => {
    if (!session) return
    const ini = readUrlDay(searchParams, 'pFechaIni', 'fechaIni')
    const fin = readUrlDay(searchParams, 'pFechaFin', 'fechaFin')
    const estado = searchParams.get('estadoCredito')
    const next: FormValues = {
      oficinaId: resolveInformeOficinaId(searchParams, session.oficinaId),
      estadoCredito: estado && estado.length > 0 ? estado : 'DES',
      rango: [ini ?? dayjs().startOf('month'), fin ?? dayjs()],
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('fechaIni') ||
      searchParams.has('pFechaIni') ||
      searchParams.has('estadoCredito') ||
      searchParams.has('oficinaId') ||
      searchParams.has('pOficinaId')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, form, searchParams.toString()])

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoRentabilidadCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoRentabilidadPdf(toParams(v)),
  })

  const oficinaWatch = Form.useWatch('oficinaId', form)
  const stats = useInformeStats(consulta, oficinaWatch ?? session?.oficinaId)
  const columns: ColumnsType<RptCreditoRentabilidadRow> = [
    { title: 'Crédito', dataIndex: 'creditoId', width: 75, fixed: 'left' },
    { title: 'Oficina', dataIndex: 'oficina', width: 90, ellipsis: true },
    { title: 'Código', dataIndex: 'codigo', width: 80 },
    { title: 'Cliente', dataIndex: 'cliente', width: 140, ellipsis: true },
    {
      title: 'Desembolso',
      dataIndex: 'fechaDesembolso',
      width: 100,
      render: formatFecha,
    },
    {
      title: 'Pago',
      dataIndex: 'fechaPago',
      width: 100,
      render: formatFecha,
    },
    { title: 'Cuotas', dataIndex: 'numeroCuotas', width: 65 },
    { title: 'Forma pago', dataIndex: 'formaPago', width: 90 },
    { title: 'Estado', dataIndex: 'estado', width: 80 },
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
      title: 'Σ cuota',
      dataIndex: 'sumCuota',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Cuotas pag.', dataIndex: 'cuotasPagadas', width: 85 },
    {
      title: 'Gastos adm.',
      dataIndex: 'montoGastosAdm',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Σ interés',
      dataIndex: 'sumInteres',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Σ mora',
      dataIndex: 'sumMora',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Σ pago',
      dataIndex: 'sumPago',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
  ]

  return (
    <CredixInformePage
      title="Rentabilidad de créditos"
      subtitle={undefined}
      breadcrumb={reportesCreditoBreadcrumb('Rentabilidad créditos')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{
            oficinaId: session?.oficinaId ?? 0,
            estadoCredito: 'DES',
            rango: [dayjs().startOf('month'), dayjs()],
          }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" label="Oficina">
            <OficinaSelect disabled={!esCentral} size="middle" />
          </Form.Item>
          <Form.Item name="estadoCredito" rules={[{ required: true }]}>
            <Select options={CREDITO_ESTADO_REPORTE_OPTIONS} style={{ width: 200 }} />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true }]}>
            <CredixRangePicker format="DD/MM/YYYY" />
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
      <CredixDataTable<RptCreditoRentabilidadRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Consulte para ver rentabilidad' }}
      />
    </CredixInformePage>
  )
}
