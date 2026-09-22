import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber, Select } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadGenerarKardexCsv,
  downloadGenerarKardexPdf,
  fetchGenerarKardex,
} from '../../api/almacen'
import { fetchAlmacenes } from '../../api/almacenes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { GenerarKardexParams, GenerarKardexRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

type FormValues = {
  oficinaId: number
  articuloId: number
  almacenId: number
}

function toParams(v: FormValues): GenerarKardexParams {
  return {
    oficinaId: v.oficinaId,
    articuloId: v.articuloId,
    almacenId: v.almacenId,
  }
}

export function KardexPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()
  const oficinaId = session?.oficinaId ?? 0

  useEffect(() => {
    if (session) {
      form.setFieldsValue({ oficinaId: session.oficinaId })
    }
  }, [session, form])

  const almacenes = useQuery({
    queryKey: ['almacenes', oficinaId],
    queryFn: () => fetchAlmacenes(oficinaId),
    enabled: oficinaId > 0,
  })

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchGenerarKardex(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadGenerarKardexCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadGenerarKardexPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<GenerarKardexRow> = [
    { title: 'Det.', dataIndex: 'movimientoDetId', width: 60 },
    {
      title: 'Fecha',
      dataIndex: 'fecha',
      width: 100,
      render: formatFecha,
    },
    { title: 'Concepto', dataIndex: 'concepto', ellipsis: true },
    { title: 'Cant. ent.', dataIndex: 'cantEnt', width: 75 },
    {
      title: 'PU ent.',
      dataIndex: 'puEnt',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total ent.',
      dataIndex: 'totalEnt',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Cant. sal.', dataIndex: 'cantSal', width: 75 },
    {
      title: 'PU sal.',
      dataIndex: 'puSal',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total sal.',
      dataIndex: 'totalSal',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Saldo cant.', dataIndex: 'cantSaldo', width: 85 },
    {
      title: 'PU saldo',
      dataIndex: 'puSaldo',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo total',
      dataIndex: 'totalSaldo',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
  ]

  const almacenOptions =
    almacenes.data?.map((a) => ({
      value: a.almacenId,
      label: a.denominacion,
    })) ?? []

  return (
    <CredixInformePage
      title="Kardex de artículo"
      subtitle="Movimientos de entrada y salida por artículo y almacén."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/almacen">Almacén</Link> },
        { title: 'Kardex' },
      ]}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="vertical"
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" label="Oficina" rules={[{ required: true }]}>
            <InputNumber disabled style={{ width: 120 }} />
          </Form.Item>
          <Form.Item
            name="almacenId"
            label="Almacén"
            rules={[{ required: true, message: 'Seleccione almacén' }]}
          >
            <Select
              showSearch
              placeholder="Almacén"
              options={almacenOptions}
              optionFilterProp="label"
              style={{ maxWidth: 360 }}
            />
          </Form.Item>
          <Form.Item
            name="articuloId"
            label="Artículo id"
            rules={[{ required: true, message: 'Indique artículo' }]}
          >
            <InputNumber min={1} style={{ width: 140 }} />
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
            message="Error al consultar"
            description={
              consulta.error instanceof ApiError
                ? consulta.error.message
                : 'Error desconocido'
            }
            style={{ marginBottom: 16 }}
          />
        ) : null
      }
    >
      <CredixDataTable<GenerarKardexRow>
        rowKey={(r, i) => String(r.movimientoDetId ?? i)}
        size="small"
        scroll={{ x: 1200 }}
        loading={consulta.isPending}
        dataSource={consulta.data ?? []}
        columns={columns}
        pagination={{ pageSize: 25, showSizeChanger: true }}
        locale={{ emptyText: 'Indique artículo y almacén' }}
      />
    </CredixInformePage>
  )
}
