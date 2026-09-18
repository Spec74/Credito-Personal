import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  UnorderedListOutlined,
  CheckOutlined,
  EditOutlined,
  FilePdfOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import {
  Alert,
  Button,
  Input,
  Modal,
  Progress,
  Segmented,
  Select,
  Space,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { downloadRptCreditoTareaPdf } from '../../api/creditoPlanes'
import {
  completarTarea,
  eliminarTarea,
  fetchPuedeEditarTarea,
  fetchTareaDetalle,
  fetchTareas,
  guardarTarea,
  type SubtareaGuardarInput,
  type TareaListItem,
} from '../../api/tareas'
import { ApiError } from '../../api/errors'
import { TareaEditorModal } from '../../components/credito/TareaEditorModal'
import { CredixCrudPage, CredixDataTable, type CredixStatItem } from '../../components/credix'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { formatMoney } from '../../utils/formatMoney'
import { filtraTareasPorBusqueda } from '../../utils/tareaListSearch'

const { Text } = Typography

type FiltroEstado = 'PEN' | 'COM' | 'TODAS'

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

export function TareasPage() {
  const queryClient = useQueryClient()
  const [filtro, setFiltro] = useState<FiltroEstado>('PEN')
  const [buscarTarea, setBuscarTarea] = useState('')
  const [analista, setAnalista] = useState<string>('')
  const [editorOpen, setEditorOpen] = useState(false)
  const [editId, setEditId] = useState(0)

  const permisos = useQuery({
    queryKey: ['tareas-puede-editar'],
    queryFn: fetchPuedeEditarTarea,
    staleTime: creditoStaleTime.master,
  })

  const listQuery = useQuery({
    queryKey: ['credito-tareas', filtro],
    queryFn: () => fetchTareas(filtro === 'TODAS' ? undefined : filtro),
    staleTime: creditoStaleTime.listado,
    placeholderData: (prev) => prev,
  })

  const detalleQuery = useQuery({
    queryKey: ['tarea-detalle', editId],
    queryFn: () => fetchTareaDetalle(editId),
    enabled: editId > 0 && editorOpen,
    staleTime: creditoStaleTime.operacion,
  })

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: ['credito-tareas'] })
  }

  const puedeEditar = permisos.data?.puedeEditar ?? false
  const rows = useMemo(() => listQuery.data ?? [], [listQuery.data])

  const analistas = useMemo(() => {
    const set = new Set<string>()
    for (const r of rows) {
      if (r.nombreUsuario?.trim()) {
        set.add(r.nombreUsuario.trim())
      }
    }
    return [...set].sort((a, b) => a.localeCompare(b, 'es'))
  }, [rows])

  const filasFiltradas = useMemo(() => {
    let f = rows
    if (analista) {
      f = f.filter((r) => r.nombreUsuario === analista)
    }
    f = filtraTareasPorBusqueda(f, buscarTarea)
    return f
  }, [rows, analista, buscarTarea])

  const stats: CredixStatItem[] = useMemo(() => {
    const pen = rows.filter((r) => r.estado === 'PEN').length
    const com = rows.filter((r) => r.estado === 'COM').length
    return [
      { value: filasFiltradas.length, label: 'Tareas visibles' },
      { value: pen, label: 'Pendientes', tone: pen > 0 ? 'red' : undefined },
      { value: com, label: 'Completadas', tone: 'green' },
      { value: rows.length, label: 'Total en filtro' },
    ]
  }, [rows, filasFiltradas.length])

  const guardar = useMutation({
    mutationFn: (body: { creditoId: number; subtareas: SubtareaGuardarInput[] }) =>
      guardarTarea({
        tareaId: editId,
        creditoId: body.creditoId,
        subtareas: body.subtareas,
      }),
    onSuccess: (r) => {
      message.success(r.mensaje)
      setEditorOpen(false)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const eliminar = useMutation({
    mutationFn: eliminarTarea,
    onSuccess: (r) => {
      message.success(r.mensaje)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const completar = useMutation({
    mutationFn: ({ id, val }: { id: number; val: boolean }) => completarTarea(id, val),
    onSuccess: (r) => {
      message.success(r.mensaje)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const exportPdf = useMutation({
    mutationFn: () =>
      downloadRptCreditoTareaPdf({
        estado: filtro === 'TODAS' ? undefined : filtro,
      }),
    onSuccess: () => message.success('PDF de tareas descargado'),
    onError: (e) => message.error(errMsg(e)),
  })

  const columns: ColumnsType<TareaListItem> = [
    { title: 'ID', dataIndex: 'tareaId', align: 'center' },
    {
      title: 'Cliente / tarea',
      key: 'cliente',
      ellipsis: true,
      minWidth: 200,
      render: (_, r) => (
        <div>
          <div className="credito-tareas-cliente-nombre">
            {r.clienteDni} — {r.clienteNombre}
          </div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            Crédito #{r.creditoId} · {formatMoney(r.montoCredito)}
          </Text>
        </div>
      ),
    },
    { title: 'Analista', dataIndex: 'nombreUsuario', ellipsis: true, minWidth: 100 },
    {
      title: 'Subtareas',
      key: 'sub',
      width: 120,
      render: (_, r) => {
        const pct =
          r.totalSubtareas > 0
            ? Math.round((r.subtareasCompletadas / r.totalSubtareas) * 100)
            : 0
        return (
          <div className="credito-tareas-sub-progress">
            <span className="credito-tareas-sub-progress__label">
              {r.subtareasCompletadas}/{r.totalSubtareas}
            </span>
            <Progress
              percent={pct}
              size="small"
              showInfo={false}
              status={pct === 100 ? 'success' : 'active'}
            />
          </div>
        )
      },
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      render: (e: string) => (
        <Tag color={e === 'COM' ? 'success' : 'processing'}>
          {e === 'COM' ? 'Completada' : 'Pendiente'}
        </Tag>
      ),
    },
    {
      title: 'Creada',
      dataIndex: 'fechaCreacion',
      render: (v: string) => v?.slice(0, 10) ?? '—',
    },
    {
      title: 'Acciones',
      key: 'acciones',
      width: 280,
      render: (_, r) => (
        <Space size={4} wrap>
          <Link to={`/credito/consulta?creditoId=${r.creditoId}`}>
            <Button size="small">Consulta</Button>
          </Link>
          <Button
            size="small"
            icon={<EditOutlined />}
            onClick={() => {
              setEditId(r.tareaId)
              setEditorOpen(true)
            }}
          >
            Editar
          </Button>
          <Button
            size="small"
            type={r.estado === 'COM' ? 'default' : 'primary'}
            icon={<CheckOutlined />}
            loading={completar.isPending}
            onClick={() =>
              completar.mutate({ id: r.tareaId, val: r.estado !== 'COM' })
            }
          >
            {r.estado === 'COM' ? 'Reabrir' : 'Completar'}
          </Button>
          {puedeEditar ? (
            <Button
              size="small"
              danger
              onClick={() => {
                Modal.confirm({
                  title: 'Eliminar tarea',
                  content: `¿Eliminar la tarea #${r.tareaId}?`,
                  okText: 'Eliminar',
                  okType: 'danger',
                  cancelText: 'Cancelar',
                  onOk: () => eliminar.mutateAsync(r.tareaId),
                })
              }}
            >
              Eliminar
            </Button>
          ) : null}
        </Space>
      ),
    },
  ]

  return (
    <CredixCrudPage
      className="credito-tareas-page credix-page--stats-3"
      title="Mis tareas"
      subtitle="Seguimiento de pendientes por crédito con subtareas, filtros y exportación (paridad MVC Tareas)."
      panelTitle="Listado de tareas"
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Tareas' },
      ]}
      toolbar={
        <div className="credito-tareas-toolbar">
          <div className="credito-tareas-toolbar__buscar">
            <Input
              allowClear
              size="large"
              prefix={<SearchOutlined style={{ color: 'var(--credix-brand)' }} />}
              placeholder="Cliente, DNI, Nº crédito, analista"
              value={buscarTarea}
              onChange={(e) => setBuscarTarea(e.target.value)}
            />
          </div>
          <Select
            className="credito-tareas-toolbar__analista"
            size="large"
            allowClear
            placeholder="Analista: todos"
            value={analista || undefined}
            onChange={(v) => setAnalista(v ?? '')}
            options={analistas.map((a) => ({ value: a, label: a }))}
          />
          <Segmented<FiltroEstado>
            className="credito-tareas-toolbar__filtros"
            value={filtro}
            onChange={setFiltro}
            options={[
              { label: 'Pendientes', value: 'PEN' },
              { label: 'Completadas', value: 'COM' },
              { label: 'Todas', value: 'TODAS' },
            ]}
          />
          <div className="credito-tareas-toolbar__actions">
            <Button
              icon={<ReloadOutlined />}
              onClick={refrescar}
              loading={listQuery.isFetching}
            >
              Actualizar
            </Button>
            <Link to={`/informes/credito-tarea?estado=${filtro === 'TODAS' ? 'PEN' : filtro}`}>
              <Button block icon={<UnorderedListOutlined />}>
                Informe detallado
              </Button>
            </Link>
            {puedeEditar ? (
              <>
                <Button
                  icon={<FilePdfOutlined />}
                  loading={exportPdf.isPending}
                  onClick={() => exportPdf.mutate()}
                >
                  Exportar PDF
                </Button>
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  onClick={() => {
                    setEditId(0)
                    setEditorOpen(true)
                  }}
                >
                  Nueva tarea
                </Button>
              </>
            ) : (
              <Text type="secondary">Creación: Administrador o Aprobador</Text>
            )}
          </div>
        </div>
      }
      extra={
        <TareaEditorModal
          open={editorOpen}
          editId={editId}
          puedeEditar={puedeEditar}
          loadingDetalle={detalleQuery.isLoading}
          detalle={detalleQuery.data}
          guardando={guardar.isPending}
          onClose={() => setEditorOpen(false)}
          onGuardar={({ creditoId, subtareas }) => {
            if (!creditoId) {
              message.warning('Seleccione un crédito')
              return
            }
            const payload = subtareas
              .filter((s) => s.titulo.trim())
              .map((s) => ({
                titulo: s.titulo.trim(),
                completada: s.completada,
              }))
            if (payload.length === 0) {
              message.warning('Agregue al menos una subtarea')
              return
            }
            guardar.mutate({ creditoId, subtareas: payload })
          }}
        />
      }
    >
      {listQuery.isError ? (
        <Alert
          type="error"
          showIcon
          message={errMsg(listQuery.error)}
          style={{ marginBottom: 16 }}
        />
      ) : null}

      <CredixDataTable<TareaListItem>
        mode="operacion"
        className="credito-tareas-table"
        rowKey="tareaId"
        columns={columns}
        dataSource={filasFiltradas}
        loading={listQuery.isLoading}
        scroll={{ x: 980 }}
        pagination={{
          pageSize: 15,
          showSizeChanger: true,
          pageSizeOptions: ['10', '15', '25', '50'],
          showTotal: (t) => `${t} tarea(s)`,
        }}
        locale={{ emptyText: 'No hay tareas con los filtros actuales' }}
        onRow={(r) => ({
          className:
            r.estado === 'COM' ? 'credito-tareas-row--completada' : '',
        })}
      />
    </CredixCrudPage>
  )
}
