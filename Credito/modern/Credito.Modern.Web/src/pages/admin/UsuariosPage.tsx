import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
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
import {
  CopyOutlined,
  EditOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import {
  celularPeRule,
  direccionRealistaRule,
  disabledFechaNacimiento,
  dniRule,
  emailRule,
  FIELD_MAX,
  fechaNacimientoRule,
  maxLen,
  required,
  requiredText,
} from '../../validation/formRules'
import dayjs from 'dayjs'
import { consultarDniApiPeru } from '../../api/apiperu'
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

const { Paragraph, Text } = Typography

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

  const consultarReniec = useMutation({
    mutationFn: async (dniRaw: string) => {
      const dni = dniRaw.replace(/\D/g, '')
      if (dni.length !== 8) {
        throw new Error('Ingrese un DNI de 8 dígitos')
      }
      const existe = await validarDniUsuario(dni)
      if (existe.existe && usuarioId < 1) {
        throw new Error('Ya existe un usuario con ese DNI')
      }
      return consultarDniApiPeru(dni)
    },
    onSuccess: (r) => {
      const tieneDatos = Boolean(
        r.success && (r.nombres?.trim() || r.apellidoPaterno?.trim() || r.apellidoMaterno?.trim()),
      )
      if (!tieneDatos) {
        message.warning(
          r.mensaje?.trim() ||
            'No se encontraron datos en ApiPerú. Complete nombre y apellidos manualmente.',
        )
        return
      }
      form.setFieldsValue({
        nombre: r.nombres ?? '',
        apePaterno: r.apellidoPaterno ?? '',
        apeMaterno: r.apellidoMaterno ?? '',
      })
      message.success('Datos completados desde ApiPerú')
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error ApiPerú'),
  })

  const CLAVE_RESET_TEMPORAL = '123456'

  const mostrarCredencialesReset = (nombreUsuario: string) => {
    const usuario = nombreUsuario.trim() || form.getFieldValue('nombreUsuario') || '—'
    Modal.success({
      title: 'Clave restablecida',
      width: 440,
      content: (
        <Space direction="vertical" size="middle" style={{ width: '100%', marginTop: 8 }}>
          <Paragraph type="secondary" style={{ marginBottom: 0 }}>
            La clave anterior no se puede recuperar (está cifrada). Use estas credenciales
            temporales para revisar o indicar al usuario que cambie su clave.
          </Paragraph>
          <div>
            <Text type="secondary">Usuario</Text>
            <Space.Compact style={{ width: '100%', marginTop: 4 }}>
              <Input value={usuario} readOnly />
              <Button
                icon={<CopyOutlined />}
                onClick={() => {
                  void navigator.clipboard.writeText(String(usuario)).then(() =>
                    message.success('Usuario copiado'),
                  )
                }}
              />
            </Space.Compact>
          </div>
          <div>
            <Text type="secondary">Clave temporal</Text>
            <Space.Compact style={{ width: '100%', marginTop: 4 }}>
              <Input.Password value={CLAVE_RESET_TEMPORAL} readOnly visibilityToggle />
              <Button
                icon={<CopyOutlined />}
                onClick={() => {
                  void navigator.clipboard.writeText(CLAVE_RESET_TEMPORAL).then(() =>
                    message.success('Clave copiada'),
                  )
                }}
              />
            </Space.Compact>
          </div>
        </Space>
      ),
    })
  }

  const resetClave = useMutation({
    mutationFn: resetearClaveUsuario,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      const nombreUsuario =
        (form.getFieldValue('nombreUsuario') as string | undefined)?.trim() ||
        detalleQuery.data?.nombreUsuario ||
        ''
      form.setFieldValue('claveUsuario', '')
      mostrarCredencialesReset(nombreUsuario)
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'No se pudo restablecer la clave'),
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
      // Vacío = no cambiar. La clave real no se puede mostrar: solo hay hash PBKDF2 en BD.
      claveUsuario: '',
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
          <Link to="/mantenimiento/oficinas">Oficinas</Link>
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
                        rules={[required('DNI obligatorio'), dniRule]}
                        extra={
                          usuarioId < 1
                            ? 'Ingrese el DNI y pulse Validar ApiPerú para completar nombres y apellidos.'
                            : undefined
                        }
                      >
                        <Space.Compact style={{ width: '100%' }}>
                          <Input
                            maxLength={FIELD_MAX.dni}
                            disabled={usuarioId >= 1}
                            inputMode="numeric"
                            onPressEnter={() => {
                              if (usuarioId >= 1) return
                              const dni = form.getFieldValue('numeroDocumento') as string
                              if (dni?.trim()) consultarReniec.mutate(dni)
                            }}
                          />
                          {usuarioId < 1 ? (
                            <Button
                              type="default"
                              icon={<SearchOutlined />}
                              loading={consultarReniec.isPending}
                              onClick={() => {
                                const dni = form.getFieldValue('numeroDocumento') as string
                                consultarReniec.mutate(dni ?? '')
                              }}
                            >
                              Validar ApiPerú
                            </Button>
                          ) : null}
                        </Space.Compact>
                      </Form.Item>
                      <Space wrap style={{ width: '100%' }}>
                        <Form.Item name="apePaterno" label="Ap. paterno" rules={requiredText(FIELD_MAX.nombre)}>
                          <Input style={{ width: 140 }} maxLength={FIELD_MAX.nombre} />
                        </Form.Item>
                        <Form.Item name="apeMaterno" label="Ap. materno" rules={requiredText(FIELD_MAX.nombre)}>
                          <Input style={{ width: 140 }} maxLength={FIELD_MAX.nombre} />
                        </Form.Item>
                        <Form.Item name="nombre" label="Nombres" rules={requiredText(FIELD_MAX.nombre)}>
                          <Input style={{ width: 160 }} maxLength={FIELD_MAX.nombre} />
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
                      <Form.Item
                        name="fechaNacimiento"
                        label="Fecha nacimiento"
                        rules={[fechaNacimientoRule(18)]}
                        extra="Mínimo 18 años; no se admiten fechas futuras."
                      >
                        <CredixDatePicker disabledDate={(d) => disabledFechaNacimiento(d, 18)} />
                      </Form.Item>
                      <Form.Item name="telefonoMovil" label="Celular" rules={[celularPeRule]}>
                        <Input maxLength={FIELD_MAX.celular} inputMode="numeric" />
                      </Form.Item>
                      <Form.Item name="emailPersonal" label="Email" rules={[emailRule, maxLen(FIELD_MAX.email)]}>
                        <Input type="email" maxLength={FIELD_MAX.email} />
                      </Form.Item>
                      <Form.Item
                        name="direccion"
                        label="Dirección"
                        rules={[direccionRealistaRule('dirección'), maxLen(FIELD_MAX.direccion)]}
                      >
                        <Input.TextArea
                          rows={2}
                          maxLength={FIELD_MAX.direccion}
                          placeholder="Jr. / Av. + número (opcional pero debe ser legible)"
                        />
                      </Form.Item>
                      <Form.Item
                        name="nombreUsuario"
                        label="Nombre usuario"
                        rules={requiredText(FIELD_MAX.usuario)}
                      >
                        <Input maxLength={FIELD_MAX.usuario} />
                      </Form.Item>
                      {usuarioId >= 1 ? (
                        <Alert
                          type="info"
                          showIcon
                          style={{ marginBottom: 12 }}
                          message="La clave actual no se puede ver"
                          description="Por seguridad se guarda cifrada (hash). El ojito solo muestra lo que usted escriba ahora. Para revisar como ese usuario: Resetear clave (queda 123456) o escriba una clave temporal nueva y guarde."
                        />
                      ) : null}
                      <Form.Item
                        name="claveUsuario"
                        label={usuarioId >= 1 ? 'Nueva clave (opcional)' : 'Clave'}
                        rules={
                          usuarioId < 1
                            ? [
                                { required: true, message: 'Clave obligatoria' },
                                { min: 6, message: 'Mínimo 6 caracteres' },
                              ]
                            : [
                                {
                                  validator: async (_, value: string | undefined) => {
                                    const v = (value ?? '').trim()
                                    if (!v) return
                                    if (v.length < 6) {
                                      throw new Error('Mínimo 6 caracteres')
                                    }
                                  },
                                },
                              ]
                        }
                        extra={
                          usuarioId >= 1
                            ? 'Vacío = no cambiar. Si escribe una nueva, el ojito la muestra al tipearla.'
                            : 'Use el ojito para verificar lo que escribe.'
                        }
                      >
                        <Input.Password
                          visibilityToggle
                          placeholder={usuarioId >= 1 ? 'Dejar vacío para no cambiar' : 'Mínimo 6 caracteres'}
                          autoComplete="new-password"
                        />
                      </Form.Item>
                      <Form.Item name="estado" label="Activo" valuePropName="checked">
                        <Switch />
                      </Form.Item>
                      <Button
                        type="primary"
                        loading={guardar.isPending}
                        onClick={() => {
                          void form.validateFields().then((v) => {
                            const claveNueva = ((v.claveUsuario as string) ?? '').trim()
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
                              // Vacío en edición = no tocar clave (backend ignora blanco / ********).
                              claveUsuario: claveNueva,
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
                          loading={resetClave.isPending}
                          onClick={() =>
                            Modal.confirm({
                              title: '¿Restablecer clave temporal?',
                              content: (
                                <Paragraph style={{ marginBottom: 0 }}>
                                  Se asignará la clave <Text code>{CLAVE_RESET_TEMPORAL}</Text> al
                                  usuario{' '}
                                  <Text strong>
                                    {(form.getFieldValue('nombreUsuario') as string) || '…'}
                                  </Text>
                                  . La clave anterior quedará invalidada (no es recuperable).
                                </Paragraph>
                              ),
                              okText: 'Restablecer',
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
