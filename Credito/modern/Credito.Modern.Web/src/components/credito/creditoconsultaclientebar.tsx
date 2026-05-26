import { useCallback, useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { UserOutlined } from '@ant-design/icons'
import { Link } from 'react-router-dom'
import {
  fetchCreditosGrillaPersona,
  type CreditoGrillaPersonaRow,
} from '../../api/creditoGestion'
import { CreditoBuscarCliente } from './CreditoBuscarCliente'
import { CreditoPersonaCabecera } from './CreditoPersonaCabecera'
import { CreditoPersonaAvalesPanel } from './CreditoPersonaAvalesPanel'
import { CredixDataTable } from '../credix'
import { formatMoney } from '../../utils/formatMoney'
import { formatFecha } from '../../utils/formatFecha'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { creditosGrillaPersonaQueryKey } from '../../utils/creditoGrillaQueryKey'

const { Text } = Typography

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
  const autoCargadoPersonaRef = useRef<number | null>(null)

  useEffect(() => {
    if (personaIdInicial != null && personaIdInicial > 0) {
      setPersonaId(personaIdInicial)
      autoCargadoPersonaRef.current = null
    }
  }, [personaIdInicial])

  const creditosQuery = useQuery({
    queryKey: creditosGrillaPersonaQueryKey(oficinaId, personaId!, grupoActivo, 1, 25),
    queryFn: () =>
      fetchCreditosGrillaPersona({
        oficinaId,
        personaId: personaId!,
        grupoActivo,
        page: 1,
        pageSize: 25,
      }),
    enabled: oficinaId > 0 && personaId != null && personaId > 0,
    staleTime: creditoStaleTime.listado,
  })

  const cargarPersona = (pid: number, label: string) => {
    autoCargadoPersonaRef.current = null
    setPersonaId(pid)
    setClienteLabel(label)
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
    { title: 'Estado', dataIndex: 'estado' },
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

  return (
    <section className="credito-consulta-cliente" aria-labelledby="credito-consulta-cliente-label">
      <div className="credito-consulta-cliente__search">
        <span className="credito-consulta-cliente__label" id="credito-consulta-cliente-label">
          <UserOutlined aria-hidden /> Buscar cliente
        </span>
        <CreditoBuscarCliente
          value={clienteLabel}
          onChange={setClienteLabel}
          onSelectPersona={(pid, label) => {
            cargarPersona(pid, label)
          }}
          ariaLabelledBy="credito-consulta-cliente-label"
        />
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

          <CreditoPersonaAvalesPanel personaId={personaId} />

          <div className="credito-consulta-cliente__creditos">
          <div className="credito-consulta-cliente__creditos-head">
            <Text strong>Créditos del cliente</Text>
            <div className="credito-consulta-cliente__creditos-meta">
              <Tag color="processing">{items.length} crédito(s)</Tag>
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
            loading={creditosQuery.isLoading}
            columns={columns}
            dataSource={items}
            pagination={false}
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
