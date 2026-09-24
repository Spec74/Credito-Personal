import { useEffect, useState } from 'react'
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
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import dayjs from 'dayjs'
import {
  activarUsuario,
  asignarOficinasUsuario,
  asignarRolesUsuario,
  fetchRolesAsignacion,
  fetchUsuarioDetalle,
  fetchUsuariosGestion,
  guardarUsuario,
  resetearClaveUsuario,
  validarDniUsuario,
  type UsuarioGestionRow,
} from '../../api/usuariosAdmin'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable, CredixDatePicker } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

const { Paragraph } = Typography

export function UsuariosPage() {
  const screens = Grid.useBreakpoint()
  const [buscar, setBuscar] = useState('')
  const buscarDebounced = useDebouncedValue(buscar.trim(), 350)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [usuarioId, setUsuarioId] = useState(0)
  const [oficinaRolId, setOficinaRolId] = useState<number | undefined>()
  const [form] = Form.useForm()
  const queryClient = useQueryClient()

  const usuariosQuery = useQuery({
    queryKey: ['usuarios-gestion', buscarDebounced, page, pageSize, incluirInactivos],
    queryFn: () =>
      fetchUsuariosGestion({
        buscar: buscarDebounced,
        page,
        pageSize,
        incluirInactivos,
      }),
  })

  const detalleQuery = useQuery({
    queryKey: ['usuario-detalle', usuarioId],
    queryFn: () => fetchUsuarioDetalle(usuarioId),
    enabled: usuarioId >= 1 && drawerOpen,
  })

  const oficinaRolEfectiva =
    oficinaRolId ??
    detalleQuery.data?.oficinas.find((o) => o.asignado)?.oficinaId

  const rolesQuery = useQuery({
    queryKey: ['usuario-roles', usuarioId, oficinaRolEfectiva],
    queryFn: () => fetchRolesAsignacion(usuarioId, oficinaRolEfectiva!),
    enabled: usuarioId >= 1 && oficinaRolEfectiva != null && oficinaRolEfectiva >= 1,
  })

  const guardar = useMutation({
    mutationFn: guardarUsuario,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Usuario guardado')
      const id = res.id ?? usuarioId
      if (id && id >= 1) setUsuarioId(id)
      void queryClient.invalidateQueries({ queryKey: ['usuarios-gestion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  const activar = useMutation({
    mutationFn: activarUsuario,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['usuarios-gestion'] })
    },
  })

  const resetClave = useMutation({
    mutationFn: resetearClaveUsuario,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Clave restablecida a 123456')
    },
  })

  const asignarOficinas = useMutation({
    mutationFn: (ids: number[]) => asignarOficinasUsuario(usuarioId, ids),
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Oficinas asignadas')
      void detalleQuery.refetch()
    },
  })

  const asignarRoles = useMutation({
    mutationFn: ({ oficinaId, rolIds }: { oficinaId: number; rolIds: number[] }) =>
      asignarRolesUsuario(usuarioId, oficinaId, rolIds),
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Roles asignados')
      void rolesQuery.refetch()
    },
  })

  useEffect(() => {
    const d = detalleQuery.data
    if (!d) return
    form.setFieldsValue({
      apePaterno: d.apePaterno,
      apeMaterno: d.apeMaterno,
      nombre: d.nombre,
      numeroDocumento: d.numeroDocumento,
      sexo: d.sexo ?? 'M',
      fechaNacimiento: d.fechaNacimiento ? dayjs(d.fechaNacimiento) : null,
      telefonoMovil: d.celular1 ?? '',
      emailPersonal: d.emailPersonal ?? '',
      direccion: d.direccion ?? '',
      nombreUsuario: d.nombreUsuario,
      claveUsuario: '********',
      estado: d.estado,
    })
    const asignadas = d.oficinas.filter((o) => o.asignado).map((o) => o.oficinaId)
    form.setFieldValue('oficinaIds', asignadas)
  }, [detalleQuery.data, form])

  const openCreate = () => {
    setUsuarioId(0)
    setOficinaRolId(undefined)
    form.resetFields()
    form.setFieldsValue({
      sexo: 'M',
      estado: true,
      claveUsuario: '',
      oficinaIds: [],
      rolIds: [],
    })
    setDrawerOpen(true)
  }

  const openEdit = (row: UsuarioGestionRow) => {
    setUsuarioId(row.usuarioId)
    setOficinaRolId(undefined)
    setDrawerOpen(true)
  }

  const columns: ColumnsType<UsuarioGestionRow> = [
    { title: 'Id', dataIndex: 'usuarioId', width: 70 },
    { title: 'Usuario', dataIndex: 'nombreUsuario', width: 120 },
    { title: 'Nombre', dataIndex: 'nombreCompleto', ellipsis: true },
    { title: 'Celular', dataIndex: 'celular1', width: 100 },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 90,
      render: (v: boolean) => (v ? <Tag color="green">Activo</Tag> : <Tag>Inactivo</Tag>),
    },
    {
      title: '',
      key: 'acc',
      width: 200,
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
                title: row.estado ? '¿Desactivar usuario?' : '¿Activar usuario?',
                onOk: () => activar.mutateAsync(row.usuarioId),
              })
            }
          >
            {row.estado ? 'Desactivar' : 'Activar'}
          </Button>
        </Space>
      ),
    },
  ]

  const pagination: TablePaginationConfig = {
    current: page,
    pageSize,
    total: usuariosQuery.data?.totalRecords ?? 0,
    showSizeChanger: true,
    onChange: (p, ps) => {
      setPage(p)
      setPageSize(ps)
    },
  }

  const oficinasOptions = detalleQuery.data?.oficinas ?? []
  const usuariosRows = usuariosQuery.data?.rows ?? []
  const stats = useCrudListStats(usuariosRows, 'usuarios')

  return (
    <CredixCrudPage
      title="Usuarios"
      subtitle="Persona, credenciales, oficinas y roles por oficina."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/admin">Administración</Link> },
        { title: 'Usuarios' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/admin/roles">Roles</Link>
          <Link to="/admin/oficinas">Oficinas</Link>
        </Space>
      }
      toolbar={
        <Space wrap>
          <Input.Search
            allowClear
            placeholder="Buscar nombre de usuario"
            style={{ width: '100%', maxWidth: 280 }}
            value={buscar}
            onChange={(e) => {
              setBuscar(e.target.value)
              setPage(1)
            }}
            onSearch={() => void usuariosQuery.refetch()}
          />
          <Checkbox
            checked={incluirInactivos}
            onChange={(e) => {
              setIncluirInactivos(e.target.checked)
              setPage(1)
            }}
          >
            Incluir inactivos
          </Checkbox>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Nuevo usuario
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void usuariosQuery.refetch()}
            loading={usuariosQuery.isFetching}
          >
            Actualizar
          </Button>
        </Space>
      }
      extra={
        <Drawer
          title={usuarioId >= 1 ? `Usuario #${usuarioId}` : 'Nuevo usuario'}
          width={screens.md ? 640 : '100%'}
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
        >
          <Form form={form} layout="vertical">
            <Tabs
              className="credix-tabs"
              items={[
                {
                  key: 'datos',
                  label: 'Datos',
                  children: (
                    <>
                      <Form.Item
                        name="numeroDocumento"
                        label="DNI"
                        rules={[{ required: true }]}
                      >
                        <Input
                          maxLength={12}
                          disabled={usuarioId >= 1}
                          onBlur={async () => {
                            const dni = form.getFieldValue('numeroDocumento') as string
                            if (!dni?.trim() || usuarioId >= 1) return
                            const v = await validarDniUsuario(dni)
                            if (v.existe) message.warning('Ya existe un usuario con ese DNI')
                          }}
                        />
                      </Form.Item>
                      <Space wrap style={{ width: '100%' }}>
                        <Form.Item name="apePaterno" label="Ap. paterno" rules={[{ required: true }]}>
                          <Input style={{ width: 140 }} />
                        </Form.Item>
                        <Form.Item name="apeMaterno" label="Ap. materno" rules={[{ required: true }]}>
                          <Input style={{ width: 140 }} />
                        </Form.Item>
                        <Form.Item name="nombre" label="Nombres" rules={[{ required: true }]}>
                          <Input style={{ width: 160 }} />
                        </Form.Item>
                      </Space>
                      <Form.Item name="sexo" label="Sexo" rules={[{ required: true }]}>
                        <Select
                          options={[
                            { value: 'M', label: 'Masculino' },
                            { value: 'F', label: 'Femenino' },
                          ]}
                        />
                      </Form.Item>
                      <Form.Item name="fechaNacimiento" label="Fecha nacimiento">
                        <CredixDatePicker />
                      </Form.Item>
                      <Form.Item name="telefonoMovil" label="Celular">
                        <Input maxLength={10} />
                      </Form.Item>
                      <Form.Item name="emailPersonal" label="Email">
                        <Input type="email" />
                      </Form.Item>
                      <Form.Item name="direccion" label="Dirección">
                        <Input.TextArea rows={2} />
                      </Form.Item>
                      <Form.Item
                        name="nombreUsuario"
                        label="Nombre usuario"
                        rules={[{ required: true }]}
                      >
                        <Input maxLength={50} />
                      </Form.Item>
                      <Form.Item
                        name="claveUsuario"
                        label="Clave"
                        rules={usuarioId < 1 ? [{ required: true }] : []}
                      >
                        <Input.Password placeholder={usuarioId >= 1 ? 'Dejar ******** para no cambiar' : ''} />
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
                              usuarioId,
                              apePaterno: v.apePaterno,
                              apeMaterno: v.apeMaterno,
                              nombre: v.nombre,
                              numeroDocumento: v.numeroDocumento,
                              sexo: v.sexo,
                              fechaNacimiento: v.fechaNacimiento
                                ? (v.fechaNacimiento as dayjs.Dayjs).format('YYYY-MM-DD')
                                : null,
                              telefonoMovil: v.telefonoMovil,
                              emailPersonal: v.emailPersonal,
                              direccion: v.direccion,
                              nombreUsuario: v.nombreUsuario,
                              claveUsuario: (v.claveUsuario as string) ?? '',
                              estado: v.estado,
                            })
                          })
                        }}
                      >
                        Guardar datos
                      </Button>
                      {usuarioId >= 1 ? (
                        <Button
                          style={{ marginLeft: 8 }}
                          onClick={() =>
                            Modal.confirm({
                              title: '¿Restablecer clave a 123456?',
                              onOk: () => resetClave.mutateAsync(usuarioId),
                            })
                          }
                        >
                          Resetear clave
                        </Button>
                      ) : null}
                    </>
                  ),
                },
                {
                  key: 'oficinas',
                  label: 'Oficinas',
                  disabled: usuarioId < 1,
                  children: (
                    <>
                      <Paragraph type="secondary">
                        Guarde el usuario primero. Reemplaza todas las oficinas asignadas.
                      </Paragraph>
                      <Form.Item name="oficinaIds" label="Oficinas">
                        <Select
                          mode="multiple"
                          loading={detalleQuery.isLoading}
                          options={oficinasOptions.map((o) => ({
                            value: o.oficinaId,
                            label: o.denominacion,
                          }))}
                        />
                      </Form.Item>
                      <Button
                        type="primary"
                        loading={asignarOficinas.isPending}
                        onClick={() => {
                          const ids = (form.getFieldValue('oficinaIds') as number[]) ?? []
                          asignarOficinas.mutate(ids)
                        }}
                      >
                        Guardar oficinas
                      </Button>
                    </>
                  ),
                },
                {
                  key: 'roles',
                  label: 'Roles',
                  disabled: usuarioId < 1,
                  children: (
                    <>
                      <Paragraph type="secondary">
                        Roles por oficina; el login usa los roles de la oficina elegida.
                      </Paragraph>
                      <Select
                        placeholder="Oficina para roles"
                        style={{ width: '100%', marginBottom: 12 }}
                        value={oficinaRolEfectiva}
                        onChange={(v) => {
                          setOficinaRolId(v)
                          form.setFieldValue('rolIds', [])
                        }}
                        options={oficinasOptions
                          .filter((o) => o.asignado)
                          .map((o) => ({ value: o.oficinaId, label: o.denominacion }))}
                      />
                      <Form.Item name="rolIds" label="Roles">
                        <Select
                          mode="multiple"
                          loading={rolesQuery.isLoading}
                          disabled={!oficinaRolEfectiva}
                          options={(rolesQuery.data ?? []).map((r) => ({
                            value: r.rolId,
                            label: r.denominacion,
                          }))}
                        />
                      </Form.Item>
                      <Button
                        type="primary"
                        disabled={!oficinaRolEfectiva}
                        loading={asignarRoles.isPending}
                        onClick={() => {
                          const rolIds = (form.getFieldValue('rolIds') as number[]) ?? []
                          asignarRoles.mutate({ oficinaId: oficinaRolEfectiva!, rolIds })
                        }}
                      >
                        Guardar roles
                      </Button>
                    </>
                  ),
                },
              ]}
            />
          </Form>
        </Drawer>
      }
    >
      <CredixDataTable<UsuarioGestionRow>
        rowKey="usuarioId"
        columns={columns}
        dataSource={usuariosQuery.data?.rows ?? []}
        loading={usuariosQuery.isLoading}
        pagination={pagination}
        locale={{ emptyText: 'No hay usuarios' }}
      />
    </CredixCrudPage>
  )
}
