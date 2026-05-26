import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Form, InputNumber } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadCajasAsignadasCsv,
  downloadCajasAsignadasPdf,
  fetchCajasAsignadas,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { RptCajasAsignadasRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

type FormValues = { oficinaId: number }

export function CajasAsignadasPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  useEffect(() => {
    if (session?.oficinaId) {
      form.setFieldsValue({ oficinaId: session.oficinaId })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCajasAsignadas(v.oficinaId),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCajasAsignadasCsv(v.oficinaId),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCajasAsignadasPdf(v.oficinaId),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptCajasAsignadasRow> = [
    { title: 'Caja diario', dataIndex: 'cajaDiarioId', width: 90 },
    { title: 'Caja', dataIndex: 'caja', width: 120, ellipsis: true },
    { title: 'Modo', dataIndex: 'modo', width: 80 },
    { title: 'Cajero', dataIndex: 'cajero', width: 140, ellipsis: true },
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
    {
      title: 'Saldo ini.',
      dataIndex: 'saldoInicial',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Entradas',
      dataIndex: 'entradas',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Salidas',
      dataIndex: 'salidas',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo final',
      dataIndex: 'saldoFinal',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Resumen', dataIndex: 'resumen', ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Cajas asignadas (oficina)"
      subtitle="Sesiones de caja diario abiertas o del día en la oficina del usuario en sesión."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/informes">Informes</Link> },
        { title: 'Cajas asignadas' },
      ]}
      stats={stats}
      filters={
        <Form form={form} layout="inline" onFinish={(v) => consulta.mutate(v)}>
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
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
      <CredixDataTable<RptCajasAsignadasRow>
        rowKey="cajaDiarioId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Consulte para ver cajas asignadas' }}
      />
    </CredixInformePage>
  )
}
