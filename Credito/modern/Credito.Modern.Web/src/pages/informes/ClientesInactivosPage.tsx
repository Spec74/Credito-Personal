import { useEffect, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Checkbox, DatePicker, Form, InputNumber, Typography } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import {
  downloadClientesInactivosCsv,
  downloadClientesInactivosPdf,
  fetchClientesInactivos,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'

import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildClientesInactivosInformeColumns } from '../../config/clientesInactivosInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { RptClientesInactivosRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import {
  gestorLabelFromId,
  toClientesInactivosParams,
} from '../../utils/gestorInformeForm'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import { readUrlDay, readUrlOfficeId, readUrlUserId } from '../../utils/informeUrlParams'

const { RangePicker } = DatePicker

const INACTIVOS_COLUMNS = buildClientesInactivosInformeColumns()

type FormValues = {
  oficinaId: number
  usuarioId?: number
  sinRango: boolean
  rango?: [Dayjs, Dayjs]
}

export function ClientesInactivosPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
    sinRango: true,
    rango: [dayjs().startOf('month'), dayjs()],
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      const fechaIni = v.sinRango ? undefined : v.rango?.[0]?.format('YYYY-MM-DD')
      const fechaFin = v.sinRango ? undefined : v.rango?.[1]?.format('YYYY-MM-DD')
      return fetchClientesInactivos(
        toClientesInactivosParams(session.oficinaId, v.usuarioId, fechaIni, fechaFin),
      )
    },
  })

  useEffect(() => {
    if (!session) return
    const ini = readUrlDay(searchParams, 'pFechaIni', 'fechaIni')
    const fin = readUrlDay(searchParams, 'pFechaFin', 'fechaFin')
    const uid = readUrlUserId(searchParams)
    const sinRango = !ini && !fin
    const next: FormValues = {
      oficinaId: readUrlOfficeId(searchParams) ?? session.oficinaId,
      usuarioId:
        uid != null && uid > 0 ? uid : puedeElegirGestor ? undefined : session.usuarioId,
      sinRango,
      rango: ini && fin ? [ini, fin] : [dayjs().startOf('month'), dayjs()],
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('pOficinaId') ||
      searchParams.has('oficinaId') ||
      searchParams.has('usuarioId') ||
      searchParams.has('pUsuarioId') ||
      searchParams.has('pFechaIni') ||
      searchParams.has('fechaIni') ||
      searchParams.has('sinRango')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, searchParams.toString()])

  const csv = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      const fechaIni = v.sinRango ? undefined : v.rango?.[0]?.format('YYYY-MM-DD')
      const fechaFin = v.sinRango ? undefined : v.rango?.[1]?.format('YYYY-MM-DD')
      await downloadClientesInactivosCsv(
        toClientesInactivosParams(session.oficinaId, v.usuarioId, fechaIni, fechaFin),
      )
    },
  })

  const pdf = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      const fechaIni = v.sinRango ? undefined : v.rango?.[0]?.format('YYYY-MM-DD')
      const fechaFin = v.sinRango ? undefined : v.rango?.[1]?.format('YYYY-MM-DD')
      await downloadClientesInactivosPdf(
        toClientesInactivosParams(session.oficinaId, v.usuarioId, fechaIni, fechaFin),
      )
    },
  })

  const filas = useMemo(() => consulta.data ?? [], [consulta.data])
  const queried = consulta.isSuccess || consulta.isError
  const exportDisabled = !consulta.isSuccess || filas.length === 0
  const sinRango = Form.useWatch('sinRango', form) ?? true

  const stats = useInformeStats(consulta, session?.oficinaId)
  const gestorId = form.getFieldValue('usuarioId') as number | undefined
  const rango = form.getFieldValue('rango') as [Dayjs, Dayjs] | undefined

  return (
    <CredixInformePage
      title="Clientes inactivos"
      subtitle={
        <>
          Paridad <strong>ReporteClientesInactivos</strong> (MVC sin fechas) o con rango en varios
          informes. Marque «Sin rango (legacy)» para omitir <code>fechaIni</code>/
          <code>fechaFin</code>.
        </>
      }
      breadcrumb={reportesCreditoBreadcrumb('Clientes inactivos')}
      stats={stats}
      panelTitle="CLIENTES INACTIVOS"
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por código, DNI, cliente o agente…"
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
          <Form.Item name="sinRango" valuePropName="checked">
            <Checkbox>Sin rango (legacy)</Checkbox>
          </Form.Item>
          {!sinRango ? (
            <Form.Item
              name="rango"
              label="Rango"
              rules={[{ required: true, message: 'Indique el rango de fechas' }]}
            >
              <RangePicker format="DD/MM/YYYY" allowClear={false} />
            </Form.Item>
          ) : null}
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
          {sinRango
            ? 'Sin rango de fechas (paridad MVC)'
            : rango?.[0] && rango[1]
              ? `${formatFecha(rango[0].format('YYYY-MM-DD'))} – ${formatFecha(rango[1].format('YYYY-MM-DD'))}`
              : null}{' '}
          · Oficina sesión #{session?.oficinaId} · {gestorLabelFromId(gestorId)}
        </Typography.Text>
      ) : null}

      <CredixDataTable<RptClientesInactivosRow>
        rowKey="personaId"
        columns={INACTIVOS_COLUMNS}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{ pageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        locale={{ emptyText: 'Pulse Consultar (mismos filtros que el reporte legacy).' }}
      />
    </CredixInformePage>
  )
}
