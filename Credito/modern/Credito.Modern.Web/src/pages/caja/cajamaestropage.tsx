import { useMemo, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Checkbox,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Switch,
  Tag,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined } from '@ant-design/icons'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import { fetchOficinas } from '../../api/oficinas'
import {
  activarCaja,
  fetchCajaGestores,
  fetchCajasGestion,
  guardarCaja,
  type CajaGestionRow,
} from '../../api/cajaMaestro'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { mantenimientoCajasBreadcrumb } from '../../utils/mantenimientoBreadcrumbs'
import { CajaListToolbar } from './components/CajaListToolbar'

const PAGE_SIZES = ['15', '30', '45'] as const

export function CajaMaestroPage() {
  const location = useLocation()
  const isMantenimiento = location.pathname.startsWith('/mantenimiento/')
  const [buscar, setBuscar] = useState('')
  const buscarDebounced = useDebouncedValue(buscar.trim(), 400)
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<CajaGestionRow | null>(null)
  const [form] = Form.useForm()
  const queryClient = useQueryClient()

  const oficinasQuery = useQuery({ queryKey: ['oficinas'], queryFn: fetchOficinas })
  const gestoresQuery = useQuery({ queryKey: ['caja-gestores'], queryFn: fetchCajaGestores })

  const cajasQuery = useQuery({
    queryKey: ['cajas-gestion', buscarDebounced, page, pageSize, incluirInactivos],
    queryFn: () =>
      fetchCajasGestion({
        buscar: buscarDebounced,
        page,
        pageSize,
        incluirInactivos,
      }),
  })

  const guardar = useMutation({
    mutationFn: guardarCaja,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Caja guardada')
      setModalOpen(false)
      void queryClient.invalidateQueries({ queryKey: ['cajas-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['cajas-para-asignar'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  const activar = useMutation({
    mutationFn: activarCaja,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['cajas-gestion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error'),
  })

  const openCreate = () => {
    setEditing(null)
    form.setFieldsValue({
      oficinaId: oficinasQuery.data?.[0]?.oficinaId,
      denominacion: '',
      cajeroId: undefined,
      estado: true,
    })
    setModalOpen(true)
  }

  const openEdit = (row: CajaGestionRow) => {
    setEditing(row)
    form.setFieldsValue({
      oficinaId: row.oficinaId,
      denominacion: row.denominacion,
      cajeroId: row.cajeroId ?? undefined,
      estado: row.estado,
    })
    setModalOpen(true)
  }

  const columns: ColumnsType<CajaGestionRow> = useMemo(
    () => [
      { title: 'Id', dataIndex: 'cajaId', width: 70 },
      { title: 'Caja', dataIndex: 'denominacion', ellipsis: true, minWidth: 140 },
      {
        title: 'Oficina',
        dataIndex: 'oficinaDenominacion',
        width: 140,
        ellipsis: true,
      },
      {
        title: 'Gestor (cajero)',
        dataIndex: 'cajeroNombre',
        width: 160,
        ellipsis: true,
      },
      {
        title: 'Abierta',
        dataIndex: 'indAbierto',
        width: 90,
        render: (v: boolean) =>
          v ? <Tag color="orange">Sí</Tag> : <Tag>No</Tag>,
      },
      {
        title: 'Estado',
        dataIndex: 'estado',
        width: 90,
        render: (v: boolean) =>
          v ? <Tag color="green">Activo</Tag> : <Tag>Inactivo</Tag>,
      },
      {
        title: 'Acciones',
        key: 'acc',
        width: 200,
        fixed: 'right',
        render: (_, row) => (
          <Space wrap size="small">
            <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(row)}>
              Editar
            </Button>
            <Button
              size="small"
              onClick={() =>
                Modal.confirm({
                  title: row.estado ? '¿Desactivar caja?' : '¿Activar caja?',
                  content: row.indAbierto
                    ? 'La caja figura como abierta (asignada). Revise si corresponde desactivar el maestro.'
                    : undefined,
                  onOk: () => activar.mutateAsync(row.cajaId),
                })
              }
            >
              {row.estado ? 'Desactivar' : 'Activar'}
            </Button>
          </Space>
        ),
      },
    ],
    [activar],
  )

  const cajasRows = cajasQuery.data?.rows ?? []
  const totalRecords = cajasQuery.data?.totalRecords ?? 0
  const abiertas = cajasRows.filter((r) => r.indAbierto).length
  const activas = cajasRows.filter((r) => r.estado).length

  const stats = useMemo(
    () => [
      { value: totalRecords, label: 'Total cajas' },
      { value: activas, label: 'Activas (página)', tone: 'green' as const },
      { value: abiertas, label: 'Abiertas (página)', tone: 'red' as const },
    ],
    [totalRecords, activas, abiertas],
  )

  const onTableChange = (pagination: TablePaginationConfig) => {
    setPage(pagination.current ?? 1)
    setPageSize(pagination.pageSize ?? 15)
  }

  const breadcrumb = isMantenimiento
    ? mantenimientoCajasBreadcrumb()
    : [
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Maestro de cajas' },
      ]

  return (
    <CredixCrudPage
      className="caja-maestro-page credix-page--stats-3"
      title={isMantenimiento ? 'Mantenimiento de cajas' : 'Maestro de cajas'}
      subtitle="Alta y mantenimiento de cajas por oficina y gestor — paridad Caja/Index del MVC."
      panelTitle="LISTA DE CAJAS"
      stats={stats}
      breadcrumb={breadcrumb}
      toolbar={
        <CajaListToolbar
          className="credix-list-toolbar"
          value={buscar}
          onChange={(v) => {
            setBuscar(v)
            setPage(1)
          }}
          placeholder="Nombre de caja"
          hint="Búsqueda al servidor (400 ms). Doble clic en fila para editar."
          hintShort="Doble clic para editar."
          totalCount={totalRecords}
          loading={cajasQuery.isFetching}
          onRefresh={() => void cajasQuery.refetch()}
          extra={
            <div className="caja-maestro-filtros">
              <Checkbox
                checked={incluirInactivos}
                onChange={(e) => {
                  setIncluirInactivos(e.target.checked)
                  setPage(1)
                }}
              >
                Incluir inactivas
              </Checkbox>
              <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
                Nueva caja
              </Button>
            </div>
          }
        />
      }
      extra={
        <Modal
          title={editing ? `Editar caja #${editing.cajaId}` : 'Nueva caja'}
          open={modalOpen}
          onCancel={() => setModalOpen(false)}
          onOk={() => form.submit()}
          confirmLoading={guardar.isPending}
          width={480}
          destroyOnClose
        >
          <Form
            form={form}
            layout="vertical"
            onFinish={(v) =>
              guardar.mutate({
                cajaId: editing?.cajaId ?? 0,
                oficinaId: v.oficinaId,
                denominacion: v.denominacion.trim(),
                cajeroId: v.cajeroId ?? null,
                estado: v.estado,
              })
            }
          >
            <Form.Item name="oficinaId" label="Oficina" rules={[{ required: true }]}>
              <Select
                showSearch
                optionFilterProp="label"
                options={(oficinasQuery.data ?? []).map((o) => ({
                  value: o.oficinaId,
                  label: o.denominacion ?? `Oficina ${o.oficinaId}`,
                }))}
              />
            </Form.Item>
            <Form.Item
              name="denominacion"
              label="Denominación"
              rules={[{ required: true }]}
            >
              <Input maxLength={100} />
            </Form.Item>
            <Form.Item name="cajeroId" label="Gestor (cajero)">
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="Opcional — requerido para asignar turno"
                options={(gestoresQuery.data ?? []).map((g) => ({
                  value: g.usuarioId,
                  label: g.nombreCompleto,
                }))}
              />
            </Form.Item>
            <Form.Item name="estado" label="Activo" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        </Modal>
      }
    >
      <p className="credix-module-banner credix-module-banner--spaced">
        <strong>Paridad MVC:</strong> denominación, oficina, gestor y estado activo · paginación
        15/30/45 · doble clic abre el mismo formulario de edición.
      </p>

      {cajasQuery.isError ? (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 12 }}
          message={
            cajasQuery.error instanceof ApiError
              ? cajasQuery.error.message
              : 'Error al cargar cajas'
          }
        />
      ) : null}

      <CredixDataTable<CajaGestionRow>
        mode="operacion"
        className="caja-maestro-table"
        rowKey="cajaId"
        columns={columns}
        dataSource={cajasRows}
        loading={cajasQuery.isLoading}
        scroll={{ x: 'max-content' }}
        pagination={{
          current: page,
          pageSize,
          total: totalRecords,
          showSizeChanger: true,
          pageSizeOptions: [...PAGE_SIZES],
          showTotal: (t) => `${t} caja(s)`,
        }}
        onChange={onTableChange}
        onRow={(row) => ({
          onDoubleClick: () => openEdit(row),
        })}
        locale={{
          emptyText: cajasQuery.isLoading
            ? 'Cargando cajas…'
            : 'No hay cajas que coincidan con el filtro.',
        }}
      />
    </CredixCrudPage>
  )
}
