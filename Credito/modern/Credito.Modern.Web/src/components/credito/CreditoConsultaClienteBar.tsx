import { useCallback, useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Empty, Input, Spin, Tag, Typography, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { SearchOutlined, UserOutlined } from '@ant-design/icons'
import { Link } from 'react-router-dom'
import {
  fetchCreditosGrillaPersona,
  type CreditoGrillaPersonaRow,
} from '../../api/creditoGestion'
import { buscarClientes } from '../../api/clientes'
import { CreditoPersonaCabecera } from './CreditoPersonaCabecera'
import { CreditoPersonaAvalesPanel } from './CreditoPersonaAvalesPanel'
import { CredixDataTable } from '../credix'
import { formatMoney } from '../../utils/formatMoney'
import { formatFecha } from '../../utils/formatFecha'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { creditosGrillaPersonaQueryKey } from '../../utils/creditoGrillaQueryKey'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import type { ClienteBuscarItem } from '../../types/api'

const { Text } = Typography

function parseClienteLabel(label: string) {
  const code = label.match(/\[([^\]]*)\]/)?.[1]?.trim() || null
  const clean = label.replace(/\s*\[[^\]]*\]\s*$/, '').trim()
  const [documento = '', ...rest] = clean.split(/\s+/)
  return {
    documento,
    nombre: rest.join(' '),
    code,
  }
}

type Props = {
  oficinaId: number
  creditoActivoId: number | null
  personaIdInicial?: number | null
  onSeleccionarCredito: (creditoId: number, personaId: number, clienteLabel: string) => void
}

export function CreditoConsultaClienteBar({
  oficinaId,
  creditoActivoId,
  personaIdInicial,
  onSeleccionarCredito,
}: Props) {
  const [clienteLabel, setClienteLabel] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(personaIdInicial ?? null)
  const [grupoActivo, setGrupoActivo] = useState(true)
  const [creditosPage, setCreditosPage] = useState(1)
  const creditosPageSize = 25
  const autoCargadoPersonaRef = useRef<number | null>(null)
  const terminoBusqueda = clienteLabel.trim()
  const terminoDebounced = useDebouncedValue(terminoBusqueda, 220)

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

  const clientesQuery = useQuery({
    queryKey: ['credito-clientes-buscar', terminoDebounced],
    queryFn: () => buscarClientes(terminoDebounced),
    enabled: terminoDebounced.length >= 2,
    staleTime: 30_000,
  })

  const clientesEncontrados = clientesQuery.data ?? []

  const cargarPersona = useCallback((pid: number, label: string) => {
    autoCargadoPersonaRef.current = null
    setPersonaId(pid)
    setClienteLabel(label)
  }, [])

  const buscarAhora = () => {
    if (terminoBusqueda.length < 2) {
      message.warning('Ingrese al menos 2 caracteres: DNI, nombre, código o celular')
      return
    }
    void clientesQuery.refetch()
  }

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

  const seleccionarCliente = useCallback(
    (item: ClienteBuscarItem) => {
      cargarPersona(item.personaId, item.label)
    },
    [cargarPersona],
  )

  const columns: ColumnsType<CreditoGrillaPersonaRow> = [
    {
      title: 'Crédito',
      dataIndex: 'creditoId',
      align: 'center',
      render: (id: number) => (
        <strong className={creditoActivoId === id ? 'credito-consulta__credito-id--active' : ''}>
          {id}
        </strong>
      ),
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      render: (estado: string | null) => {
        const meta = getCreditoEstadoMeta(estado)
        return meta ? (
          <Tag color={meta.color}>{`${meta.codigo} - ${meta.label}`}</Tag>
        ) : (
          estado || '—'
        )
      },
    },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      align: 'right',
      render: formatMoney,
    },
    {
      title: '1er pago',
      dataIndex: 'fechaPrimerPago',
      render: (v: string | null) => (v ? formatFecha(v) : '—'),
    },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true, minWidth: 140 },
    {
      title: '',
      key: 'act',
      width: 1,
      render: (_, row) => (
        <Button
          type={creditoActivoId === row.creditoId ? 'primary' : 'link'}
          size="small"
          onClick={() => seleccionarCredito(row)}
        >
          {creditoActivoId === row.creditoId ? 'Activo' : 'Ver'}
        </Button>
      ),
    },
  ]

  const items = creditosQuery.data?.items ?? []
  const totalCreditos = creditosQuery.data?.totalCount ?? items.length

  return (
    <section className="credito-consulta-cliente" aria-labelledby="credito-consulta-cliente-label">
      <div className="credito-consulta-cliente__search">
        <span className="credito-consulta-cliente__label" id="credito-consulta-cliente-label">
          <UserOutlined aria-hidden /> Buscar cliente
        </span>
        <div className="credito-cliente-search">
          <div className="credito-cliente-search__bar">
            <Input
              size="large"
              allowClear
              value={clienteLabel}
              prefix={<SearchOutlined />}
              placeholder="Buscar por DNI, nombre, apellidos, código o celular"
              aria-labelledby="credito-consulta-cliente-label"
              onChange={(e) => {
                setClienteLabel(e.target.value)
                setPersonaId(null)
                autoCargadoPersonaRef.current = null
              }}
              onPressEnter={() => {
                const unico = clientesEncontrados[0]
                if (clientesEncontrados.length === 1 && unico) {
                  seleccionarCliente(unico)
                } else {
                  buscarAhora()
                }
              }}
            />
            <Button
              type="primary"
              size="large"
              icon={<SearchOutlined />}
              loading={clientesQuery.isFetching}
              onClick={buscarAhora}
            >
              Buscar cliente
            </Button>
          </div>

          <div className="credito-cliente-search__meta">
            {terminoBusqueda.length < 2 ? (
              <Text type="secondary">Escriba mínimo 2 caracteres para iniciar la búsqueda.</Text>
            ) : clientesQuery.isFetching ? (
              <Text type="secondary">Buscando coincidencias...</Text>
            ) : clientesEncontrados.length > 0 ? (
              <Text type="secondary">
                {clientesEncontrados.length} resultado(s). Seleccione un cliente para cargar su ficha y créditos.
              </Text>
            ) : clientesQuery.isSuccess ? (
              <Text type="secondary">Sin resultados. Pruebe con DNI, primer apellido, código o celular.</Text>
            ) : (
              <Text type="secondary">Busca en clientes activos por documento, nombre, código y celular.</Text>
            )}
          </div>

          {terminoBusqueda.length >= 2 ? (
            <Spin spinning={clientesQuery.isFetching}>
              {clientesEncontrados.length > 0 ? (
                <div className="credito-cliente-search__results" role="listbox">
                  {clientesEncontrados.map((item) => {
                    const parsed = parseClienteLabel(item.label)
                    const selected = personaId === item.personaId
                    return (
                      <button
                        key={item.personaId}
                        type="button"
                        className={
                          selected
                            ? 'credito-cliente-search__result credito-cliente-search__result--selected'
                            : 'credito-cliente-search__result'
                        }
                        onClick={() => seleccionarCliente(item)}
                      >
                        <span className="credito-cliente-search__avatar" aria-hidden>
                          <UserOutlined />
                        </span>
                        <span className="credito-cliente-search__body">
                          <strong>{parsed.nombre || item.label}</strong>
                          <span>
                            DNI: {parsed.documento || '—'}
                            {parsed.code ? ` · Código: ${parsed.code}` : ''}
                          </span>
                        </span>
                        <span className="credito-cliente-search__action">
                          {selected ? 'Seleccionado' : 'Seleccionar'}
                        </span>
                      </button>
                    )
                  })}
                </div>
              ) : clientesQuery.isSuccess ? (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description="No encontramos clientes con ese criterio"
                />
              ) : null}
            </Spin>
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

          <CreditoPersonaAvalesPanel oficinaId={oficinaId} personaId={personaId} />

          <div className="credito-consulta-cliente__creditos">
          <div className="credito-consulta-cliente__creditos-head">
            <Text strong>Créditos del cliente</Text>
            <div className="credito-consulta-cliente__creditos-meta">
              <Tag color="processing">{totalCreditos} crédito(s)</Tag>
              <Button
                type="link"
                size="small"
                onClick={() => setGrupoActivo((v) => !v)}
              >
                {grupoActivo ? 'Ver histórico' : 'Ver activos'}
              </Button>
              <Link to={`/credito/persona/${personaId}`}>Listado completo</Link>
              <Link to={`/informes/reporte-cliente?personaId=${personaId}`}>
                Ficha PDF
              </Link>
            </div>
          </div>

          <CredixDataTable<CreditoGrillaPersonaRow>
            mode="operacion"
            className="credito-consulta-cliente__table"
            rowKey="creditoId"
            loading={creditosQuery.isLoading || creditosQuery.isFetching}
            columns={columns}
            dataSource={items}
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
          <Text type="secondary" className="credito-consulta-cliente__hint">
            Doble clic en una fila o use «Ver» para abrir el plan de pagos y la gestión del crédito.
          </Text>
        </div>
        </>
      ) : null}
    </section>
  )
}
