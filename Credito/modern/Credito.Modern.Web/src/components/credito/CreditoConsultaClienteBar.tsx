import { useCallback, useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Collapse, Empty, Segmented, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { Link } from 'react-router-dom'
import {
  fetchCreditoContexto,
  fetchCreditosGrillaPersona,
  type CreditoGrillaPersonaRow,
} from '../../api/creditoGestion'
import { CreditoBuscarCliente } from './creditobuscarcliente'
import { CreditoPersonaCabecera } from './CreditoPersonaCabecera'
import { CreditoPersonaAvalesPanel } from './CreditoPersonaAvalesPanel'
import { CredixDataTable } from '../credix'
import { formatMoney } from '../../utils/formatMoney'
import { formatFecha } from '../../utils/formatFecha'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { creditosGrillaPersonaQueryKey } from '../../utils/creditoGrillaQueryKey'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'

const { Text } = Typography

type Props = {
  oficinaId: number
  creditoActivoId: number | null
  personaIdInicial?: number | null
  onSeleccionarCredito: (creditoId: number, personaId: number, clienteLabel: string) => void
  onPersonaSeleccionada?: (personaId: number, clienteLabel: string) => void
  onLimpiarConsulta?: () => void
}

export function CreditoConsultaClienteBar({
  oficinaId,
  creditoActivoId,
  personaIdInicial,
  onSeleccionarCredito,
  onPersonaSeleccionada,
  onLimpiarConsulta,
}: Props) {
  const [clienteLabel, setClienteLabel] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(personaIdInicial ?? null)
  const [grupoActivo, setGrupoActivo] = useState(true)
  const [creditosPage, setCreditosPage] = useState(1)
  const creditosPageSize = 25
  const autoCargadoPersonaRef = useRef<number | null>(null)

  useEffect(() => {
    if (personaIdInicial != null && personaIdInicial > 0) {
      setPersonaId(personaIdInicial)
      autoCargadoPersonaRef.current = null
    }
  }, [personaIdInicial])

  useEffect(() => {
    setCreditosPage(1)
  }, [personaId, grupoActivo])

  const creditosQuery = useQuery({
    queryKey: creditosGrillaPersonaQueryKey(
      oficinaId,
      personaId!,
      grupoActivo,
      creditosPage,
      creditosPageSize,
    ),
    queryFn: () =>
      fetchCreditosGrillaPersona({
        oficinaId,
        personaId: personaId!,
        grupoActivo,
        page: creditosPage,
        pageSize: creditosPageSize,
      }),
    enabled: oficinaId > 0 && personaId != null && personaId > 0,
    staleTime: creditoStaleTime.listado,
  })

  const cargarPersona = useCallback(
    (pid: number, label: string) => {
      autoCargadoPersonaRef.current = null
      setPersonaId(pid)
      setClienteLabel(label)
      onPersonaSeleccionada?.(pid, label)
    },
    [onPersonaSeleccionada],
  )

  const cargarPorCreditoId = useCallback(
    async (term: string): Promise<boolean> => {
      const raw = term.trim()
      const id = Number(raw)
      if (!Number.isFinite(id) || id < 1 || !/^\d{4,10}$/.test(raw)) {
        return false
      }
      try {
        const info = await fetchCreditoContexto(id)
        const nombre = (info.personaNombre ?? '').trim()
        const label = nombre ? `${nombre} · Crédito ${id}` : `Crédito ${id}`
        autoCargadoPersonaRef.current = info.personaId
        setPersonaId(info.personaId)
        setClienteLabel(label)
        onSeleccionarCredito(id, info.personaId, label)
        return true
      } catch {
        return false
      }
    },
    [onSeleccionarCredito],
  )

  useEffect(() => {
    if (!personaId || creditosQuery.isLoading || creditosQuery.isFetching) {
      return
    }
    const filas = creditosQuery.data?.items ?? []
    if (filas.length !== 1) {
      return
    }
    if (autoCargadoPersonaRef.current === personaId) {
      return
    }
    autoCargadoPersonaRef.current = personaId
    onSeleccionarCredito(filas[0].creditoId, personaId, clienteLabel)
  }, [
    personaId,
    clienteLabel,
    creditosQuery.data,
    creditosQuery.isLoading,
    creditosQuery.isFetching,
    onSeleccionarCredito,
  ])

  const seleccionarCredito = useCallback(
    (row: CreditoGrillaPersonaRow) => {
      if (personaId == null) return
      onSeleccionarCredito(row.creditoId, personaId, clienteLabel)
    },
    [personaId, clienteLabel, onSeleccionarCredito],
  )

  const limpiar = () => {
    setClienteLabel('')
    setPersonaId(null)
    setGrupoActivo(true)
    setCreditosPage(1)
    autoCargadoPersonaRef.current = null
    onLimpiarConsulta?.()
  }

  const columns: ColumnsType<CreditoGrillaPersonaRow> = [
    {
      title: 'Crédito',
      dataIndex: 'creditoId',
      align: 'center',
      width: 96,
      render: (id: number) => (
        <strong className={creditoActivoId === id ? 'credito-consulta__credito-id--active' : ''}>
          {id}
        </strong>
      ),
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 148,
      render: (estado: string | null) => {
        const meta = getCreditoEstadoMeta(estado)
        return meta ? (
          <Tag color={meta.color}>{`${meta.codigo} · ${meta.label}`}</Tag>
        ) : (
          estado || '—'
        )
      },
    },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      align: 'right',
      width: 112,
      render: formatMoney,
    },
    {
      title: '1er pago',
      dataIndex: 'fechaPrimerPago',
      width: 110,
      render: (v: string | null) => (v ? formatFecha(v) : '—'),
    },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true, minWidth: 140 },
    {
      title: '',
      key: 'act',
      width: 88,
      align: 'right',
      render: (_, row) => (
        <Button
          type={creditoActivoId === row.creditoId ? 'primary' : 'default'}
          size="small"
          onClick={() => seleccionarCredito(row)}
        >
          {creditoActivoId === row.creditoId ? 'Activo' : 'Abrir'}
        </Button>
      ),
    },
  ]

  const items = creditosQuery.data?.items ?? []
  const totalCreditos = creditosQuery.data?.totalCount ?? items.length
  const usarChips = items.length > 0 && items.length <= 4 && creditosPage === 1

  return (
    <section className="credito-consulta-cliente" aria-labelledby="credito-consulta-cliente-label">
      <div className="credito-consulta-search-dock">
        <div className="credito-consulta-search-dock__search">
          <span className="credito-consulta-search-dock__label" id="credito-consulta-cliente-label">
            Buscar cliente
          </span>
          <CreditoBuscarCliente
            value={clienteLabel}
            onChange={(label) => {
              setClienteLabel(label)
              if (!label.trim()) {
                setPersonaId(null)
                autoCargadoPersonaRef.current = null
              }
            }}
            onSelectPersona={cargarPersona}
            ariaLabelledBy="credito-consulta-cliente-label"
            autoFocus
            autoSelectSingle
            onMiss={cargarPorCreditoId}
            searchButtonLabel="Cargar"
          />
          {personaId != null || creditoActivoId != null ? (
            <Button
              type="link"
              className="credito-consulta-search-dock__reset"
              onClick={limpiar}
            >
              Nueva consulta
            </Button>
          ) : null}
        </div>
      </div>

      {personaId != null && personaId > 0 ? (
        <>
          <CreditoPersonaCabecera
            oficinaId={oficinaId}
            personaId={personaId}
            clienteLabel={clienteLabel}
            onSolicitudCreada={() => {
              void creditosQuery.refetch()
            }}
            onDepurado={() => {
              void creditosQuery.refetch()
            }}
          />

          <Collapse
            ghost
            className="credito-consulta-avales-collapse"
            items={[
              {
                key: 'avales',
                label: 'Avales y avalados',
                children: (
                  <CreditoPersonaAvalesPanel oficinaId={oficinaId} personaId={personaId} />
                ),
              },
            ]}
          />

          <div className="credito-consulta-cliente__creditos">
            <div className="credito-consulta-cliente__creditos-head">
              <div className="credito-consulta-cliente__creditos-title">
                <Text strong>Créditos del cliente</Text>
                <Tag color="processing">{totalCreditos}</Tag>
              </div>
              <div className="credito-consulta-cliente__creditos-meta">
                <Segmented
                  size="small"
                  value={grupoActivo ? 'activos' : 'historico'}
                  onChange={(v) => setGrupoActivo(v === 'activos')}
                  options={[
                    { label: 'Activos', value: 'activos' },
                    { label: 'Histórico', value: 'historico' },
                  ]}
                />
                <Link to={`/credito/persona/${personaId}`}>Listado completo</Link>
              </div>
            </div>

            {usarChips ? (
              <div className="credito-consulta-creditos-chips" role="list">
                {items.map((row) => {
                  const meta = getCreditoEstadoMeta(row.estado)
                  const active = creditoActivoId === row.creditoId
                  return (
                    <button
                      key={row.creditoId}
                      type="button"
                      role="listitem"
                      className={
                        active
                          ? 'credito-consulta-credito-chip is-active'
                          : 'credito-consulta-credito-chip'
                      }
                      onClick={() => seleccionarCredito(row)}
                    >
                      <span className="credito-consulta-credito-chip__id">#{row.creditoId}</span>
                      <span className="credito-consulta-credito-chip__monto">
                        {formatMoney(row.montoCredito)}
                      </span>
                      {meta ? (
                        <Tag color={meta.color} className="credito-consulta-credito-chip__estado">
                          {meta.codigo}
                        </Tag>
                      ) : null}
                    </button>
                  )
                })}
              </div>
            ) : (
              <CredixDataTable<CreditoGrillaPersonaRow>
                mode="operacion"
                className="credito-consulta-cliente__table"
                rowKey="creditoId"
                loading={creditosQuery.isLoading || creditosQuery.isFetching}
                columns={columns}
                dataSource={items}
                size="small"
                pagination={{
                  current: creditosPage,
                  pageSize: creditosPageSize,
                  total: totalCreditos,
                  showSizeChanger: false,
                  size: 'small',
                  onChange: setCreditosPage,
                  showTotal: (t) => `${t} crédito(s)`,
                }}
                locale={{ emptyText: 'Sin créditos en esta vista' }}
                onRow={(row) => ({
                  onDoubleClick: () => seleccionarCredito(row),
                  className:
                    creditoActivoId === row.creditoId
                      ? 'credito-consulta-cliente__row--active'
                      : '',
                })}
              />
            )}

            {!usarChips && items.length > 0 ? (
              <Text type="secondary" className="credito-consulta-cliente__hint">
                Doble clic o «Abrir» para ver el plan y la gestión del crédito.
              </Text>
            ) : null}

            {items.length === 0 && !creditosQuery.isLoading ? (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description={
                  grupoActivo
                    ? 'Sin créditos activos. Pruebe «Histórico» o cree una solicitud.'
                    : 'Sin créditos en el histórico.'
                }
              />
            ) : null}
          </div>
        </>
      ) : null}
    </section>
  )
}
