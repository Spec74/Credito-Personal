import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Form, InputNumber, message } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadSaldoCarteraCsv,
  downloadSaldoCarteraPdf,
  fetchSaldoCartera,
  type SaldoCarteraParams,
} from '../../api/credito'
import { useAuth } from '../../auth/useAuth'
import { ApiError } from '../../api/errors'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { ListarSaldoCarteraRow } from '../../types/api'

const now = new Date()

function formatMoney(value: number | null | undefined): string {
  if (value == null) {
    return '—'
  }
  return value.toLocaleString('es-PE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
}

export function SaldoCarteraPage() {
  const { session } = useAuth()
  const [rows, setRows] = useState<ListarSaldoCarteraRow[]>([])
  const [form] = Form.useForm<SaldoCarteraParams>()

  const defaultParams: SaldoCarteraParams = {
    anio: now.getFullYear(),
    mes: now.getMonth() + 1,
    oficinaId: session?.oficinaId ?? 1,
    usuarioId: session?.usuarioId ?? 1,
  }

  useEffect(() => {
    if (session) {
      form.setFieldsValue({
        oficinaId: session.oficinaId,
        usuarioId: session.usuarioId,
      })
    }
  }, [session, form])

  useEffect(() => {
    if (session) {
      form.setFieldsValue({
        oficinaId: session.oficinaId,
        usuarioId: session.usuarioId,
      })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: fetchSaldoCartera,
    onSuccess: (data) => {
      setRows(data)
      message.success(`${data.length} fila(s)`)
    },
    onError: (err) => {
      const msg = err instanceof ApiError ? err.message : 'Error al consultar'
      message.error(msg)
    },
  })

  const descargarCsv = useMutation({
    mutationFn: downloadSaldoCarteraCsv,
    onSuccess: () => message.success('CSV descargado'),
    onError: (err) =>
      message.error(err instanceof ApiError ? err.message : 'Error CSV'),
  })

  const descargarPdf = useMutation({
    mutationFn: downloadSaldoCarteraPdf,
    onSuccess: () => message.success('PDF descargado'),
    onError: (err) =>
      message.error(err instanceof ApiError ? err.message : 'Error PDF'),
  })

  const onConsultar = async () => {
    const values = await form.validateFields()
    consulta.mutate(values)
  }

  const getParams = async (): Promise<SaldoCarteraParams> => form.validateFields()

  const stats = useInformeStats({ ...consulta, data: rows }, session?.oficinaId)
  const columns: ColumnsType<ListarSaldoCarteraRow> = [
    { title: 'Agente', dataIndex: 'agenteId', width: 90 },
    { title: 'Oficina', dataIndex: 'oficinaId', width: 90 },
    {
      title: 'N° desembolsos',
      dataIndex: 'nroDesembolsos',
      width: 110,
      align: 'right',
    },
    {
      title: 'Desembolsos',
      dataIndex: 'montoDesembolsos',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo cartera',
      dataIndex: 'saldoCartera',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cli. cartera',
      dataIndex: 'nroClientesSaldoCartera',
      width: 100,
      align: 'right',
    },
    {
      title: 'Saldo mora',
      dataIndex: 'saldoMoraCartera',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cli. mora',
      dataIndex: 'nroClientesSaldoMoraCartera',
      width: 90,
      align: 'right',
    },
    {
      title: 'Saldo vencido',
      dataIndex: 'saldoVencido',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Morosidad',
      dataIndex: 'saldoMorosidad',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cli. nuevos',
      dataIndex: 'nroClientesNuevos',
      width: 95,
      align: 'right',
    },
    {
      title: 'Cierre',
      dataIndex: 'fechaCierre',
      width: 160,
      render: (v: string | null) => v?.replace('T', ' ').slice(0, 19) ?? '—',
    },
  ]

  return (
    <CredixInformePage
      title="Saldo de cartera"
      subtitle="Saldos de cartera por agente y oficina en el período indicado."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Saldo cartera' },
      ]}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={defaultParams}
          onFinish={() => void onConsultar()}
        >
          <Form.Item
            name="anio"
            label="Año"
            rules={[{ required: true, type: 'number', min: 1900, max: 2100 }]}
          >
            <InputNumber />
          </Form.Item>
          <Form.Item
            name="mes"
            label="Mes"
            rules={[{ required: true, type: 'number', min: 1, max: 12 }]}
          >
            <InputNumber />
          </Form.Item>
          <Form.Item name="oficinaId" label="Oficina" rules={[{ required: true }]}>
            <InputNumber disabled />
          </Form.Item>
          <Form.Item name="usuarioId" label="Usuario" rules={[{ required: true }]}>
            <InputNumber disabled />
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
          csvLoading={descargarCsv.isPending}
          pdfLoading={descargarPdf.isPending}
          onCsv={async () => descargarCsv.mutate(await getParams())}
          onPdfTabular={async () => descargarPdf.mutate(await getParams())}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError
                ? consulta.error.message
                : 'Error en la consulta'
            }
          />
        ) : null
      }
    >
      <CredixDataTable<ListarSaldoCarteraRow>
        rowKey={(r) => `${r.agenteId}-${r.oficinaId}-${r.fechaCierre ?? ''}`}
        columns={columns}
        dataSource={rows}
        loading={consulta.isPending}
        pagination={{ pageSize: 15, showSizeChanger: true }}
        locale={{ emptyText: 'Ejecute Consultar para cargar datos' }}
        scroll={{ x: 1200 }}
      />
    </CredixInformePage>
  )
}
