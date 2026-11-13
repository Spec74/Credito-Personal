import { useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadMovimientoCajaAnuladoCsv,
  downloadMovimientoCajaAnuladoPdf,
  fetchMovimientoCajaAnulado,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'

import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type {
  MovimientoCajaAnuladoParams,
  RptMovimientoCajaAnuladoRow,
} from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { useEsCentral } from '../../hooks/useEsCentral'
import { readUrlDay, resolveInformeOficinaId } from '../../utils/informeUrlParams'


type FormValues = {
  oficinaId: number
  rango: [Dayjs, Dayjs]
}

function toParams(v: FormValues): MovimientoCajaAnuladoParams {
  return {
    oficinaId: v.oficinaId,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
}

export function MovimientoCajaAnuladoPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  const { esCentral } = useEsCentral()
  const [searchParams] = useSearchParams()
  const oficinaWatch = Form.useWatch('oficinaId', form)

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchMovimientoCajaAnulado(toParams(v)),
  })

  useEffect(() => {
    if (!session) return
    const ini = readUrlDay(searchParams, 'pFechaIni', 'fechaIni')
    const fin = readUrlDay(searchParams, 'pFechaFin', 'fechaFin')
    const next: FormValues = {
      oficinaId: resolveInformeOficinaId(searchParams, session.oficinaId),
      rango: [ini ?? dayjs().startOf('month'), fin ?? dayjs()],
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('oficinaId') ||
      searchParams.has('pOficinaId') ||
      searchParams.has('fechaIni') ||
      searchParams.has('pFechaIni')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, form, searchParams.toString()])

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadMovimientoCajaAnuladoCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadMovimientoCajaAnuladoPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, oficinaWatch ?? session?.oficinaId)
  const columns: ColumnsType<RptMovimientoCajaAnuladoRow> = [
    { title: 'Id', dataIndex: 'movimientoCajaId', width: 70 },
    { title: 'Operación', dataIndex: 'operacion', width: 80 },
    {
      title: 'Importe',
      dataIndex: 'importePago',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Persona', dataIndex: 'persona', width: 140, ellipsis: true },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
    {
      title: 'Registro',
      dataIndex: 'fechaReg',
      width: 100,
      render: formatFecha,
    },
    { title: 'Usuario reg.', dataIndex: 'usuarioRegistro', width: 110, ellipsis: true },
    { title: 'Motivo', dataIndex: 'motivoAnulacion', width: 120, ellipsis: true },
    {
      title: 'F. anulación',
      dataIndex: 'fechaAnulacion',
      width: 100,
      render: formatFecha,
    },
    { title: 'Usuario anul.', dataIndex: 'usuarioAnulacion', width: 110, ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Comprobantes de caja anulados"
      subtitle={undefined}
      breadcrumb={reportesCreditoBreadcrumb('Movimientos anulados')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{
            oficinaId: session?.oficinaId ?? 0,
            rango: [dayjs().startOf('month'), dayjs()],
          }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" label="Oficina">
            <OficinaSelect disabled={!esCentral} size="middle" />
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
      <CredixDataTable<RptMovimientoCajaAnuladoRow>
        rowKey="movimientoCajaId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Consulte para ver movimientos anulados' }}
      />
    </CredixInformePage>
  )
}
