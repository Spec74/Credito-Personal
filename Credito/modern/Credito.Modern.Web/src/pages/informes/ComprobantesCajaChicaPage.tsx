import { useMemo } from 'react'
import { useMutation } from '@tanstack/react-query'
import { CalendarOutlined, SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadComprobantesCajaChicaCsv,
  downloadComprobantesCajaChicaPdf,
  fetchComprobantesCajaChica,
} from '../../api/creditoPlanes'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'

import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type {
  ComprobantesCajaChicaParams,
  RptComprobantesCajaChicaRow,
} from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

type FormValues = {
  rango: [Dayjs, Dayjs]
}

function toParams(v: FormValues): ComprobantesCajaChicaParams {
  return {
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
}

export function ComprobantesCajaChicaPage() {
  const [form] = Form.useForm<FormValues>()

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchComprobantesCajaChica(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadComprobantesCajaChicaCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadComprobantesCajaChicaPdf(toParams(v)),
  })

  const totalImporte = useMemo(
    () => (consulta.data ?? []).reduce((s, r) => s + (r.importe ?? 0), 0),
    [consulta.data],
  )

  const stats = useInformeStats(consulta, undefined, [
    {
      value: consulta.isSuccess ? formatMoney(totalImporte) : '—',
      label: 'Importe total',
      tone: 'red',
    },
  ])

  const columns: ColumnsType<RptComprobantesCajaChicaRow> = useMemo(
    () => [
      { title: 'Gasto', dataIndex: 'gasto', width: 140, ellipsis: true },
      { title: 'Fecha', dataIndex: 'fecha', width: 100, render: formatFecha },
      { title: 'Documento', dataIndex: 'documento', width: 110 },
      { title: 'Serie', dataIndex: 'serie', width: 70 },
      { title: 'Número', dataIndex: 'numero', width: 90 },
      { title: 'RUC', dataIndex: 'ruc', width: 110 },
      { title: 'Razón social', dataIndex: 'razonSocial', ellipsis: true },
      { title: 'Detalle', dataIndex: 'detalleGasto', ellipsis: true },
      {
        title: 'Importe',
        dataIndex: 'importe',
        width: 95,
        align: 'right',
        render: (v: number) => (
          <span className="comprobantes-caja-chica-table__importe">{formatMoney(v)}</span>
        ),
      },
    ],
    [],
  )

  const ejecutarConsulta = () => {
    void form.validateFields().then((v) => consulta.mutate(v))
  }

  return (
    <CredixInformePage
      className="comprobantes-caja-chica-page credix-page--stats-3"
      title="Comprobantes de caja chica"
      subtitle="Rendiciones cerradas en el rango de fechas — paridad ReporteComprobantesCajaChica (MVC)."
      panelTitle="COMPROBANTES RENDIDOS"
      breadcrumb={reportesCreditoBreadcrumb('Comprobantes rendidos')}
      stats={stats}
      tableSearchVariant="prominent"
      searchPlaceholder="Gasto, RUC, razón social, serie, número"
      tableResultCount={consulta.data?.length}
      filters={
        <Form
          form={form}
          layout="inline"
          className="comprobantes-caja-chica-filters"
          initialValues={{ rango: [dayjs().startOf('month'), dayjs()] }}
          onFinish={ejecutarConsulta}
        >
          <Form.Item
            name="rango"
            rules={[{ required: true, message: 'Seleccione fechas' }]}
          >
            <CredixRangePicker
              format="DD/MM/YYYY"
              style={{ maxWidth: 360 }}
              suffixIcon={<CalendarOutlined />}
            />
          </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              icon={<SearchOutlined />}
              loading={consulta.isPending}
            >
              Consultar
            </Button>
          </Form.Item>
        </Form>
      }
      exportBar={
        <InformeExportBar
          onCsv={() => {
            void form.validateFields().then((v) => csv.mutate(v))
          }}
          onPdfTabular={() => {
            void form.validateFields().then((v) => pdf.mutate(v))
          }}
          csvLoading={csv.isPending}
          pdfLoading={pdf.isPending}
          csvDisabled={!consulta.data?.length}
          pdfDisabled={!consulta.data?.length}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 12 }}
            message="No se pudo cargar el informe"
          />
        ) : null
      }
    >
      <p className="credix-module-banner credix-module-banner--spaced">
        <strong>Paridad MVC:</strong> rango de fechas + reporte RDLC legacy · en modern: tabla con
        búsqueda en resultados, CSV y PDF tabular con los mismos datos.
      </p>

      {!consulta.isSuccess && !consulta.isPending ? (
        <Text type="secondary">Seleccione fechas y pulse Consultar.</Text>
      ) : null}

      <CredixDataTable<RptComprobantesCajaChicaRow>
        className="comprobantes-caja-chica-table"
        rowKey={(r) => `${r.fecha}-${r.serie}-${r.numero}-${r.importe}-${r.ruc}`}
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        scroll={{ x: 'max-content' }}
        pagination={{ pageSize: 25, showSizeChanger: true, showTotal: (t) => `${t} comprobante(s)` }}
        locale={{
          emptyText: consulta.isSuccess
            ? 'Sin comprobantes en el rango indicado'
            : 'Consulte para ver comprobantes rendidos',
        }}
      />
    </CredixInformePage>
  )
}
