import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Checkbox, Form, Select } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadListaPrecioInformeCsv,
  downloadListaPrecioInformePdf,
  fetchListaPrecioInforme,
} from '../../api/ventasInformes'
import { fetchMarcas } from '../../api/marcas'
import { ApiError } from '../../api/errors'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { ListaPrecioInformeParams, RptListaPrecioGeneralRow } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'

type FormValues = {
  marcaId?: number
  indDescuento: boolean
  indPuntos: boolean
}

function toParams(v: FormValues): ListaPrecioInformeParams {
  return {
    marcaId: v.marcaId,
    indDescuento: v.indDescuento,
    indPuntos: v.indPuntos,
  }
}

export function ListaPrecioInformePage() {
  const [form] = Form.useForm<FormValues>()

  const marcas = useQuery({
    queryKey: ['marcas'],
    queryFn: fetchMarcas,
  })

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchListaPrecioInforme(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadListaPrecioInformeCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadListaPrecioInformePdf(toParams(v)),
  })

  const stats = useInformeStats(consulta)
  const columns: ColumnsType<RptListaPrecioGeneralRow> = [
    { title: 'Id', dataIndex: 'articuloId', width: 70 },
    { title: 'Tipo', dataIndex: 'tipoArticulo', width: 100, ellipsis: true },
    { title: 'Artículo', dataIndex: 'articuloDes', ellipsis: true },
    {
      title: 'Precio',
      dataIndex: 'monto',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Descuento',
      dataIndex: 'descuento',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Puntos', dataIndex: 'puntosCanje', width: 70 },
  ]

  const marcaOptions =
    marcas.data?.map((m) => ({
      value: m.marcaId,
      label: m.denominacion,
    })) ?? []

  return (
    <CredixInformePage
      title="Informe lista de precios"
      subtitle="Consulta de precios por marca y filtros de descuento o puntos."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/ventas">Ventas</Link> },
        { title: 'Informe lista de precios' },
      ]}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="vertical"
          initialValues={{ indDescuento: false, indPuntos: false }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="marcaId" label="Marca (opcional)">
            <Select
              allowClear
              showSearch
              placeholder="Todas las marcas"
              options={marcaOptions}
              optionFilterProp="label"
              style={{ maxWidth: 320 }}
            />
          </Form.Item>
          <Form.Item name="indDescuento" valuePropName="checked">
            <Checkbox>Solo con descuento</Checkbox>
          </Form.Item>
          <Form.Item name="indPuntos" valuePropName="checked">
            <Checkbox>Solo con puntos canje</Checkbox>
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
      <CredixDataTable<RptListaPrecioGeneralRow>
        rowKey="articuloId"
        size="small"
        scroll={{ x: 800 }}
        loading={consulta.isPending}
        dataSource={consulta.data ?? []}
        columns={columns}
        pagination={{ pageSize: 25, showSizeChanger: true }}
        locale={{ emptyText: 'Consulte para ver precios' }}
      />
    </CredixInformePage>
  )
}
