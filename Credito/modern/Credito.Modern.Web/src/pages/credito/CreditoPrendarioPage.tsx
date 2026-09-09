import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { PlusOutlined, SearchOutlined } from '@ant-design/icons'
import { Button, Input, Space, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { fetchPrendarioCreditos, fetchPrendarioResumen, type PrendarioCategoria, type PrendarioCreditoRow } from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, type CredixStatItem } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

const CATEGORIA: Record<PrendarioCategoria, { color: string; label: string }> = {
  Vigente: { color: 'green', label: 'Vigente' },
  PorVencer: { color: 'gold', label: 'Por vencer' },
  Vencido: { color: 'red', label: 'Vencido' },
  Rematado: { color: 'magenta', label: 'Rematado' },
  SinBienes: { color: 'default', label: 'Sin bienes' },
  Otro: { color: 'blue', label: 'En trámite' },
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

export function CreditoPrendarioPage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [buscar, setBuscar] = useState('')
  const [termino, setTermino] = useState('')
  const [page, setPage] = useState(1)

  const resumen = useQuery({
    queryKey: ['prendario-resumen', oficinaId],
    queryFn: () => fetchPrendarioResumen(oficinaId),
    enabled: oficinaId > 0,
  })

  const listado = useQuery({
    queryKey: ['prendario-creditos', oficinaId, termino, page],
    queryFn: () => fetchPrendarioCreditos({ oficinaId, buscar: termino, page, pageSize: 20 }),
    enabled: oficinaId > 0,
  })

  const stats: CredixStatItem[] = useMemo(() => {
    const r = resumen.data
    if (!r) {
      return []
    }
    return [
      { label: 'Desembolsados', value: r.total },
      { label: 'Por vencer (3 días)', value: r.porVencer },
      { label: 'Vencidos', value: r.vencidos, tone: 'red' },
      { label: 'Rematados', value: r.rematados },
    ]
  }, [resumen.data])

  const columns: ColumnsType<PrendarioCreditoRow> = useMemo(
    () => [
    { title: 'Crédito', dataIndex: 'creditoId', width: 90 },
    {
      title: 'Contrato',
      dataIndex: 'numeroContratoPrendario',
      width: 110,
      render: (v: string | null) => v || '(pendiente)',
    },
    { title: 'DNI', dataIndex: 'numeroDocumento', width: 100 },
    { title: 'Cliente', dataIndex: 'nombreCompleto', ellipsis: true },
    {
      title: 'Tasación',
      dataIndex: 'montoTasacion',
      width: 110,
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: 'Préstamo',
      dataIndex: 'montoCredito',
      width: 110,
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: 'Vence',
      dataIndex: 'fechaVencimiento',
      width: 110,
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Remate',
      dataIndex: 'fechaRemate',
      width: 110,
      render: (v: string | null) => formatFecha(v),
    },
    {
      title: 'Días',
      dataIndex: 'diasParaVencer',
      width: 70,
      align: 'right',
      render: (v: number) => (
        <Text type={v < 0 ? 'danger' : v <= 3 ? 'warning' : undefined}>{v}</Text>
      ),
    },
    {
      title: 'Situación',
      dataIndex: 'categoria',
      width: 120,
      render: (v: PrendarioCategoria) => {
        const cat = CATEGORIA[v] ?? CATEGORIA.Otro
        return <Tag color={cat.color}>{cat.label}</Tag>
      },
    },
    {
      title: '',
      key: 'acciones',
      width: 90,
      render: (_, row) => (
        <Button
          type="link"
          size="small"
          onClick={() =>
            navigate(`/credito/prendario/gestionar/${row.personaId}?creditoId=${row.creditoId}`)
          }
        >
          Gestionar
        </Button>
      ),
    },
  ],
    [navigate],
  )

  return (
    <CredixPage
      title="Crédito prendario"
      subtitle="Cartera de créditos con bienes en custodia. Las tarjetas miden solo desembolsos."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Prendario' },
      ]}
      stats={stats}
      actions={
        <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/credito/prendario/nuevo')}>
          Nuevo
        </Button>
      }
    >
      <Space.Compact style={{ width: 'min(480px, 100%)', marginBottom: 16 }}>
        <Input
          placeholder="Cliente, DNI o contrato"
          value={buscar}
          onChange={(e) => setBuscar(e.target.value)}
          onPressEnter={() => {
            setPage(1)
            setTermino(buscar.trim())
          }}
        />
        <Button
          icon={<SearchOutlined />}
          loading={listado.isFetching}
          onClick={() => {
            setPage(1)
            setTermino(buscar.trim())
          }}
        >
          Buscar
        </Button>
      </Space.Compact>
      {listado.error ? (
        <Text type="danger">{errMsg(listado.error)}</Text>
      ) : null}
      <CredixDataTable<PrendarioCreditoRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={listado.data?.items ?? []}
        loading={listado.isFetching}
        pagination={{
          current: page,
          pageSize: 20,
          total: listado.data?.total ?? 0,
          showSizeChanger: false,
          onChange: (p) => setPage(p),
        }}
        locale={{ emptyText: 'No hay créditos prendarios en esta oficina' }}
        scroll={{ x: 'max-content' }}
      />
    </CredixPage>
  )
}
