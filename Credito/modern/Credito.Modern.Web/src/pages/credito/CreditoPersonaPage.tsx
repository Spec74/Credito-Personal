import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Button,
  Segmented,
  Space,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { fetchCreditosGrillaPersona, type CreditoGrillaPersonaRow } from '../../api/creditoGestion'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { creditosGrillaPersonaQueryKey } from '../../utils/creditoGrillaQueryKey'

export function CreditoPersonaPage() {
  const { personaId: personaParam } = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const qPersona = searchParams.get('pPersonaId') ?? searchParams.get('personaId')
  const personaId =
    Number(personaParam) ||
    (qPersona && !Number.isNaN(Number(qPersona)) ? Number(qPersona) : 0)
  const [grupoActivo, setGrupoActivo] = useState(true)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)

  const listQuery = useQuery({
    queryKey: creditosGrillaPersonaQueryKey(oficinaId, personaId, grupoActivo, page, pageSize),
    queryFn: () =>
      fetchCreditosGrillaPersona({
        oficinaId,
        personaId,
        grupoActivo,
        page,
        pageSize,
      }),
    enabled: oficinaId > 0 && personaId > 0,
    staleTime: creditoStaleTime.listado,
    placeholderData: (prev) => prev,
  })

  const columns: ColumnsType<CreditoGrillaPersonaRow> = [
    { title: 'ID', dataIndex: 'creditoId', width: 80 },
    { title: 'Estado', dataIndex: 'estado', width: 80 },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      align: 'right',
      render: formatMoney,
    },
    {
      title: '1er pago',
      dataIndex: 'fechaPrimerPago',
      width: 110,
      render: (v: string | null) => (v ? formatFecha(v) : '—'),
    },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
    {
      title: '',
      key: 'act',
      width: 100,
      render: (_, r) => (
        <Button
          type="link"
          size="small"
          onClick={() => navigate(`/credito/consulta?creditoId=${r.creditoId}`)}
        >
          Consultar
        </Button>
      ),
    },
  ]

  if (personaId < 1) {
    message.error('Persona inválida')
  }

  const stats = useMemo((): CredixStatItem[] => [
    {
      value: listQuery.data?.totalCount ?? listQuery.data?.items?.length ?? 0,
      label: 'Créditos',
    },
    { value: grupoActivo ? 'Activos' : 'Histórico', label: 'Vista' },
  ], [listQuery.data, grupoActivo])

  return (
    <CredixPage
      title="Créditos por persona"
      subtitle={`Listado de créditos de la persona #${personaId}.`}
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: `Persona #${personaId}` },
      ]}
      actions={
        <Link to={`/credito/simulador?personaId=${personaId}`}>
          <Button type="primary">Nuevo crédito (simulador)</Button>
        </Link>
      }
    >
      <CredixPanel>
        <Space wrap style={{ marginBottom: 16 }}>
          <Segmented
            value={grupoActivo ? 'activos' : 'historico'}
            onChange={(v) => {
              setGrupoActivo(v === 'activos')
              setPage(1)
            }}
            options={[
              { label: 'Activos (PEN/APR/DES)', value: 'activos' },
              { label: 'Histórico (ANU/PAG/REP)', value: 'historico' },
            ]}
          />
        </Space>
        <CredixDataTable<CreditoGrillaPersonaRow>
          mode="operacion"
          rowKey="creditoId"
          columns={columns}
          dataSource={listQuery.data?.items ?? []}
          loading={listQuery.isLoading}
          pagination={{
            current: page,
            pageSize,
            total: listQuery.data?.totalCount ?? 0,
            showSizeChanger: true,
            onChange: (p, ps) => {
              setPage(p)
              setPageSize(ps)
            },
          }}
        />
      </CredixPanel>
    </CredixPage>
  )
}
