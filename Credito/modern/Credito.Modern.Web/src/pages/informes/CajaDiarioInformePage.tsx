import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber } from 'antd'
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
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'

import { useInformeStats } from '../../hooks/useInformeStats'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import type { CajaDiarioInformeParams, RptCajaDiarioRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

type FormValues = {
  oficinaId: number
  rango: [Dayjs, Dayjs]
  usuarioId?: number
}

function toParams(v: FormValues): CajaDiarioInformeParams {
  const p: CajaDiarioInformeParams = {
    oficinaId: v.oficinaId,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
  if (v.usuarioId != null && v.usuarioId > 0) {
    p.usuarioId = v.usuarioId
  }
  return p
}

export function CajaDiarioInformePage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    rango: [dayjs().startOf('month'), dayjs()],
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
  }

  useEffect(() => {
    if (!session) return
    form.setFieldsValue({
      oficinaId: session.oficinaId,
      usuarioId: puedeElegirGestor ? form.getFieldValue('usuarioId') : session.usuarioId,
    })
  }, [session, form, puedeElegirGestor])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCajaDiarioInforme(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCajaDiarioInformeCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCajaDiarioInformePdf(toParams(v)),
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
      subtitle="Sesiones de caja diario por oficina y rango; gestor opcional (TODOS) para roles elevados."
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
          initialValues={defaultValues}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true, message: 'Indique el rango' }]}>
            <CredixRangePicker format="DD/MM/YYYY" />
          </Form.Item>
          {puedeElegirGestor ? (
            <Form.Item name="usuarioId" label="Gestor">
              <GestorSelect allowAll legacyList size="middle" />
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
