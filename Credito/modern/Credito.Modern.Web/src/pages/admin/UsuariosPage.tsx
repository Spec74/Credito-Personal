import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query'
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
  const [verRed, setVerRed] = useState(true)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [usuarioId, setUsuarioId] = useState(0)
  const [oficinaRolId, setOficinaRolId] = useState<number | undefined>()
  const [form] = Form.useForm()
  const queryClient = useQueryClient()

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

  const esCentral = usuariosQuery.data?.esCentral === true
  const oficinaSesionId = usuariosQuery.data?.oficinaId ?? 0
  const oficinaSesionNombre = usuariosQuery.data?.oficina?.trim() || 'esta sucursal'

  const oficinasAsignadas = useMemo(
    () => (detalleQuery.data?.oficinas ?? []).filter((o) => o.asignado),
    [detalleQuery.data?.oficinas],
  )

  /** Sucursal solo edita roles de su sede. Central elige. */
  const oficinaRolEfectiva = esCentral
    ? (oficinaRolId ?? oficinasAsignadas[0]?.oficinaId)
    : oficinaSesionId >= 1
      ? oficinaSesionId
      : oficinasAsignadas[0]?.oficinaId

  const oficinaRolLabel =
    oficinasAsignadas.find((o) => o.oficinaId === oficinaRolEfectiva)?.denominacion ??
    'oficina'

  const rolesQuery = useQuery({
    queryKey: ['usuario-roles', usuarioId, oficinaRolEfectiva],
    queryFn: () => fetchRolesAsignacion(usuarioId, oficinaRolEfectiva!),
    enabled: usuarioId >= 1 && oficinaRolEfectiva != null && oficinaRolEfectiva >= 1,
  })

  /** Resumen de roles en TODAS las oficinas asignadas (evita creer que “no tiene admin” cuando sí lo tiene en Pichari). */
  const rolesResumenQueries = useQueries({
    queries: oficinasAsignadas.map((o) => ({
      queryKey: ['usuario-roles', usuarioId, o.oficinaId] as const,
      queryFn: () => fetchRolesAsignacion(usuarioId, o.oficinaId),
      enabled: usuarioId >= 1 && drawerOpen && o.oficinaId >= 1,
    })),
  })

  const rolesPorOficina = useMemo(() => {
    return oficinasAsignadas.map((o, i) => {
      const q = rolesResumenQueries[i]
      const nombres =
        q?.data
          ?.filter((r) => r.asignado)
          .map((r) => r.denominacion)
          .filter(Boolean) ?? []
      return {
        oficinaId: o.oficinaId,
        denominacion: o.denominacion,
        roles: nombres,
        loading: q?.isLoading ?? false,
      }
    })
  }, [oficinasAsignadas, rolesResumenQueries])

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

  const mostrarCredencialesReset = (nombreUsuario: string, claveTemporal: string) => {
    const usuario = nombreUsuario.trim() || form.getFieldValue('nombreUsuario') || '—'
    Modal.success({
      title: 'Clave restablecida',
      width: 440,
      content: (
        <Space direction="vertical" size="middle" style={{ width: '100%', marginTop: 8 }}>
          <Paragraph type="secondary" style={{ marginBottom: 0 }}>
            Clave temporal generada. El usuario deberá cambiarla en el próximo inicio de sesión.
            La clave anterior no se puede recuperar (está cifrada).
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
              <Input.Password value={claveTemporal} readOnly visibilityToggle />
              <Button
                icon={<CopyOutlined />}
                onClick={() => {
                  void navigator.clipboard.writeText(claveTemporal).then(() =>
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
      mostrarCredencialesReset(nombreUsuario, claveTemporal)
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
      message.success('Oficinas asignadas. Revise Roles en cada sede nueva.')
      void detalleQuery.refetch()
      void queryClient.invalidateQueries({ queryKey: ['usuario-roles', usuarioId] })
    },
  })

  const asignarRoles = useMutation({
    mutationFn: ({ oficinaId, rolIds }: { oficinaId: number; rolIds: number[] }) =>
      asignarRolesUsuario(usuarioId, oficinaId, rolIds),
    onSuccess: (res, vars) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      const sede =
        oficinasAsignadas.find((o) => o.oficinaId === vars.oficinaId)?.denominacion ??
        `oficina #${vars.oficinaId}`
      message.success(`Roles guardados en ${sede}`)
      void queryClient.invalidateQueries({ queryKey: ['usuario-roles', usuarioId] })
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

  useEffect(() => {
    const roles = rolesQuery.data
    if (!roles) return
    form.setFieldValue(
      'rolIds',
      roles.filter((r) => r.asignado).map((r) => r.rolId),
    )
  }, [rolesQuery.data, form])

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

  const oficinasOptions = detalleQuery.data?.oficinas ?? []
  const usuariosRows = usuariosQuery.data?.rows ?? []
  const stats = useCrudListStats(usuariosRows, 'usuarios')

  return (
    <CredixCrudPage
      title="Usuarios"
      subtitle={
        esCentral
          ? verRed
            ? 'Toda la red. Puede filtrar por sede al editar.'
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
                          description="Por seguridad se guarda cifrada (hash). El ojito solo muestra lo que usted escriba ahora. Use «Resetear clave» para generar una temporal (el usuario deberá cambiarla al iniciar sesión) o escriba una clave nueva y guarde."
                        />
                      ) : null}
                      <Form.Item
                        name="claveUsuario"
                        label={usuarioId >= 1 ? 'Nueva clave (opcional)' : 'Clave'}
                        rules={claveUsuarioRules(usuarioId < 1)}
                        extra={
                          usuarioId >= 1
                            ? 'Vacío = no cambiar. Mínimo 8 caracteres, con letra y número.'
                            : 'Mínimo 8 caracteres, con letra y número.'
                        }
                      >
                        <Input.Password
                          visibilityToggle
                          placeholder={
                            usuarioId >= 1
                              ? 'Dejar vacío para no cambiar'
                              : 'Mínimo 8 caracteres, letra y número'
                          }
                          autoComplete="new-password"
                        />
                      </Form.Item>
                      {esCentral ? (
                        <Form.Item name="estado" label="Activo" valuePropName="checked">
                          <Switch />
                        </Form.Item>
                      ) : (
                        <Alert
                          type="info"
                          showIcon
                          style={{ marginBottom: 12 }}
                          message={`Se asigna a ${oficinaSesionNombre}`}
                          description={
                            usuarioId < 1
                              ? 'Al guardar queda en esta sucursal. Luego defina sus roles.'
                              : 'Para quitarlo use «Quitar de sede» en la lista. No se apaga la cuenta de otras sucursales.'
                          }
                        />
                      )}
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
                              estado: v.estado !== false,
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
                                  Se generará una clave temporal para el usuario{' '}
                                  <Text strong>
                                    {(form.getFieldValue('nombreUsuario') as string) || '…'}
                                  </Text>
                                  . Deberá cambiarla en el próximo inicio de sesión. La clave
                                  anterior quedará invalidada (no es recuperable).
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
                  children: esCentral ? (
                    <>
                      <Paragraph type="secondary">
                        Central puede asignar varias sedes. Los roles de una sede que quite se
                        borran.
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
                  ) : (
                    <>
                      <Paragraph>
                        Este usuario está en <Text strong>{oficinaSesionNombre}</Text>.
                      </Paragraph>
                      {(detalleQuery.data?.otrasSedes ?? []).length > 0 ? (
                        <Alert
                          type="info"
                          showIcon
                          message="También trabaja en otras sedes"
                          description={(detalleQuery.data?.otrasSedes ?? []).join(', ')}
                        />
                      ) : (
                        <Paragraph type="secondary">
                          No está asignado a otras sucursales. Central es quien lo habilita en más
                          de una sede.
                        </Paragraph>
                      )}
                    </>
                  ),
                },
                {
                  key: 'roles',
                  label: 'Roles',
                  disabled: usuarioId < 1,
                  children: (
                    <>
                      <Alert
                        type="info"
                        showIcon
                        style={{ marginBottom: 12 }}
                        message={
                          esCentral
                            ? 'Roles por sede: elija oficina y marque solo los de esa sucursal.'
                            : `Roles solo de ${oficinaSesionNombre}.`
                        }
                      />

                      {oficinasAsignadas.length === 0 ? (
                        <Alert
                          type="warning"
                          showIcon
                          style={{ marginBottom: 12 }}
                          message="Asigne al menos una oficina en la pestaña Oficinas antes de definir roles."
                        />
                      ) : (
                        <div style={{ marginBottom: 16 }}>
                          <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                            Resumen actual (todas las sedes del usuario)
                          </Text>
                          <Space direction="vertical" size={8} style={{ width: '100%' }}>
                            {rolesPorOficina.map((row) => (
                              <div key={row.oficinaId}>
                                <Space wrap size={[4, 4]} align="center">
                                  <Button
                                    type={
                                      row.oficinaId === oficinaRolEfectiva ? 'primary' : 'default'
                                    }
                                    size="small"
                                    disabled={!esCentral && row.oficinaId !== oficinaSesionId}
                                    onClick={() => {
                                      if (!esCentral && row.oficinaId !== oficinaSesionId) return
                                      setOficinaRolId(row.oficinaId)
                                    }}
                                  >
                                    {row.denominacion}
                                  </Button>
                                  {row.loading ? (
                                    <Tag>Cargando…</Tag>
                                  ) : row.roles.length === 0 ? (
                                    <Tag color="orange">Sin roles</Tag>
                                  ) : (
                                    row.roles.map((nombre) => (
                                      <Tag key={`${row.oficinaId}-${nombre}`} color="blue">
                                        {nombre}
                                      </Tag>
                                    ))
                                  )}
                                </Space>
                              </div>
                            ))}
                          </Space>
                        </div>
                      )}

                      <Paragraph type="secondary" style={{ marginBottom: 8 }}>
                        Editando roles de:{' '}
                        <Text strong>{oficinaRolLabel}</Text>
                        {oficinaRolEfectiva != null ? (
                          <>
                            {' '}
                            <Text type="secondary">(id {oficinaRolEfectiva})</Text>
                          </>
                        ) : null}
                      </Paragraph>
                      {esCentral ? (
                        <Select
                          placeholder="Oficina para editar roles"
                          style={{ width: '100%', marginBottom: 12 }}
                          value={oficinaRolEfectiva}
                          onChange={(v) => setOficinaRolId(v)}
                          options={oficinasAsignadas.map((o) => ({
                            value: o.oficinaId,
                            label: o.denominacion,
                          }))}
                        />
                      ) : null}
                      <Form.Item
                        name="rolIds"
                        label={`Roles en ${oficinaRolLabel}`}
                        rules={[
                          {
                            validator: async (_, value: number[] | undefined) => {
                              const ids = (value ?? []).filter((id) => id >= 1)
                              if (ids.length < 1) {
                                throw new Error('Debe asignar al menos un rol')
                              }
                            },
                          },
                        ]}
                      >
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
                          void form.validateFields(['rolIds']).then((v) => {
                            const rolIds = ((v.rolIds as number[]) ?? []).filter((id) => id >= 1)
                            if (rolIds.length < 1) {
                              message.warning('Debe asignar al menos un rol')
                              return
                            }
                            asignarRoles.mutate({ oficinaId: oficinaRolEfectiva!, rolIds })
                          })
                        }}
                      >
                        Guardar roles en {oficinaRolLabel}
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
