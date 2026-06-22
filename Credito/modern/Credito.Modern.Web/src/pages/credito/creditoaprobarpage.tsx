import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Modal, Tag, Tooltip, message } from 'antd'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import {
  CheckOutlined,
  CloseOutlined,
  FileSearchOutlined,
  PlayCircleOutlined,
  UserOutlined,
} from '@ant-design/icons'
import {
  aprobarCredito,
  fetchCreditosPorAprobar,
  rechazarCredito,
  type CreditoPorAprobarRow,
} from '../../api/creditoAprobar'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import {
  CredixCrudPage,
  CredixDataTable,
  type CredixStatItem,
} from '../../components/credix'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { resolveAprobarBuscar } from '../../utils/creditoAprobarSearch'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'
import { formatMoney } from '../../utils/formatMoney'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import {
  esCreditoAdministrador,
  esCreditoAprobador1,
} from '../../utils/creditoOperacionPermisos'
import { AprobarSearchToolbar } from './components/AprobarSearchToolbar'
import { AprobarTableEmpty } from './components/AprobarTableEmpty'

const PAGE_SIZES = ['15', '30', '45'] as const

export function CreditoAprobarPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const roles = useMemo(() => session?.roles ?? [], [session?.roles])
  const puedeAprobar = esCreditoAprobador1(roles) || esCreditoAdministrador(roles)

  const [buscar, setBuscar] = useState('')
  const buscarResuelto = useMemo(() => resolveAprobarBuscar(buscar), [buscar])
  const buscarAplicado = useDebouncedValue(buscarResuelto.aplicado, 350)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [sortField, setSortField] = useState('agente')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')

  const listQuery = useQuery({
    queryKey: [
      'creditos-por-aprobar',
      oficinaId,
      buscarAplicado,
      page,
      pageSize,
      sortField,
      sortOrder,
    ],
    queryFn: () =>
      fetchCreditosPorAprobar({
        oficinaId,
        buscar: buscarAplicado || undefined,
        page,
        pageSize,
        sortField,
        sortOrder,
      }),
    enabled: oficinaId > 0,
    staleTime: creditoStaleTime.listado,
    placeholderData: (prev) => prev,
  })

  const invalidar = () => {
    void queryClient.invalidateQueries({ queryKey: ['creditos-por-aprobar'] })
  }

  const aprobar = useMutation({
    mutationFn: ({
      creditoId,
      opcion,
    }: {
      creditoId: number
      opcion: 0 | 1
    }) => aprobarCredito(oficinaId, creditoId, opcion),
    onSuccess: () => {
      message.success('Crédito aprobado')
      invalidar()
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'No se pudo aprobar'),
  })

  const rechazar = useMutation({
    mutationFn: (creditoId: number) => rechazarCredito(oficinaId, creditoId),
    onSuccess: () => {
      message.success('Crédito rechazado')
      invalidar()
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'No se pudo rechazar'),
  })

  const confirmarAprobar = (row: CreditoPorAprobarRow) => {
    Modal.confirm({
      title: 'Aprobar crédito',
      content: `¿Aprobar crédito ${row.creditoId} de ${row.cliente ?? 'cliente'}? Se usará la aprobación vigente del legacy.`,
      okText: 'Aprobar',
      cancelText: 'Cancelar',
      onOk: () => aprobar.mutateAsync({ creditoId: row.creditoId, opcion: 1 }),
    })
  }

  const confirmarRechazar = (row: CreditoPorAprobarRow) => {
    Modal.confirm({
      title: 'Rechazar solicitud',
      content: `¿Rechazar crédito ${row.creditoId}? Esta acción usa la misma lógica que el MVC (orden/solicitud).`,
      okText: 'Rechazar',
      okButtonProps: { danger: true },
      cancelText: 'Cancelar',
      onOk: () => rechazar.mutateAsync(row.creditoId),
    })
  }

  const columns: ColumnsType<CreditoPorAprobarRow> = useMemo(
    () => [
      { title: 'Crédito', dataIndex: 'creditoId', width: 82, sorter: true },
      { title: 'Código', dataIndex: 'codigo', width: 96, sorter: true },
      {
        title: 'Cliente',
        dataIndex: 'cliente',
        ellipsis: true,
        minWidth: 150,
        sorter: true,
        render: (nombre: string | null) => (
          <span className="credito-aprobacion-table__cliente">{nombre ?? '—'}</span>
        ),
      },
      {
        title: 'Documento',
        dataIndex: 'documento',
        width: 118,
        ellipsis: true,
        sorter: true,
        render: (doc: string | null) => (
          <span className="credito-aprobacion-table__doc">{doc?.trim() || '—'}</span>
        ),
      },
      {
        title: 'Monto',
        dataIndex: 'monto',
        width: 108,
        align: 'right',
        sorter: true,
        render: (v: number) => (
          <span className="credito-aprobacion-table__monto">{formatMoney(v)}</span>
        ),
      },
      {
        title: 'Interés',
        dataIndex: 'interes',
        width: 96,
        align: 'right',
        render: formatMoney,
        sorter: true,
      },
      { title: 'Gestor', dataIndex: 'agente', width: 132, ellipsis: true, sorter: true },
      {
        title: 'Estado',
        dataIndex: 'estado',
        width: 110,
        align: 'center',
        render: (estado: string) => {
          const meta = getCreditoEstadoMeta(estado)
          return <Tag color={meta?.color ?? 'default'}>{meta?.label ?? estado}</Tag>
        },
      },
      {
        title: 'Acciones',
        key: 'acciones',
        width: puedeAprobar ? 270 : 170,
        fixed: 'right',
        render: (_, row) => (
          <div className="credito-aprobacion-actions">
            <Tooltip title="Consulta de crédito">
              <Button
                size="small"
                className="credito-aprobacion-actions__btn credito-aprobacion-actions__btn--consulta"
                icon={<FileSearchOutlined />}
                onClick={(e) => {
                  e.stopPropagation()
                  navigate(`/credito/consulta?creditoId=${row.creditoId}`)
                }}
              >
                Consulta
              </Button>
            </Tooltip>
            <Tooltip title="Ficha cliente (PDF)">
              <Button
                size="small"
                className="credito-aprobacion-actions__btn"
                icon={<UserOutlined />}
                onClick={(e) => {
                  e.stopPropagation()
                  navigate(`/informes/reporte-cliente?personaId=${row.personaId}`)
                }}
              >
                Ficha
              </Button>
            </Tooltip>
            {row.estado === 'CRE' ? (
              <Tooltip title="Continuar simulación y generar para aprobación">
                <Button
                  size="small"
                  type="primary"
                  className="credito-aprobacion-actions__btn credito-aprobacion-actions__btn--aprobar"
                  icon={<PlayCircleOutlined />}
                  onClick={(e) => {
                    e.stopPropagation()
                    navigate(
                      `/credito/simulador?personaId=${row.personaId}&solicitudCreditoId=${row.creditoId}`,
                    )
                  }}
                >
                  Continuar
                </Button>
              </Tooltip>
            ) : null}
            {puedeAprobar && row.estado !== 'CRE' ? (
              <>
                <Tooltip title="Aprobar crédito pendiente">
                  <Button
                    size="small"
                    type="primary"
                    className="credito-aprobacion-actions__btn credito-aprobacion-actions__btn--aprobar"
                    icon={<CheckOutlined />}
                    loading={aprobar.isPending}
                    disabled={row.estado !== 'PEN'}
                    onClick={(e) => {
                      e.stopPropagation()
                      confirmarAprobar(row)
                    }}
                  >
                    Aprobar
                  </Button>
                </Tooltip>
                <Tooltip title="Rechazar solicitud">
                  <Button
                    size="small"
                    danger
                    className="credito-aprobacion-actions__btn credito-aprobacion-actions__btn--rechazar"
                    icon={<CloseOutlined />}
                    loading={rechazar.isPending}
                    disabled={row.estado !== 'PEN'}
                    onClick={(e) => {
                      e.stopPropagation()
                      confirmarRechazar(row)
                    }}
                  >
                    Rechazar
                  </Button>
                </Tooltip>
              </>
            ) : null}
          </div>
        ),
      },
    ],
    [navigate, aprobar.isPending, rechazar.isPending, puedeAprobar],
  )

  const onTableChange = (
    pagination: TablePaginationConfig,
    _filters: unknown,
    sorter: unknown,
  ) => {
    setPage(pagination.current ?? 1)
    setPageSize(pagination.pageSize ?? 15)
    const s = sorter as { field?: string; order?: 'ascend' | 'descend' | null }
    if (s.field) {
      setSortField(String(s.field))
      setSortOrder(s.order === 'descend' ? 'desc' : 'asc')
    }
  }

  const total = listQuery.data?.total ?? 0
  const filtrado = buscarAplicado.length > 0
  const haySolicitudesCre = (listQuery.data?.items ?? []).some((r) => r.estado === 'CRE')

  const aprobarStats: CredixStatItem[] = useMemo(() => {
    const rows = listQuery.data?.items ?? []
    const monto = rows.reduce((s, r) => s + (r.monto ?? 0), 0)
    return [
      {
        value: total,
        label: filtrado ? 'Coincidencias' : 'Pendientes (total)',
        tone: 'green',
      },
      { value: formatMoney(monto), label: 'Monto en página' },
      { value: page, label: 'Página' },
    ]
  }, [listQuery.data, page, total, filtrado])

  const aplicarBusqueda = () => {
    setPage(1)
    if (!buscarResuelto.terminoCorto) {
      void listQuery.refetch()
    }
  }

  return (
    <CredixCrudPage
      className="credito-aprobacion-page credix-page--stats-3"
      title="Créditos por aprobar"
      subtitle="Bandeja PEN de la oficina actual con búsqueda avanzada y aprobación en la misma pantalla."
      panelTitle="CREDITOS POR APROBAR"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Aprobar créditos' },
      ]}
      stats={aprobarStats}
      toolbar={
        <AprobarSearchToolbar
          value={buscar}
          onChange={(v) => {
            setBuscar(v)
            setPage(1)
          }}
          onSearch={aplicarBusqueda}
          loading={listQuery.isFetching}
          terminos={buscarResuelto.terminos}
          terminoCorto={buscarResuelto.terminoCorto}
          filtrado={filtrado}
          totalFiltrado={filtrado ? total : undefined}
          onRefresh={() => void listQuery.refetch()}
        />
      }
    >
      {listQuery.isError ? (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 16 }}
          message={
            listQuery.error instanceof ApiError
              ? listQuery.error.message
              : 'Error al cargar el listado'
          }
        />
      ) : null}

      {!puedeAprobar ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message="Tu usuario puede consultar esta bandeja, pero solo APROBADOR 1 o ADMINISTRADOR puede aprobar o rechazar."
        />
      ) : null}

      {haySolicitudesCre ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Hay solicitudes en preparación (CRE)"
          description="Estas filas aún no se pueden aprobar. Abra Continuar, simule el plan y pulse Generar crédito para aprobación para que pasen a PEN."
        />
      ) : null}

      <CredixDataTable<CreditoPorAprobarRow>
        mode="operacion"
        className="credito-aprobacion-table"
        rowKey="creditoId"
        columns={columns}
        dataSource={listQuery.data?.items ?? []}
        loading={listQuery.isLoading}
        scroll={{ x: 'max-content' }}
        pagination={{
          current: page,
          pageSize,
          total,
          showSizeChanger: true,
          pageSizeOptions: [...PAGE_SIZES],
          showTotal: (t) =>
            filtrado ? `${t} coincidencia(s)` : `${t} crédito(s) pendiente(s)`,
        }}
        onChange={onTableChange}
        onRow={(row) => ({
          onDoubleClick: () =>
            navigate(`/credito/consulta?creditoId=${row.creditoId}`),
        })}
        locale={{
          emptyText: listQuery.isLoading ? (
            'Cargando créditos…'
          ) : (
            <AprobarTableEmpty
              buscando={filtrado || buscarResuelto.terminoCorto}
              terminoCorto={buscarResuelto.terminoCorto}
            />
          ),
        }}
      />
    </CredixCrudPage>
  )
}
