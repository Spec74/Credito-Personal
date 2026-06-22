import { useQuery } from '@tanstack/react-query'
import { ExportOutlined, UserOutlined } from '@ant-design/icons'
import { Button, Empty, Tag, Tooltip, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchAvalesPersona,
  type CreditoAvalRelacion,
} from '../../api/creditoGestion'
import { formatMoney } from '../../utils/formatMoney'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'
import { CredixDataTable } from '../credix'

const { Text } = Typography
const appBasePath = import.meta.env.BASE_URL.replace(/\/$/, '')

type Props = {
  oficinaId: number
  personaId: number
}

/** Relaciones reales de aval/avalado para la ficha moderna de crédito. */
export function CreditoPersonaAvalesPanel({ oficinaId, personaId }: Props) {
  const query = useQuery({
    queryKey: ['credito-avales-persona', oficinaId, personaId],
    queryFn: () => fetchAvalesPersona(oficinaId, personaId),
    enabled: oficinaId > 0 && personaId > 0,
    staleTime: creditoStaleTime.ficha,
  })

  const personaUrl = (relatedPersonaId: number) =>
    `${appBasePath}/credito/consulta?personaId=${relatedPersonaId}`

  const columns: ColumnsType<CreditoAvalRelacion> = [
    {
      title: 'Tipo',
      dataIndex: 'grupo',
      width: 96,
      render: (grupo: string) => (
        <Tag
          className="credito-consulta-avales__type"
          color={grupo === 'AVALADO' ? 'purple' : 'blue'}
        >
          {grupo}
        </Tag>
      ),
    },
    {
      title: 'Persona relacionada',
      dataIndex: 'persona',
      ellipsis: true,
      minWidth: 220,
      render: (persona: string | null, row) => {
        if (!persona || !row.personaRelacionadaId) return '—'
        return (
          <Tooltip title="Abrir Crédito > Créditos para esta persona en una nueva pestaña">
            <Button
              className="credito-consulta-avales__persona"
              type="link"
              size="small"
              href={personaUrl(row.personaRelacionadaId)}
              target="_blank"
              rel="noopener noreferrer"
            >
              <UserOutlined aria-hidden />
              {persona}
              <ExportOutlined aria-hidden />
            </Button>
          </Tooltip>
        )
      },
    },
    { title: 'DNI', dataIndex: 'dni', width: 110, render: (v: string | null) => v || '—' },
    {
      title: 'Celular',
      dataIndex: 'celular',
      width: 112,
      render: (v: string | null) => v || '—',
    },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      align: 'right',
      width: 112,
      render: formatMoney,
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 130,
      render: (estado: string) => {
        const meta = getCreditoEstadoMeta(estado)
        return meta ? <Tag color={meta.color}>{meta.label}</Tag> : estado
      },
    },
  ]

  const rows = query.data ?? []
  if (query.isLoading) {
    return (
      <section className="credito-consulta-avales">
        <Text type="secondary">Cargando avales y avalados…</Text>
      </section>
    )
  }

  return (
    <section className="credito-consulta-avales" aria-label="Avales y avalados">
      <div className="credito-consulta-avales__head">
        <div>
          <Text strong>Aval y avalados</Text>
          <Text type="secondary" className="credito-consulta-avales__hint">
            Haga clic en el nombre para revisar los créditos y avales de esa persona.
          </Text>
        </div>
        {rows.length > 0 ? (
          <Tag color="processing" className="credito-consulta-avales__context-tag">
            Se abre en nueva pestaña para conservar esta búsqueda
          </Tag>
        ) : null}
      </div>
      {rows.length === 0 ? (
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="No hay aval asignado ni créditos avalados para este cliente."
        />
      ) : null}
      {rows.length > 0 ? (
        <CredixDataTable<CreditoAvalRelacion>
          mode="operacion"
          size="small"
          className="credito-consulta-avales__table"
          rowKey={(r, i) => `${r.grupo}-${r.creditoId}-${i}`}
          pagination={rows.length > 8 ? { pageSize: 8, showSizeChanger: false } : false}
          loading={query.isFetching}
          columns={columns}
          dataSource={rows}
          scroll={{ x: 620 }}
        />
      ) : null}
    </section>
  )
}
