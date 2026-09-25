import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Drawer,
  Form,
  Grid,
  Input,
  Modal,
  Select,
  Space,
  Switch,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  activarRol,
  asignarMenusRol,
  fetchRolMenusDetalle,
  fetchRolesGestion,
  guardarRol,
  type RolGestionRow,
} from '../../api/rolesAdmin'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'

const { Paragraph } = Typography

export function RolesPage() {
  const screens = Grid.useBreakpoint()
  const [filtro, setFiltro] = useState('')
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [rolId, setRolId] = useState(0)
  const [form] = Form.useForm<{ denominacion: string; estado: boolean; menuIds: number[] }>()
  const queryClient = useQueryClient()

  const rolesQuery = useQuery({
    queryKey: ['roles-gestion', incluirInactivos],
    queryFn: () => fetchRolesGestion(incluirInactivos),
  })

  const menusQuery = useQuery({
    queryKey: ['rol-menus-detalle', rolId],
    queryFn: () => fetchRolMenusDetalle(rolId),
    enabled: rolId >= 1 && drawerOpen,
  })

  useEffect(() => {
    if (!menusQuery.data) return
    const sel = menusQuery.data.menus.filter((m) => m.asignado).map((m) => m.menuId)
    form.setFieldsValue({
      denominacion: menusQuery.data.rol.denominacion,
      estado: menusQuery.data.rol.estado,
      menuIds: sel,
    })
  }, [menusQuery.data, form])

  const guardar = useMutation({
    mutationFn: guardarRol,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Rol guardado')
      const nuevoId = res.id ?? rolId
      if (rolId < 1 && nuevoId) setRolId(nuevoId)
      void queryClient.invalidateQueries({ queryKey: ['roles-gestion'] })
      if (nuevoId) void queryClient.invalidateQueries({ queryKey: ['rol-menus-detalle', nuevoId] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  const activar = useMutation({
    mutationFn: activarRol,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['roles-gestion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al activar/desactivar'),
  })

  const asignarMenus = useMutation({
    mutationFn: ({ id, menuIds }: { id: number; menuIds: number[] }) =>
      asignarMenusRol(id, menuIds),
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo asignar menú')
        return
      }
      message.success('Menús asignados')
      void menusQuery.refetch()
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al asignar menús'),
  })

  const filtradas = useMemo(() => {
    const lista = rolesQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return lista
    return lista.filter(
      (r) =>
        r.denominacion?.toLowerCase().includes(q) || String(r.rolId).includes(q),
    )
  }, [rolesQuery.data, filtro])

  const stats = useCrudListStats(filtradas, 'roles')

  const openCreate = () => {
    setRolId(0)
    form.setFieldsValue({ denominacion: '', estado: true, menuIds: [] })
    setDrawerOpen(true)
  }

  const openEdit = (row: RolGestionRow) => {
    setRolId(row.rolId)
    setDrawerOpen(true)
  }

  const closeDrawer = () => {
    setDrawerOpen(false)
    setRolId(0)
    form.resetFields()
  }

  const columns: ColumnsType<RolGestionRow> = [
    {
      title: 'Id',
      dataIndex: 'rolId',
      width: 80,
      sorter: (a, b) => a.rolId - b.rolId,
    },
    {
      title: 'Rol',
      dataIndex: 'denominacion',
      sorter: (a, b) => a.denominacion.localeCompare(b.denominacion, 'es'),
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 110,
      render: (activo: boolean) =>
        activo ? <Tag color="green">Activo</Tag> : <Tag>Inactivo</Tag>,
    },
    {
      title: 'Acciones',
      key: 'acciones',
      width: 220,
      render: (_, row) => (
        <Space wrap size="small">
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(row)}>
            Editar
          </Button>
          <Button
            size="small"
            loading={activar.isPending}
            onClick={() =>
              Modal.confirm({
                title: row.estado ? '¿Desactivar rol?' : '¿Activar rol?',
                onOk: () => activar.mutateAsync(row.rolId),
              })
            }
          >
            {row.estado ? 'Desactivar' : 'Activar'}
          </Button>
        </Space>
      ),
    },
  ]

  const menuOptions = (menusQuery.data?.menus ?? []).map((m) => ({
    value: m.menuId,
    label: m.denominacion,
  }))

  return (
    <CredixCrudPage
      title="Roles"
      subtitle="Alta de roles, activación y asignación de menús."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/admin">Administración</Link> },
        { title: 'Roles' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/admin/usuarios">Usuarios</Link>
          <Link to="/mantenimiento/oficinas">Oficinas</Link>
        </Space>
      }
      toolbar={
        <Space wrap>
          <Input.Search
            placeholder="Buscar rol"
            allowClear
            value={filtro}
            onChange={(e) => setFiltro(e.target.value)}
            style={{ width: '100%', maxWidth: 280 }}
          />
          <Checkbox
            checked={incluirInactivos}
            onChange={(e) => setIncluirInactivos(e.target.checked)}
          >
            Incluir inactivos
          </Checkbox>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Nuevo rol
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void rolesQuery.refetch()}
            loading={rolesQuery.isFetching}
          >
            Actualizar
          </Button>
        </Space>
      }
      extra={
        <Drawer
          title={rolId >= 1 ? `Rol #${rolId}` : 'Nuevo rol'}
          width={screens.md ? 560 : '100%'}
          open={drawerOpen}
          onClose={closeDrawer}
          destroyOnClose
        >
          <Tabs
            className="credix-tabs"
            items={[
              {
                key: 'datos',
                label: 'Datos',
                children: (
                  <Form form={form} layout="vertical">
                    <Form.Item
                      name="denominacion"
                      label="Denominación"
                      rules={[{ required: true, message: 'Obligatorio' }]}
                    >
                      <Input maxLength={100} />
                    </Form.Item>
                    <Form.Item name="estado" label="Activo" valuePropName="checked">
                      <Switch />
                    </Form.Item>
                    <Button
                      type="primary"
                      loading={guardar.isPending}
                      onClick={() => {
                        void form.validateFields().then((v) => {
                          guardar.mutate({
                            rolId,
                            denominacion: v.denominacion,
                            estado: v.estado ?? true,
                          })
                        })
                      }}
                    >
                      Guardar rol
                    </Button>
                  </Form>
                ),
              },
              {
                key: 'menus',
                label: 'Menú',
                disabled: rolId < 1,
                children: (
                  <Form form={form} layout="vertical">
                    <Paragraph type="secondary">
                      Opciones de menú asignables al rol.
                    </Paragraph>
                    <Form.Item name="menuIds" label="Menús">
                      <Select
                        mode="multiple"
                        allowClear
                        showSearch
                        optionFilterProp="label"
                        loading={menusQuery.isLoading}
                        disabled={rolId < 1}
                        options={menuOptions}
                        style={{ width: '100%' }}
                      />
                    </Form.Item>
                    <Button
                      type="primary"
                      disabled={rolId < 1}
                      loading={asignarMenus.isPending}
                      onClick={() => {
                        const menuIds = (form.getFieldValue('menuIds') as number[]) ?? []
                        asignarMenus.mutate({ id: rolId, menuIds })
                      }}
                    >
                      Guardar menús
                    </Button>
                  </Form>
                ),
              },
            ]}
          />
        </Drawer>
      }
    >
      <CredixDataTable<RolGestionRow>
        rowKey="rolId"
        columns={columns}
        dataSource={filtradas}
        loading={rolesQuery.isLoading}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} rol(es)` }}
        locale={{ emptyText: 'No hay roles' }}
      />
    </CredixCrudPage>
  )
}
