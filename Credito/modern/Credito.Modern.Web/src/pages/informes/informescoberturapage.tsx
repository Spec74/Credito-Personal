import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { fetchCatalogoCobertura, type ReporteCoberturaItem } from '../../api/reportes'
import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'

const { Text } = Typography

const nivelColor: Record<string, string> = {
  'completo-datos': 'green',
  parcial: 'blue',
  json: 'cyan',
  'json-texto': 'purple',
  'solo-mvc': 'orange',
  'vista-indice': 'default',
}

const columns: ColumnsType<ReporteCoberturaItem> = [
  {
    title: 'Nivel',
    dataIndex: 'nivelCobertura',
    width: 130,
    render: (n: string) => <Tag color={nivelColor[n] ?? 'default'}>{n}</Tag>,
  },
  { title: 'Informe', dataIndex: 'nombre', ellipsis: true },
  { title: 'Área', dataIndex: 'area', width: 90 },
  {
    title: 'API',
    key: 'api',
    width: 200,
    render: (_, r) =>
      r.pdfEndpoint ? (
        <Text code style={{ fontSize: 11 }}>
          {r.pdfEndpoint.replace('/api/v1', '')}
        </Text>
      ) : (
        <Text type="secondary">—</Text>
      ),
  },
  { title: 'Nota', dataIndex: 'nota', ellipsis: true },
]

function CoberturaTable({
  title,
  rows,
  loading,
}: {
  title: string
  rows: ReporteCoberturaItem[]
  loading: boolean
}) {
  return (
    <CredixPanel title={title}>
      <CredixDataTable<ReporteCoberturaItem>
        rowKey="catalogoId"
        loading={loading}
        columns={columns}
        dataSource={rows}
        pagination={{ pageSize: 12 }}
      />
    </CredixPanel>
  )
}

export function InformesCoberturaPage() {
  const query = useQuery({
    queryKey: ['reportes', 'catalogo-cobertura'],
    queryFn: fetchCatalogoCobertura,
  })

  const soloMvc = (query.data?.items ?? []).filter((i) => i.nivelCobertura === 'solo-mvc')

  const stats: CredixStatItem[] = [
    { value: query.data?.totalCatalogo ?? '—', label: 'Catálogo MVC' },
    {
      value: query.data?.completoDatosJsonCsvPdf ?? '—',
      label: 'JSON + CSV + PDF',
      tone: 'green',
    },
    { value: query.data?.soloMvc ?? '—', label: 'Solo MVC (RDLC)', tone: 'red' },
    { value: query.data?.parcial ?? '—', label: 'Parcial' },
  ]

  return (
    <CredixPage
      title="Cobertura de informes"
      subtitle="Matriz catálogo MVC vs API moderna. Exporte con Datos CSV y PDF tabla en cada pantalla."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/informes">Informes</Link> },
        { title: 'Cobertura' },
      ]}
      stats={stats}
    >
      <CoberturaTable
        title="Solo diseño RDLC / ticket (pendiente)"
        rows={soloMvc}
        loading={query.isLoading}
      />
      <CoberturaTable
        title="Catálogo completo"
        rows={query.data?.items ?? []}
        loading={query.isLoading}
      />
      <CoberturaTable
        title="Informes adicionales (solo API)"
        rows={query.data?.informesAdicionalesApi ?? []}
        loading={query.isLoading}
      />
    </CredixPage>
  )
}
