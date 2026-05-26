import { useEffect, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, DatePicker, Form, InputNumber, Typography } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import {
  downloadClientesNuevosMesCsv,
  downloadClientesNuevosMesPdf,
  fetchClientesNuevosMes,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import type { CredixStatItem } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildClientesNuevosMesInformeColumns } from '../../config/clientesNuevosMesInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { RptCreditoObservadoRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import { readUrlDay, readUrlOfficeId, readUrlUserId } from '../../utils/informeUrlParams'
import {
  gestorLabelFromId,
  toClientesNuevosMesParams,
} from '../../utils/gestorInformeForm'

const { RangePicker } = DatePicker

const NUEVOS_COLUMNS = buildClientesNuevosMesInformeColumns()

type FormValues = {
  oficinaId: number
  usuarioId?: number
  rango: [Dayjs, Dayjs]
}

function mesCalendarioActual(): [Dayjs, Dayjs] {
  return [dayjs().startOf('month'), dayjs().endOf('month')]
}

export function ClientesNuevosMesPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
    rango: mesCalendarioActual(),
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      return fetchClientesNuevosMes(
        toClientesNuevosMesParams(
          session.oficinaId,
          v.usuarioId,
          v.rango[0].format('YYYY-MM-DD'),
          v.rango[1].format('YYYY-MM-DD'),
        ),
      )
    },
  })

  useEffect(() => {
    if (!session) return
    const ini = readUrlDay(searchParams, 'pFechaIni', 'fechaIni')
    const fin = readUrlDay(searchParams, 'pFechaFin', 'fechaFin')
    const uid = readUrlUserId(searchParams)
    const next: FormValues = {
      oficinaId: readUrlOfficeId(searchParams) ?? session.oficinaId,
      usuarioId:
        uid != null && uid > 0 ? uid : puedeElegirGestor ? undefined : session.usuarioId,
      rango: ini && fin ? [ini, fin] : mesCalendarioActual(),
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('pOficinaId') ||
      searchParams.has('oficinaId') ||
      searchParams.has('pFechaIni') ||
      searchParams.has('fechaIni') ||
      searchParams.has('usuarioId') ||
      searchParams.has('pUsuarioId')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, searchParams.toString()])

  const csv = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      await downloadClientesNuevosMesCsv(
        toClientesNuevosMesParams(
          session.oficinaId,
          v.usuarioId,
          v.rango[0].format('YYYY-MM-DD'),
          v.rango[1].format('YYYY-MM-DD'),
        ),
      )
    },
  })

  const pdf = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      await downloadClientesNuevosMesPdf(
        toClientesNuevosMesParams(
          session.oficinaId,
          v.usuarioId,
          v.rango[0].format('YYYY-MM-DD'),
          v.rango[1].format('YYYY-MM-DD'),
        ),
      )
    },
  })

  const filas = consulta.data ?? []
  const queried = consulta.isSuccess || consulta.isError
  const exportDisabled = !consulta.isSuccess || filas.length === 0

  const statsExtras = useMemo((): CredixStatItem[] => {
    if (!consulta.isSuccess || filas.length === 0) {
      return []
    }
    const total = filas.reduce((s, r) => s + r.montoCredito, 0)
    return [{ value: formatMoney(total), label: 'Σ monto crédito', tone: 'green' }]
  }, [consulta.isSuccess, filas])

  const stats = useInformeStats(consulta, session?.oficinaId, statsExtras)

  const gestorId = form.getFieldValue('usuarioId') as number | undefined
  const rango = form.getFieldValue('rango') as [Dayjs, Dayjs] | undefined

  return (
    <CredixInformePage
      title="Clientes nuevos del mes"
      subtitle={
        <>
          Paridad <strong>Reporte → Crédito → Clientes nuevos</strong> y PDF/XLS{' '}
          <em>rptCreditoObservado</em>. Mismo detalle que créditos observados; periodo por defecto = mes
          calendario.
        </>
      }
      breadcrumb={reportesCreditoBreadcrumb('Clientes nuevos del mes')}
      stats={stats}
      panelTitle="CLIENTES NUEVOS"
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por crédito, cliente, agente u observación…"
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
          <Form.Item name="usuarioId" label="Gestor">
            <GestorSelect allowAll={puedeElegirGestor} legacyList size="middle" />
          </Form.Item>
          <Form.Item
            name="rango"
            label="Periodo"
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
            <RangePicker format="DD/MM/YYYY" allowClear={false} />
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
          csvDisabled={exportDisabled}
          pdfDisabled={exportDisabled}
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
      {queried && consulta.isSuccess ? (
        <Typography.Text type="secondary" className="credix-aprobacion-filtros-aplicados">
          {rango?.[0] && rango[1]
            ? `${formatFecha(rango[0].format('YYYY-MM-DD'))} – ${formatFecha(rango[1].format('YYYY-MM-DD'))}`
            : null}{' '}
          · Oficina sesión #{session?.oficinaId} · {gestorLabelFromId(gestorId)}
        </Typography.Text>
      ) : null}

      <CredixDataTable<RptCreditoObservadoRow>
        rowKey="creditoId"
        columns={NUEVOS_COLUMNS}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{ pageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        locale={{ emptyText: 'Pulse Consultar (mismos filtros que el reporte legacy).' }}
      />
    </CredixInformePage>
  )
}
