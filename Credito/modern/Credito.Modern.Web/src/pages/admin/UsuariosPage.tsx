import { useEffect, useRef, useState } from 'react'
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
  Tag,
  Typography,
  message,
} from 'antd'
import {
  CopyOutlined,
  EditOutlined,
  KeyOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import {
  celularPeRule,
  claveUsuarioRules,
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
import { fetchOficinas } from '../../api/oficinas'
import { fetchRolesGestion } from '../../api/rolesAdmin'
import {
  activarUsuario,
  fetchRolesAsignacion,
  fetchUsuarioDetalle,
  fetchUsuariosGestion,
  generarClaveTemporal,
  guardarUsuario,
  resetearClaveUsuario,
  sugerirNombreUsuario,
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
  const [verRed, setVerRed] = useState(true)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [usuarioId, setUsuarioId] = useState(0)
  const [form] = Form.useForm()
  const queryClient = useQueryClient()
  const credencialesNuevas = useRef<{ usuario: string; clave: string } | null>(null)

  const usuariosQuery = useQuery({
    queryKey: ['usuarios-gestion', buscarDebounced, page, pageSize, incluirInactivos, verRed],
    queryFn: () =>
      fetchUsuariosGestion({
        buscar: buscarDebounced,
        page,
        pageSize,
        incluirInactivos,
        verRed,
      }),
  })

  const detalleQuery = useQuery({
    queryKey: ['usuario-detalle', usuarioId],
    queryFn: () => fetchUsuarioDetalle(usuarioId),
    enabled: usuarioId >= 1 && drawerOpen,
  })

  const oficinasQuery = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
    enabled: drawerOpen,
  })

  const rolesCatalogoQuery = useQuery({
    queryKey: ['roles-gestion', false],
    queryFn: () => fetchRolesGestion(false),
    enabled: drawerOpen,
  })

  const esCentral = usuariosQuery.data?.esCentral === true
  const oficinaSesionId = usuariosQuery.data?.oficinaId ?? 0
  const oficinaSesionNombre = usuariosQuery.data?.oficina?.trim() || 'esta sucursal'

  const guardar = useMutation({
    mutationFn: guardarUsuario,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success(usuarioId < 1 ? 'Usuario creado con oficina y rol' : 'Usuario actualizado')
      const creds = credencialesNuevas.current
      credencialesNuevas.current = null
      if (creds) {
        mostrarCredenciales(
          'Usuario listo',
          creds.usuario,
          creds.clave,
          'Guarde estas credenciales para entregárselas a la persona. No se vuelven a mostrar.',
        )
      }
      setDrawerOpen(false)
      void queryClient.invalidateQueries({ queryKey: ['usuarios-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['usuario-detalle'] })
      void queryClient.invalidateQueries({ queryKey: ['usuario-roles'] })
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
      const nombre = r.nombres ?? ''
      const apePaterno = r.apellidoPaterno ?? ''
      form.setFieldsValue({
        nombre,
        apePaterno,
        apeMaterno: r.apellidoMaterno ?? '',
      })
      const actual = String(form.getFieldValue('nombreUsuario') ?? '').trim()
      if (!actual) {
        const sugerido = sugerirNombreUsuario(nombre, apePaterno)
        if (sugerido) form.setFieldValue('nombreUsuario', sugerido)
      }
      message.success('Datos completados desde ApiPerú')
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error ApiPerú'),
  })

  const mostrarCredenciales = (titulo: string, nombreUsuario: string, clave: string, extra?: string) => {
    const usuario = nombreUsuario.trim() || '—'
    Modal.success({
      title: titulo,
      width: 440,
      content: (
        <Space direction="vertical" size="middle" style={{ width: '100%', marginTop: 8 }}>
          {extra ? (
            <Paragraph type="secondary" style={{ marginBottom: 0 }}>
              {extra}
            </Paragraph>
          ) : null}
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
            <Text type="secondary">Clave</Text>
            <Space.Compact style={{ width: '100%', marginTop: 4 }}>
              <Input.Password value={clave} readOnly visibilityToggle />
              <Button
                icon={<CopyOutlined />}
                onClick={() => {
                  void navigator.clipboard.writeText(clave).then(() => message.success('Clave copiada'))
                }}
              />
            </Space.Compact>
          </div>
          <Button
            block
            onClick={() => {
              void navigator.clipboard.writeText(`${usuario}\n${clave}`).then(() =>
                message.success('Usuario y clave copiados'),
              )
            }}
          >
            Copiar usuario y clave
          </Button>
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
      const claveTemporal = (res.claveTemporal ?? '').trim()
      if (!claveTemporal) {
        message.error(res.mensaje ?? 'No se recibió la clave temporal')
        return
      }
      const nombreUsuario =
        (form.getFieldValue('nombreUsuario') as string | undefined)?.trim() ||
        detalleQuery.data?.nombreUsuario ||
        ''
      form.setFieldValue('claveUsuario', '')
      mostrarCredenciales(
        'Clave restablecida',
        nombreUsuario,
        claveTemporal,
        'Clave temporal. El usuario deberá cambiarla en el próximo inicio de sesión. La anterior no se puede recuperar.',
      )
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'No se pudo restablecer la clave'),
  })

  useEffect(() => {
    const d = detalleQuery.data
    if (!d || usuarioId < 1) return
    const asignadas = d.oficinas.filter((o) => o.asignado).map((o) => o.oficinaId)
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
      claveUsuario: '',
      estado: d.estado,
      oficinaIds: esCentral ? asignadas : [oficinaSesionId].filter((id) => id >= 1),
    })
  }, [detalleQuery.data, esCentral, form, oficinaSesionId, usuarioId])

  useEffect(() => {
    if (usuarioId < 1 || !drawerOpen) return
    const oficinaRol = esCentral
      ? ((form.getFieldValue('oficinaIds') as number[] | undefined)?.[0] ??
        detalleQuery.data?.oficinas.find((o) => o.asignado)?.oficinaId)
      : oficinaSesionId
    if (!oficinaRol || oficinaRol < 1) return
    void fetchRolesAsignacion(usuarioId, oficinaRol).then((roles) => {
      form.setFieldValue(
        'rolIds',
        roles.filter((r) => r.asignado).map((r) => r.rolId),
      )
    })
  }, [detalleQuery.data, drawerOpen, esCentral, form, oficinaSesionId, usuarioId])

  const aplicarClaveGenerada = () => {
    const clave = generarClaveTemporal()
    form.setFieldValue('claveUsuario', clave)
    message.success('Clave generada. Cópiela antes de guardar.')
    return clave
  }

  const openCreate = () => {
    setUsuarioId(0)
    form.resetFields()
    form.setFieldsValue({
      sexo: 'M',
      estado: true,
      claveUsuario: generarClaveTemporal(),
      oficinaIds: oficinaSesionId >= 1 ? [oficinaSesionId] : [],
      rolIds: [],
    })
    setDrawerOpen(true)
  }

  const openEdit = (row: UsuarioGestionRow) => {
    setUsuarioId(row.usuarioId)
    setDrawerOpen(true)
  }

  const guardarTodo = () => {
    void form.validateFields().then((v) => {
      const oficinaIds = esCentral
        ? (((v.oficinaIds as number[]) ?? []).filter((id) => id >= 1))
        : oficinaSesionId >= 1
          ? [oficinaSesionId]
          : []
      const rolIds = ((v.rolIds as number[]) ?? []).filter((id) => id >= 1)
      if (oficinaIds.length < 1) {
        message.warning('Seleccione al menos una oficina')
        return
      }
      if (rolIds.length < 1) {
        message.warning('Seleccione al menos un rol')
        return
      }
      const claveNueva = ((v.claveUsuario as string) ?? '').trim()
      const nombreUsuario = String(v.nombreUsuario ?? '').trim()
      credencialesNuevas.current =
        usuarioId < 1 && claveNueva ? { usuario: nombreUsuario, clave: claveNueva } : null
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
        nombreUsuario,
        claveUsuario: claveNueva,
        estado: v.estado !== false,
        oficinaIds,
        rolIds,
      })
    })
  }

  const columns: ColumnsType<UsuarioGestionRow> = [
    { title: 'Id', dataIndex: 'usuarioId', width: 70 },
    { title: 'Usuario', dataIndex: 'nombreUsuario', width: 120 },
    { title: 'Nombre', dataIndex: 'nombreCompleto', ellipsis: true },
    { title: 'Celular', dataIndex: 'celular1', width: 100 },
    ...(esCentral
      ? [
          {
            title: 'Sedes',
            dataIndex: 'sedes',
            ellipsis: true,
            render: (v: string | null) => v || '—',
          } satisfies ColumnsType<UsuarioGestionRow>[number],
        ]
      : []),
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 110,
      render: (v: boolean, row: UsuarioGestionRow) =>
        esCentral ? (
          v ? <Tag color="green">Activo</Tag> : <Tag>Inactivo</Tag>
        ) : row.enEstaSede && v ? (
          <Tag color="green">En esta sede</Tag>
        ) : (
          <Tag>Fuera de sede</Tag>
        ),
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
                title: esCentral
                  ? row.estado
                    ? '¿Desactivar la cuenta?'
                    : '¿Activar la cuenta?'
                  : row.enEstaSede
                    ? '¿Quitar de esta sucursal?'
                    : '¿Reincorporar a esta sucursal?',
                content: esCentral
                  ? 'Afecta el acceso en todas las sedes.'
                  : row.enEstaSede
                    ? 'Sigue existiendo. Si tiene otras sedes, ahí puede seguir entrando.'
                    : `Vuelve a ${oficinaSesionNombre}.`,
                onOk: () => activar.mutateAsync(row.usuarioId),
              })
            }
          >
            {esCentral
              ? row.estado
                ? 'Desactivar'
                : 'Activar'
              : row.enEstaSede
                ? 'Quitar de sede'
                : 'Reincorporar'}
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

  const usuariosRows = usuariosQuery.data?.rows ?? []
  const stats = useCrudListStats(usuariosRows, 'usuarios')
  const oficinasOptions = (oficinasQuery.data ?? []).map((o) => ({
    value: o.oficinaId,
    label: o.denominacion ?? `Oficina ${o.oficinaId}`,
  }))
  const rolesOptions = (rolesCatalogoQuery.data ?? []).map((r) => ({
    value: r.rolId,
    label: r.denominacion,
  }))

  return (
    <CredixCrudPage
      title="Usuarios"
      subtitle={
        esCentral
          ? verRed
            ? 'Toda la red. Un solo guardado: datos, sede y rol.'
            : `Personal de ${oficinaSesionNombre}`
          : `Personal de ${oficinaSesionNombre}`
      }
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
          {esCentral ? (
            <Checkbox
              checked={verRed}
              onChange={(e) => {
                setVerRed(e.target.checked)
                setPage(1)
              }}
            >
              Ver toda la red
            </Checkbox>
          ) : null}
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
          destroyOnClose
          footer={
            <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
              <Button onClick={() => setDrawerOpen(false)}>Cancelar</Button>
              <Button type="primary" loading={guardar.isPending} onClick={guardarTodo}>
                Guardar
              </Button>
            </Space>
          }
        >
          <Form form={form} layout="vertical" requiredMark>
            <div style={{ marginBottom: 8 }}>
              <Text strong>1. Identidad</Text>
              <Paragraph type="secondary" style={{ marginTop: 4 }}>
                Valide el DNI para completar nombres. Luego defina acceso y asignación.
              </Paragraph>
            </div>
            <Form.Item
              name="numeroDocumento"
              label="DNI"
              rules={[required('DNI obligatorio'), dniRule]}
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
                    icon={<SearchOutlined />}
                    loading={consultarReniec.isPending}
                    onClick={() => {
                      const dni = form.getFieldValue('numeroDocumento') as string
                      consultarReniec.mutate(dni ?? '')
                    }}
                  >
                    Validar DNI
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
              <Input.TextArea rows={2} maxLength={FIELD_MAX.direccion} />
            </Form.Item>

            <div style={{ margin: '16px 0 8px' }}>
              <Text strong>2. Acceso</Text>
              <Paragraph type="secondary" style={{ marginTop: 4 }}>
                {usuarioId < 1
                  ? 'Ya hay una clave generada. Puede copiarla o generar otra. Mínimo 8 caracteres, con letra y número.'
                  : 'Deje la clave vacía para no cambiarla. «Generar» escribe una nueva; «Restablecer» la aplica ya y obliga a cambiarla al entrar.'}
              </Paragraph>
            </div>
            <Form.Item name="nombreUsuario" label="Usuario de ingreso" rules={requiredText(FIELD_MAX.usuario)}>
              <Input maxLength={FIELD_MAX.usuario} autoComplete="off" />
            </Form.Item>
            <Form.Item
              name="claveUsuario"
              label={usuarioId >= 1 ? 'Nueva clave (opcional)' : 'Clave'}
              rules={claveUsuarioRules(usuarioId < 1)}
            >
              <Input.Password
                visibilityToggle
                placeholder={usuarioId >= 1 ? 'Vacío = no cambiar' : 'Clave generada o la que usted elija'}
                autoComplete="new-password"
              />
            </Form.Item>
            <Space wrap style={{ marginBottom: 20, display: 'flex' }}>
              <Button icon={<KeyOutlined />} onClick={() => aplicarClaveGenerada()}>
                Generar clave
              </Button>
              <Button
                icon={<CopyOutlined />}
                onClick={() => {
                  const usuario = String(form.getFieldValue('nombreUsuario') ?? '').trim()
                  const clave = String(form.getFieldValue('claveUsuario') ?? '').trim()
                  if (!clave) {
                    message.warning('No hay clave para copiar')
                    return
                  }
                  void navigator.clipboard.writeText(usuario ? `${usuario}\n${clave}` : clave).then(() =>
                    message.success('Copiado'),
                  )
                }}
              >
                Copiar
              </Button>
              {usuarioId >= 1 ? (
                <Button
                  loading={resetClave.isPending}
                  onClick={() =>
                    Modal.confirm({
                      title: '¿Restablecer clave ahora?',
                      content:
                        'Se genera una clave temporal e invalida la actual. El usuario deberá cambiarla al iniciar sesión. No espera al botón Guardar.',
                      okText: 'Restablecer',
                      onOk: () => resetClave.mutateAsync(usuarioId),
                    })
                  }
                >
                  Restablecer ahora
                </Button>
              ) : null}
            </Space>

            <div style={{ margin: '16px 0 8px' }}>
              <Text strong>3. Oficina y rol</Text>
              <Paragraph type="secondary" style={{ marginTop: 4 }}>
                {esCentral
                  ? 'Los mismos roles se aplican a todas las sedes que marque. Un solo Guardar alcanza.'
                  : `Queda en ${oficinaSesionNombre}. Elija al menos un rol.`}
              </Paragraph>
            </div>
            {esCentral ? (
              <Form.Item
                name="oficinaIds"
                label="Oficinas"
                rules={[{ required: true, type: 'array', min: 1, message: 'Seleccione al menos una oficina' }]}
              >
                <Select
                  mode="multiple"
                  loading={oficinasQuery.isLoading}
                  options={oficinasOptions}
                  placeholder="Sedes donde podrá entrar"
                />
              </Form.Item>
            ) : (
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 12 }}
                message={`Oficina: ${oficinaSesionNombre}`}
                description="Central es quien habilita más de una sede."
              />
            )}
            <Form.Item
              name="rolIds"
              label="Roles"
              rules={[{ required: true, type: 'array', min: 1, message: 'Seleccione al menos un rol' }]}
            >
              <Select
                mode="multiple"
                loading={rolesCatalogoQuery.isLoading}
                options={rolesOptions}
                placeholder="Qué puede hacer en el sistema"
              />
            </Form.Item>
            {esCentral ? (
              <Form.Item name="estado" label="Activo" valuePropName="checked">
                <Switch />
              </Form.Item>
            ) : null}
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
