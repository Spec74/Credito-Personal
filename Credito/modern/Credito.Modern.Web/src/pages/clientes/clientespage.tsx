import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusOutlined } from '@ant-design/icons'
import { Alert, Button, Tag, Typography } from 'antd'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import type { SorterResult } from 'antd/es/table/interface'
import { listarClientes, type ClienteListadoRow } from '../../api/clientes'
import { ApiError } from '../../api/errors'
import {
  extractSearchTermsFromInput,
  primaryCatalogSearchTerm,
} from '../../components/caja/clienteBuscarResolve'
import {
  CredixCrudPage,
  CredixDataTable,
  CredixListToolbar,
  type CredixStatItem,
} from '../../components/credix'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { ClientesTableEmpty } from './components/ClientesTableEmpty'

const { Text } = Typography

const PAGE_SIZES = [15, 30, 45] as const

export function ClientesPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [termino, setTermino] = useState('')
  const [buscarFlush, setBuscarFlush] = useState<string | null>(null)
  const debounced = useDebouncedValue(termino.trim(), 400)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState<number>(15)
  const [sortField, setSortField] = useState('Codigo')
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc')

  useEffect(() => {
    if (buscarFlush != null && buscarFlush === debounced) {
      setBuscarFlush(null)
    }
  }, [debounced, buscarFlush])

  const terminoEfectivo = buscarFlush ?? debounced
  const buscarRaw = terminoEfectivo.length >= 2 ? terminoEfectivo : ''
  const buscarAplicado = buscarRaw ? primaryCatalogSearchTerm(buscarRaw) : ''
  const terminoCorto = termino.trim().length > 0 && termino.trim().length < 2

  const listado = useQuery({
    queryKey: ['clientes-listar', buscarAplicado, page, pageSize, sortField, sortDir],
    queryFn: () =>
      listarClientes({
        buscar: buscarAplicado || undefined,
        page,
        pageSize,
        sortField,
        sortDir,
      }),
    placeholderData: (prev) => prev,
  })

  const columns: ColumnsType<ClienteListadoRow> = useMemo(
    () => [
      { title: 'Código', dataIndex: 'codigo', width: 90, sorter: true },
      {
        title: 'Cliente',
        dataIndex: 'cliente',
        ellipsis: true,
        minWidth: 200,
        sorter: true,
        render: (nombre: string) => (
          <span className="clientes-table__nombre">{nombre}</span>
        ),
      },
      {
        title: 'Documento',
        dataIndex: 'documento',
        width: 140,
        sorter: true,
        render: (doc: string) => <span className="clientes-table__doc">{doc}</span>,
      },
      { title: 'Email', dataIndex: 'email', ellipsis: true, sorter: true },
      { title: 'Móvil', dataIndex: 'celular', width: 110, sorter: true },
      { title: 'Dirección', dataIndex: 'direccion', ellipsis: true, sorter: true },
      {
        title: '',
        key: 'acc',
        width: 100,
        fixed: 'right',
        render: (_, row) => (
          <Button
            size="small"
            type="primary"
            onClick={(e) => {
              e.stopPropagation()
              navigate(`/clientes/editar/${row.personaId}`)
            }}
          >
            Editar
          </Button>
        ),
      },
    ],
    [navigate],
  )

  const onTableChange = (
    pagination: TablePaginationConfig,
    _filters: unknown,
    sorter: SorterResult<ClienteListadoRow> | SorterResult<ClienteListadoRow>[],
  ) => {
    if (pagination.current) {
      setPage(pagination.current)
    }
    if (pagination.pageSize) {
      setPageSize(pagination.pageSize)
    }
    const s = Array.isArray(sorter) ? sorter[0] : sorter
    if (s?.field) {
      const field =
        typeof s.field === 'string'
          ? s.field.charAt(0).toUpperCase() + s.field.slice(1)
          : 'Codigo'
      setSortField(field === 'Cliente' ? 'Cliente' : field)
      setSortDir(s.order === 'descend' ? 'desc' : 'asc')
    }
  }

  const terminos = useMemo(
    () => (buscarRaw ? extractSearchTermsFromInput(buscarRaw) : []),
    [buscarRaw],
  )

  const stats: CredixStatItem[] = useMemo(() => {
    if (!listado.data) {
      return []
    }
    return [
      { value: listado.data.total, label: 'Total registros' },
      { value: listado.data.rows.length, label: 'En esta página' },
      {
        value: buscarAplicado ? 'Catálogo' : 'Mis créditos',
        label: 'Origen listado',
        tone: buscarAplicado ? undefined : 'green',
      },
    ]
  }, [listado.data, buscarAplicado])

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: ['clientes-listar'] })
  }

  const emptyNode = (
    <ClientesTableEmpty buscandoCatalogo={!!buscarAplicado} terminoCorto={terminoCorto} />
  )

  return (
    <CredixCrudPage
      className="clientes-page credix-page--stats-3"
      title="Clientes"
      subtitle="Mantenimiento de clientes con el mismo criterio del MVC: listado por sus créditos o búsqueda en catálogo (2+ caracteres). Doble clic en una fila para abrir la ficha."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Clientes' },
      ]}
      stats={stats}
      panelTitle="Lista de clientes"
      actions={
        <Link to="/clientes/nuevo">
          <Button type="primary" icon={<PlusOutlined />} size="middle">
            Nuevo cliente
          </Button>
        </Link>
      }
      toolbar={
        <CredixListToolbar
          withSearchButton
          value={termino}
          onChange={(v) => {
            setTermino(v)
            setBuscarFlush(null)
            setPage(1)
          }}
          onSubmit={() => {
            setBuscarFlush(termino.trim())
            setPage(1)
          }}
          placeholder="Apellidos, DNI, código, celular, email"
          hint={
            buscarAplicado
              ? `Buscando en catálogo completo${terminos.length > 1 ? ` · ${terminos.join(' · ')}` : ''}`
              : 'Mostrando clientes de sus créditos (sin filtro o menos de 2 caracteres).'
          }
          hintShort={buscarAplicado ? 'Catálogo completo' : 'Mis créditos'}
          loading={listado.isFetching}
          onRefresh={refrescar}
          extra={
            buscarAplicado ? (
              <Tag color="blue">Catálogo</Tag>
            ) : (
              <Tag color="green">Mis créditos</Tag>
            )
          }
        />
      }
    >
      {terminoCorto ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Con menos de 2 caracteres se listan los clientes de sus créditos (paridad grid legacy vacío)."
        />
      ) : null}

      {listado.isError ? (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 16 }}
          message={
            listado.error instanceof ApiError
              ? listado.error.message
              : 'Error al cargar clientes'
          }
        />
      ) : null}

      <Text type="secondary" style={{ display: 'block', marginBottom: 8, fontSize: 12 }}>
        Doble clic en una fila para editar · Orden por columnas · Paginación 15 / 30 / 45
      </Text>

      <CredixDataTable<ClienteListadoRow>
        mode="operacion"
        className="clientes-table"
        rowKey="personaId"
        columns={columns}
        dataSource={listado.data?.rows ?? []}
        loading={listado.isLoading}
        onChange={onTableChange}
        onRow={(row) => ({
          onDoubleClick: () => navigate(`/clientes/editar/${row.personaId}`),
        })}
        pagination={{
          current: page,
          pageSize,
          total: listado.data?.total ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [...PAGE_SIZES],
          showTotal: (total) => `${total} cliente(s)`,
        }}
        locale={{
          emptyText: listado.isLoading ? 'Cargando clientes…' : emptyNode,
        }}
      />
    </CredixCrudPage>
  )
}
