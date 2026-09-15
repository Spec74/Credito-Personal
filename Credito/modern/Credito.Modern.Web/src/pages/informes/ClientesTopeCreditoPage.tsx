import { useEffect, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Form, InputNumber, Typography } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import {
  downloadClientesTopeCreditoCsv,
  downloadClientesTopeCreditoPdf,
  fetchClientesTopeCredito,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildClientesTopeCreditoInformeColumns } from '../../config/clientesTopeCreditoInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { RptClientesTopeCreditoRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import {
  gestorLabelFromId,
  toGestorInformeParams,
} from '../../utils/gestorInformeForm'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import { readUrlUserId } from '../../utils/informeUrlParams'

const TOPE_COLUMNS = buildClientesTopeCreditoInformeColumns()

type FormValues = {
  oficinaId: number
  usuarioId?: number
}

export function ClientesTopeCreditoPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      return fetchClientesTopeCredito(
        toGestorInformeParams(session.oficinaId, v.usuarioId),
      )
    },
  })

  useEffect(() => {
    if (!session) return
    const uid = readUrlUserId(searchParams)
    const next: FormValues = {
      oficinaId: session.oficinaId,
      usuarioId:
        uid != null && uid > 0 ? uid : puedeElegirGestor ? undefined : session.usuarioId,
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('usuarioId') ||
      searchParams.has('pUsuarioId') ||
      searchParams.has('oficinaId') ||
      searchParams.has('pOficinaId')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, searchParams.toString()])

  const csv = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      await downloadClientesTopeCreditoCsv(
        toGestorInformeParams(session.oficinaId, v.usuarioId),
      )
    },
  })

  const pdf = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      await downloadClientesTopeCreditoPdf(
        toGestorInformeParams(session.oficinaId, v.usuarioId),
      )
    },
  })

  const filas = useMemo(() => consulta.data ?? [], [consulta.data])
  const queried = consulta.isSuccess || consulta.isError
  const exportDisabled = !consulta.isSuccess || filas.length === 0
  const stats = useInformeStats(consulta, session?.oficinaId)
  const gestorId = form.getFieldValue('usuarioId') as number | undefined

  return (
    <CredixInformePage
      title="Clientes con tope de crédito"
      subtitle={
        <>
          Paridad <strong>Reporte → Crédito → Clientes tope crédito</strong> y PDF/XLS{' '}
          <em>rptClienteTopeCredito</em>. Gestor opcional (TODOS); oficina = sesión.
        </>
      }
      breadcrumb={reportesCreditoBreadcrumb('Clientes tope crédito')}
      stats={stats}
      panelTitle="CLIENTES TOPE CRÉDITO"
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por DNI, cliente, agente o nota…"
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
          Fecha {formatFecha(dayjs().format('YYYY-MM-DD'))} · Oficina sesión #{session?.oficinaId}{' '}
          · {gestorLabelFromId(gestorId)}
        </Typography.Text>
      ) : null}

      <CredixDataTable<RptClientesTopeCreditoRow>
        rowKey={(r) => `${r.numeroDocumento}-${r.cliente ?? ''}`}
        columns={TOPE_COLUMNS}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{ pageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        locale={{ emptyText: 'Pulse Consultar (mismos filtros que el reporte legacy).' }}
      />
    </CredixInformePage>
  )
}
