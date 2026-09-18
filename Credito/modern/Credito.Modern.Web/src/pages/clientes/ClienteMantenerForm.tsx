import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  AutoComplete,
  Button,
  Checkbox,
  Col,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Radio,
  Row,
  Select,
  Space,
  Skeleton,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import {
  EnvironmentOutlined,
  FileSearchOutlined,
  FileAddOutlined,
  LockOutlined,
  PlusOutlined,
  SaveOutlined,
  UnlockOutlined,
} from '@ant-design/icons'
import dayjs, { type Dayjs } from 'dayjs'
import { consultarDniApiPeru, consultarRucApiPeru } from '../../api/apiperu'
import {
  documentoYaEsCliente,
  guardarCliente,
  habilitarClienteDepurado,
  obtenerCliente,
  obtenerPersonaPorDocumento,
  buscarDistritosCliente,
  toggleClienteActivo,
  toggleClienteBloqueado,
  type GuardarClienteRequest,
} from '../../api/clientes'
import { fetchOcupaciones } from '../../api/ocupaciones'
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { GoogleMapLocationPicker } from '../../components/maps/GoogleMapLocationPicker'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import {
  esCreditoAdministrador,
  esCreditoAprobador1,
  puedeEditarTopeCreditoUi,
} from '../../utils/creditoOperacionPermisos'
import { geocodeDistritoCliente, geocodeDomicilioCliente } from '../../utils/googleGeocode'
import { type MapLatLng, toMapLatLng } from '../../config/googleMaps'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { ConyugueAutoComplete } from './components/ConyugueAutoComplete'
import { CrearPersonaRapidaModal } from './components/CrearPersonaRapidaModal'
import { buildPrendarioNuevoHref, parseInternalPath, withSearchParam } from '../../utils/internalReturnTo'
import {
  CALIFICACIONES,
  ESTADO_CIVIL_CONYUGE,
  REGLA_CELULAR,
  TABLA_ESTADO_CIVIL,
  TABLA_RIESGO_SBS,
  TABLA_TIPO_VIVIENDA,
} from './clienteMantenerConstants'

const { Text } = Typography

interface FormValues {
  tipoPersona: 'N' | 'J'
  nombre: string
  apePaterno?: string
  apeMaterno?: string
  numeroDocumento: string
  sexoMasculino: boolean
  email?: string
  celular1?: string
  nota?: string
  fechaNacimiento?: Dayjs | null
  direccion?: string
  direccionRef?: string
  distritoLabel?: string
  direccionNegocio?: string
  direccionNegocioRef?: string
  ocupacionId?: number
  ocupacionOtros?: string
  calificacion: string
  activo: boolean
  topeCredito?: number | null
  estadoCivilId?: number
  tipoViviendaId?: number
  conyugueLabel?: string
  clasificacionRiesgoSbsId?: number
  clasificacionRiesgoSbsObs?: string
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

type Props = {
  esEdicion: boolean
  personaId: number
}

export function ClienteMantenerForm({ esEdicion, personaId }: Props) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const returnTo = parseInternalPath(searchParams.get('returnTo'))
  const dniPrefill = (searchParams.get('dni') ?? '').replace(/\D/g, '')
  const dniPrefillDone = useRef(false)
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const roles = session?.roles ?? []
  const puedeAprobador = esCreditoAprobador1(roles) || esCreditoAdministrador(roles)
  const puedeTope = puedeEditarTopeCreditoUi(roles)
  const puedeRiesgo = puedeAprobador

  const [form] = Form.useForm<FormValues>()
  const [bloqueado, setBloqueado] = useState(false)
  const [distritoId, setDistritoId] = useState<number | null>(null)
  const [conyuguePersonaId, setConyuguePersonaId] = useState<number | null>(null)
  const [mapLocation, setMapLocation] = useState<MapLatLng | null>(null)
  const [nombresBloqueados, setNombresBloqueados] = useState(!esEdicion)
  const [avalOpen, setAvalOpen] = useState(false)
  const [distritoTerm, setDistritoTerm] = useState('')
  const [guardarDestino, setGuardarDestino] = useState<'listado' | 'credito' | 'prendario'>('listado')
  const puedePrendario =
    esCreditoAdministrador(roles) || roles.some((r) => r.trim().toUpperCase() === 'ANALISTA')
  const debouncedDistrito = useDebouncedValue(distritoTerm.trim(), 300)
  const documentoOriginalRef = useRef('')
  const geocodificadoInicialRef = useRef(false)

  const tipoPersona = Form.useWatch('tipoPersona', form) ?? 'N'
  const estadoCivilId = Form.useWatch('estadoCivilId', form)
  const ocupacionId = Form.useWatch('ocupacionId', form)

  const detalle = useQuery({
    queryKey: ['cliente-detalle', personaId],
    queryFn: () => obtenerCliente(personaId),
    enabled: esEdicion && personaId > 0,
    retry: false,
  })

  const ocupaciones = useQuery({ queryKey: ['ocupaciones'], queryFn: fetchOcupaciones })
  const estadoCivil = useQuery({
    queryKey: ['valores-tabla', TABLA_ESTADO_CIVIL],
    queryFn: () => fetchValoresTabla(TABLA_ESTADO_CIVIL),
  })
  const tipoVivienda = useQuery({
    queryKey: ['valores-tabla', TABLA_TIPO_VIVIENDA],
    queryFn: () => fetchValoresTabla(TABLA_TIPO_VIVIENDA),
  })
  const riesgoSbs = useQuery({
    queryKey: ['valores-tabla', TABLA_RIESGO_SBS],
    queryFn: () => fetchValoresTabla(TABLA_RIESGO_SBS),
  })

  const distritosQuery = useQuery({
    queryKey: ['distritos-buscar', debouncedDistrito],
    queryFn: () => buscarDistritosCliente(debouncedDistrito),
    enabled: debouncedDistrito.length >= 2,
  })

  const ocupacionOtrosVisible = useMemo(() => {
    const occ = ocupaciones.data?.find((o) => o.ocupacionId === ocupacionId)
    return occ?.denominacion?.toUpperCase().includes('OTROS') ?? false
  }, [ocupaciones.data, ocupacionId])

  const clienteId = detalle.data?.clienteId ?? 0

  useEffect(() => {
    if (!detalle.data) return
    const c = detalle.data
    setBloqueado(c.bloqueado)
    setDistritoId(c.distritoId ?? null)
    setConyuguePersonaId(c.conyuguePersonaId ?? null)
    setMapLocation(toMapLatLng(c.latitud, c.longitud))
    setDistritoTerm(c.distritoLabel ?? '')
    form.setFieldsValue({
      tipoPersona: c.tipoPersona === 'J' ? 'J' : 'N',
      nombre: c.nombre,
      apePaterno: c.apePaterno ?? undefined,
      apeMaterno: c.apeMaterno ?? undefined,
      numeroDocumento: c.numeroDocumento,
      sexoMasculino: c.sexo !== 'F',
      email: c.email ?? undefined,
      celular1: c.celular1 ?? undefined,
      nota: c.nota ?? undefined,
      fechaNacimiento: c.fechaNacimiento ? dayjs(c.fechaNacimiento) : null,
      direccion: c.direccion ?? undefined,
      direccionRef: c.direccionRef ?? undefined,
      distritoLabel: c.distritoLabel ?? undefined,
      direccionNegocio: c.direccionNegocio ?? undefined,
      direccionNegocioRef: c.direccionNegocioRef ?? undefined,
      ocupacionId: c.actividadEconId ?? undefined,
      calificacion: c.calificacion || 'A',
      activo: c.clienteEstado,
      topeCredito: c.topeCredito ?? undefined,
      estadoCivilId: c.estadoCivilId ?? undefined,
      tipoViviendaId: c.tipoViviendaId ?? undefined,
      conyugueLabel: c.conyugueLabel ?? undefined,
      clasificacionRiesgoSbsId: c.clasificacionRiesgoSbsId ?? undefined,
      clasificacionRiesgoSbsObs: c.clasificacionRiesgoSbsObs ?? undefined,
    })
    setNombresBloqueados(!puedeAprobador)
    documentoOriginalRef.current = c.numeroDocumento.trim()
    geocodificadoInicialRef.current = false
  }, [detalle.data, form, puedeAprobador])

  useEffect(() => {
    if (!detalle.data || geocodificadoInicialRef.current) return
    if (mapLocation) {
      geocodificadoInicialRef.current = true
      return
    }
    const distrito = detalle.data.distritoLabel?.trim()
    if (!distrito) return
    geocodificadoInicialRef.current = true
    void geocodeDistritoCliente(distrito).then((pos) => {
      if (pos) setMapLocation(pos)
    })
  }, [detalle.data, mapLocation])

  const buildPayload = (values: FormValues): GuardarClienteRequest => ({
    clienteId,
    tipoPersona: values.tipoPersona,
    nombre: values.nombre.trim().toUpperCase(),
    apePaterno: values.tipoPersona === 'N' ? values.apePaterno?.trim().toUpperCase() : null,
    apeMaterno: values.tipoPersona === 'N' ? values.apeMaterno?.trim().toUpperCase() : null,
    numeroDocumento: values.numeroDocumento.trim(),
    sexoMasculino: values.sexoMasculino,
    email: values.email?.trim() || null,
    celular1: values.celular1?.trim() || null,
    nota: values.nota?.trim() || null,
    fechaNacimiento: values.fechaNacimiento
      ? values.fechaNacimiento.format('YYYY-MM-DD')
      : null,
    direccion: values.direccion?.trim() || null,
    direccionRef: values.direccionRef?.trim() || null,
    distritoId,
    direccionNegocio: values.direccionNegocio?.trim() || null,
    direccionNegocioRef: values.direccionNegocioRef?.trim() || null,
    latitud: mapLocation?.lat ?? null,
    longitud: mapLocation?.lng ?? null,
    ocupacionId: ocupacionOtrosVisible ? null : values.ocupacionId ?? null,
    ocupacionOtros: ocupacionOtrosVisible ? values.ocupacionOtros?.trim() : null,
    calificacion: values.calificacion,
    activo: values.activo,
    topeCredito: values.topeCredito ?? null,
    estadoCivilId: values.estadoCivilId ?? null,
    tipoViviendaId: values.tipoViviendaId ?? null,
    conyuguePersonaId: ESTADO_CIVIL_CONYUGE.has(values.estadoCivilId ?? 0)
      ? conyuguePersonaId
      : null,
    clasificacionRiesgoSbsId: values.clasificacionRiesgoSbsId ?? null,
    clasificacionRiesgoSbsObs: values.clasificacionRiesgoSbsObs?.trim() || null,
  })

  const guardar = useMutation({
    mutationFn: (values: FormValues) => guardarCliente(buildPayload(values)),
    onSuccess: (r) => {
      message.success('Cliente guardado')
      void queryClient.invalidateQueries({ queryKey: ['cliente-detalle'] })
      void queryClient.invalidateQueries({ queryKey: ['clientes-listar'] })
      void queryClient.invalidateQueries({ queryKey: ['credito-clientes-buscar'] })
      if (returnTo) {
        navigate(
          withSearchParam(withSearchParam(returnTo, 'personaId', String(r.personaId)), 'origen', 'alta'),
          { replace: true },
        )
        return
      }
      if (guardarDestino === 'prendario') {
        navigate(buildPrendarioNuevoHref(r.personaId, 'alta'), { replace: true })
        return
      }
      if (guardarDestino === 'credito') {
        navigate(`/credito/simulador?personaId=${r.personaId}`, { replace: true })
        return
      }
      navigate('/clientes', { replace: true })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const handleFinish = async (values: FormValues) => {
    const doc = values.numeroDocumento.trim()
    if (doc !== documentoOriginalRef.current) {
      const yaCliente = await documentoYaEsCliente(doc, esEdicion ? personaId : undefined)
      if (yaCliente) {
        message.error('Ya existe un cliente con este documento')
        return
      }
    }
    guardar.mutate(values)
  }

  const validarReniec = async () => {
    const doc = form.getFieldValue('numeroDocumento')?.trim() ?? ''
    if (!doc) return
    try {
      const yaCliente = await documentoYaEsCliente(doc, esEdicion ? personaId : undefined)
      if (yaCliente) {
        message.error('Ya existe un cliente con este documento')
        form.setFieldsValue({ numeroDocumento: '', nombre: '', apePaterno: '', apeMaterno: '' })
        return
      }
      if (tipoPersona === 'N') {
        const r = await consultarDniApiPeru(doc)
        const tieneDatos = Boolean(
          r.success && (r.nombres?.trim() || r.apellidoPaterno?.trim() || r.apellidoMaterno?.trim()),
        )
        if (!tieneDatos) {
          setNombresBloqueados(false)
          message.info(
            r.mensaje?.trim() ||
              'No se encontraron datos para este DNI. Puede completar los nombres manualmente.',
          )
          return
        }
        form.setFieldsValue({
          nombre: r.nombres ?? '',
          apePaterno: r.apellidoPaterno ?? '',
          apeMaterno: r.apellidoMaterno ?? '',
        })
      } else {
        const r = await consultarRucApiPeru(doc)
        const tieneDatos = Boolean(r.success && r.razonSocial?.trim())
        if (!tieneDatos) {
          setNombresBloqueados(false)
          message.info(
            r.mensaje?.trim() ||
              'No se encontraron datos para este RUC. Puede completar los datos manualmente.',
          )
          return
        }
        form.setFieldsValue({
          nombre: r.razonSocial ?? '',
          direccion: r.direccion ?? form.getFieldValue('direccion'),
        })
      }
      setNombresBloqueados(false)
      message.success('Datos validados correctamente')
    } catch (e) {
      setNombresBloqueados(false)
      message.info(
        `${errMsg(e)}. Puede completar nombre y apellidos manualmente.`,
      )
    }
  }

  useEffect(() => {
    if (esEdicion || dniPrefillDone.current || dniPrefill.length !== 8) return
    dniPrefillDone.current = true
    form.setFieldValue('numeroDocumento', dniPrefill)
    void (async () => {
      try {
        const p = await obtenerPersonaPorDocumento(dniPrefill)
        if (p?.tieneCliente) {
          message.info('Este DNI ya está registrado como cliente')
          if (returnTo) {
            navigate(withSearchParam(returnTo, 'personaId', String(p.personaId)), { replace: true })
            return
          }
          navigate(`/clientes/editar/${p.personaId}`, { replace: true })
          return
        }
        if (p) {
          form.setFieldsValue({
            nombre: p.nombre,
            apePaterno: p.apePaterno ?? undefined,
            apeMaterno: p.apeMaterno ?? undefined,
            sexoMasculino: p.sexo !== 'F',
          })
          setNombresBloqueados(false)
          return
        }
        await validarReniec()
      } catch (e) {
        message.error(errMsg(e))
      }
    })()
  }, [dniPrefill, esEdicion, form, navigate, returnTo])

  const ubicarMapa = async () => {
    const distrito = form.getFieldValue('distritoLabel')?.trim() ?? distritoTerm.trim()
    if (!distrito) {
      message.warning('Seleccione o ingrese el distrito antes de ubicar en el mapa.')
      return
    }
    const pos = await geocodeDomicilioCliente(
      form.getFieldValue('direccion')?.trim() ?? '',
      distrito,
    )
    if (!pos) {
      message.warning(
        'No se encontró la ubicación. Ajuste el marcador manualmente en Google Maps.',
      )
      return
    }
    setMapLocation(pos)
    message.success('Ubicación actualizada en el mapa')
  }

  const stats: CredixStatItem[] = useMemo(
    () => [
      { value: esEdicion ? 'Edición' : 'Alta', label: 'Modo' },
      {
        value: detalle.data?.numeroDocumento ?? '—',
        label: detalle.data?.tipoPersona === 'J' ? 'RUC' : 'DNI',
      },
      { value: detalle.data?.calificacion ?? 'A', label: 'Calificación' },
      { value: bloqueado ? 'Sí' : 'No', label: 'Bloqueado', tone: bloqueado ? 'red' : undefined },
    ],
    [esEdicion, detalle.data, bloqueado],
  )

  if (esEdicion && detalle.isLoading) {
    return (
      <CredixPage
        className="cliente-mantener-page"
        title="Mantener cliente"
        subtitle="Cargando ficha…"
        breadcrumb={[
          { title: <Link to="/inicio">Inicio</Link> },
          { title: <Link to="/clientes">Clientes</Link> },
          { title: '…' },
        ]}
      >
        <div className="cliente-mantener__loading">
          <Skeleton active paragraph={{ rows: 10 }} />
          <span className="cliente-mantener__loading-text">Cargando datos del cliente…</span>
        </div>
      </CredixPage>
    )
  }

  if (esEdicion && detalle.isError) {
    return (
      <CredixPage
        className="cliente-mantener-page"
        title="Mantener cliente"
        breadcrumb={[
          { title: <Link to="/inicio">Inicio</Link> },
          { title: <Link to="/clientes">Clientes</Link> },
        ]}
      >
        <Alert
          type="error"
          showIcon
          message={errMsg(detalle.error)}
          action={
            <Link to="/clientes">
              <Button size="small">Volver al listado</Button>
            </Link>
          }
        />
      </CredixPage>
    )
  }

  const distritosOptions =
    distritosQuery.data?.map((d) => ({
      value: d.label,
      label: d.label,
      id: d.personaId,
    })) ?? []

  return (
    <CredixPage
      className="cliente-mantener-page"
      title={esEdicion ? 'Mantener cliente' : 'Nuevo cliente'}
      subtitle={
        <>
          Identificación, domicilio con <strong>Google Maps</strong> y calificación crediticia.
          Validación <Tag className="cliente-mantener__doc-tag">ApiPeru</Tag> en servidor.
          {returnTo ? ' Tras guardar volverá a la solicitud de origen.' : null}
        </>
      }
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/clientes">Clientes</Link> },
        ...(returnTo ? [{ title: <Link to={returnTo}>Volver</Link> }] : []),
        { title: esEdicion ? `Cliente #${personaId}` : 'Nuevo' },
      ]}
    >
      {detalle.data?.depuradoDescripcion ? (
        <Alert
          className="cliente-mantener__depurado"
          type="error"
          showIcon
          message="Cliente depurado"
          description={detalle.data.depuradoDescripcion}
          action={
            puedeAprobador ? (
              <Button
                size="small"
                type="primary"
                danger
                onClick={() =>
                  habilitarClienteDepurado(personaId).then(() => {
                    message.success('Cliente habilitado')
                    detalle.refetch()
                  })
                }
              >
                Habilitar
              </Button>
            ) : undefined
          }
        />
      ) : null}

      <Form<FormValues>
        form={form}
        layout="vertical"
        initialValues={{
          tipoPersona: 'N',
          sexoMasculino: true,
          calificacion: 'A',
          activo: true,
        }}
        onFinish={(v) => void handleFinish(v)}
      >
        <Tabs
          className="cliente-mantener-tabs"
          type="card"
          defaultActiveKey="identidad"
          items={[
            {
              key: 'identidad',
              label: 'Identificación',
              children: (
                <CredixPanel>
                  <Row gutter={16}>
                    <Col xs={24} md={8}>
                      <Form.Item name="tipoPersona" label="Tipo persona">
                        <Radio.Group
                          disabled={esEdicion}
                          optionType="button"
                          buttonStyle="solid"
                          options={[
                            { value: 'N', label: 'Natural' },
                            { value: 'J', label: 'Jurídica' },
                          ]}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={10}>
                      <Form.Item
                        name="numeroDocumento"
                        label={tipoPersona === 'N' ? 'DNI' : 'RUC'}
                        rules={[{ required: true }]}
                      >
                        <Input
                          maxLength={tipoPersona === 'N' ? 8 : 11}
                          disabled={esEdicion && !puedeAprobador}
                          onBlur={async (e) => {
                            if (esEdicion) return
                            const doc = e.target.value.trim()
                            if (doc.length < 8) return
                            const p = await obtenerPersonaPorDocumento(doc)
                            if (!p) return
                            if (p.tieneCliente) {
                              navigate(`/clientes/editar/${p.personaId}`)
                              return
                            }
                            form.setFieldsValue({
                              nombre: p.nombre,
                              apePaterno: p.apePaterno ?? undefined,
                              apeMaterno: p.apeMaterno ?? undefined,
                              sexoMasculino: p.sexo !== 'F',
                            })
                          }}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={6}>
                      <Form.Item label=" ">
                        <Button
                          block
                          icon={<FileSearchOutlined />}
                          onClick={() => void validarReniec()}
                          disabled={esEdicion && !puedeAprobador}
                        >
                          Validar (ApiPeru)
                        </Button>
                      </Form.Item>
                    </Col>
                  </Row>
                  <Row gutter={16}>
                    <Col xs={24} md={tipoPersona === 'N' ? 8 : 16}>
                      <Form.Item name="nombre" label={tipoPersona === 'N' ? 'Nombres' : 'Razón social'} rules={[{ required: true }]}>
                        <Input readOnly={nombresBloqueados} />
                      </Form.Item>
                    </Col>
                    {tipoPersona === 'N' && (
                      <>
                        <Col xs={24} md={8}>
                          <Form.Item name="apePaterno" label="Apellido paterno" rules={[{ required: true }]}>
                            <Input readOnly={nombresBloqueados} />
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={8}>
                          <Form.Item name="apeMaterno" label="Apellido materno" rules={[{ required: true }]}>
                            <Input readOnly={nombresBloqueados} />
                          </Form.Item>
                        </Col>
                      </>
                    )}
                  </Row>
                  {tipoPersona === 'N' && (
                    <Row gutter={16}>
                      <Col xs={24} md={10}>
                        <Form.Item name="sexoMasculino" label="Sexo">
                          <Radio.Group
                            options={[
                              { value: true, label: 'Masculino' },
                              { value: false, label: 'Femenino' },
                            ]}
                          />
                        </Form.Item>
                      </Col>
                      <Col xs={24} md={9}>
                        <Form.Item name="fechaNacimiento" label="Fecha nacimiento">
                          <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
                        </Form.Item>
                      </Col>
                    </Row>
                  )}
                  <Row gutter={16}>
                    <Col xs={24} md={8}>
                      <Form.Item name="celular1" label="Celular" rules={[REGLA_CELULAR]}>
                        <Input maxLength={10} inputMode="numeric" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={8}>
                      <Form.Item name="email" label="Correo">
                        <Input type="email" />
                      </Form.Item>
                    </Col>
                  </Row>
                </CredixPanel>
              ),
            },
            {
              key: 'ubicacion',
              label: 'Ubicación',
              children: (
                <CredixPanel>
                  <p className="cliente-mantener__section-title">Domicilio y negocio</p>
                  <Form.Item name="direccion" label="Domicilio">
                    <Input.TextArea rows={2} />
                  </Form.Item>
                  <Form.Item name="direccionRef" label="Referencia domicilio">
                    <Input />
                  </Form.Item>
                  <Form.Item name="distritoLabel" label="Distrito (Ayacucho)">
                    <AutoComplete
                      value={distritoTerm}
                      options={distritosOptions}
                      onSearch={setDistritoTerm}
                      onSelect={async (_, opt) => {
                        const id = (opt as { id?: number }).id
                        const label = String(opt.value ?? '')
                        setDistritoId(id ?? null)
                        setDistritoTerm(label)
                        form.setFieldValue('distritoLabel', label)
                        const pos = await geocodeDistritoCliente(label)
                        if (pos) {
                          setMapLocation(pos)
                          message.success(`Ubicando ${label} en el mapa`)
                        }
                      }}
                      placeholder="Buscar distrito…"
                    />
                  </Form.Item>
                  <Form.Item name="direccionNegocio" label="Dirección negocio">
                    <Input />
                  </Form.Item>
                  <Form.Item name="direccionNegocioRef" label="Referencia negocio">
                    <Input />
                  </Form.Item>
                  <div className="cliente-mantener__map-block">
                    <div className="cliente-mantener__map-actions">
                      <Button
                        type="primary"
                        icon={<EnvironmentOutlined />}
                        onClick={() => void ubicarMapa()}
                      >
                        Ubicar domicilio en el mapa
                      </Button>
                    </div>
                    <GoogleMapLocationPicker
                      layoutKey={`cliente-${personaId}-${esEdicion}`}
                      value={mapLocation}
                      onChange={setMapLocation}
                      height={320}
                      searchPlaceholder="Buscar en Google Maps…"
                      hintText="Google Maps: clic, arrastre del marcador o búsqueda. Paridad legacy con geocodificación por distrito + domicilio."
                    />
                  </div>
                </CredixPanel>
              ),
            },
            {
              key: 'calificacion',
              label: (
                <span>
                  Calificación
                  <span className="cliente-mantener__calificacion-badge">
                    {form.getFieldValue('calificacion') ?? detalle.data?.calificacion ?? 'A'}
                  </span>
                </span>
              ),
              children: (
                <CredixPanel>
                  <Row gutter={16}>
                    <Col xs={24} md={8}>
                      <Form.Item name="topeCredito" label="Tope crédito">
                        <InputNumber
                          style={{ width: '100%' }}
                          min={0}
                          disabled={!puedeTope}
                          addonBefore="S/."
                          precision={2}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={8}>
                      <Form.Item name="ocupacionId" label="Giro / ocupación">
                        <Select
                          allowClear
                          showSearch
                          optionFilterProp="label"
                          loading={ocupaciones.isLoading}
                          options={(ocupaciones.data ?? []).map((o) => ({
                            value: o.ocupacionId,
                            label: o.denominacion,
                          }))}
                        />
                      </Form.Item>
                    </Col>
                    {ocupacionOtrosVisible && (
                      <Col xs={24} md={8}>
                        <Form.Item name="ocupacionOtros" label="Otro giro" rules={[{ required: true }]}>
                          <Input placeholder="Denominación nueva" />
                        </Form.Item>
                      </Col>
                    )}
                    <Col xs={24} md={4}>
                      <Form.Item name="calificacion" label="Calificación">
                        <Select
                          options={[...CALIFICACIONES]}
                          disabled={esEdicion}
                          title={esEdicion ? 'La calificación se conserva del registro (paridad legacy)' : undefined}
                        />
                      </Form.Item>
                    </Col>
                  </Row>
                  <Row gutter={16}>
                    <Col xs={24} md={8}>
                      <Form.Item name="estadoCivilId" label="Estado civil">
                        <Select
                          allowClear
                          options={(estadoCivil.data ?? []).map((v) => ({
                            value: v.itemId,
                            label: v.denominacion,
                          }))}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={8}>
                      <Form.Item name="tipoViviendaId" label="Tipo vivienda">
                        <Select
                          allowClear
                          options={(tipoVivienda.data ?? []).map((v) => ({
                            value: v.itemId,
                            label: v.denominacion,
                          }))}
                        />
                      </Form.Item>
                    </Col>
                  </Row>
                  {ESTADO_CIVIL_CONYUGE.has(estadoCivilId ?? 0) && (
                    <div className="cliente-mantener__conyuge-row">
                      <Form.Item
                        name="conyugueLabel"
                        label="Cónyuge"
                        rules={[{ required: true, message: 'Busque y seleccione cónyuge' }]}
                        style={{ flex: 1, minWidth: 220 }}
                      >
                        <ConyugueAutoComplete
                          onPick={(id, label) => {
                            setConyuguePersonaId(id)
                            form.setFieldValue('conyugueLabel', label)
                          }}
                        />
                      </Form.Item>
                      {conyuguePersonaId ? (
                        <Link
                          to={`/informes/reporte-cliente?personaId=${conyuguePersonaId}`}
                          target="_blank"
                        >
                          <Button icon={<FileSearchOutlined />}>Ficha cónyuge</Button>
                        </Link>
                      ) : null}
                      <Button icon={<PlusOutlined />} onClick={() => setAvalOpen(true)}>
                        Nueva persona
                      </Button>
                    </div>
                  )}
                  <Row gutter={16}>
                    <Col xs={24} md={8}>
                      <Form.Item name="clasificacionRiesgoSbsId" label="Riesgo SBS">
                        <Select
                          allowClear
                          disabled={!puedeRiesgo}
                          options={(riesgoSbs.data ?? []).map((v) => ({
                            value: v.itemId,
                            label: v.denominacion,
                          }))}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={16}>
                      <Form.Item name="clasificacionRiesgoSbsObs" label="Obs. riesgo">
                        <Input disabled={!puedeRiesgo} />
                      </Form.Item>
                    </Col>
                  </Row>
                  <Form.Item name="nota" label="Observación">
                    <Input.TextArea rows={3} />
                  </Form.Item>
                  <Space wrap>
                    <Form.Item name="activo" valuePropName="checked" style={{ marginBottom: 0 }}>
                      <Checkbox>Activo</Checkbox>
                    </Form.Item>
                    {bloqueado ? (
                      <Text type="danger">
                        <LockOutlined /> Bloqueado
                      </Text>
                    ) : null}
                  </Space>
                </CredixPanel>
              ),
            },
          ]}
        />

        <div className="cliente-mantener__footer">
          <Button
            type="primary"
            htmlType="submit"
            icon={<SaveOutlined />}
            loading={guardar.isPending && (returnTo != null || guardarDestino === 'listado')}
            onClick={() => setGuardarDestino('listado')}
          >
            {returnTo ? 'Guardar y continuar' : 'Guardar cliente'}
          </Button>
          {!returnTo && (
            <>
              <Button
                htmlType="submit"
                icon={<FileAddOutlined />}
                loading={guardar.isPending && guardarDestino === 'credito'}
                onClick={() => setGuardarDestino('credito')}
              >
                Guardar y solicitar crédito
              </Button>
              {puedePrendario && (
                <Button
                  htmlType="submit"
                  loading={guardar.isPending && guardarDestino === 'prendario'}
                  onClick={() => setGuardarDestino('prendario')}
                >
                  Guardar y crédito prendario
                </Button>
              )}
            </>
          )}
          <Button onClick={() => navigate(returnTo ?? '/clientes')}>
            {returnTo ? 'Cancelar y volver' : 'Volver al listado'}
          </Button>
          {esEdicion && (
            <>
              <Button
                onClick={() =>
                  toggleClienteActivo(personaId).then((st) => {
                    form.setFieldValue('activo', st)
                    message.success(st ? 'Activado' : 'Desactivado')
                  })
                }
              >
                Activar / desactivar
              </Button>
              {puedeAprobador && (
                <Button
                  danger={!bloqueado}
                  icon={bloqueado ? <UnlockOutlined /> : <LockOutlined />}
                  onClick={() =>
                    toggleClienteBloqueado(personaId).then((n) => {
                      setBloqueado(n)
                      message.success(n ? 'Bloqueado' : 'Desbloqueado')
                    })
                  }
                >
                  {bloqueado ? 'Desbloquear' : 'Bloquear'}
                </Button>
              )}
            </>
          )}
        </div>
      </Form>

      <Modal
        title="Crear persona (cónyuge / aval)"
        open={avalOpen}
        onCancel={() => setAvalOpen(false)}
        footer={null}
        destroyOnHidden
      >
        <CrearPersonaRapidaModal
          onCreated={(id, label) => {
            setConyuguePersonaId(id)
            form.setFieldValue('conyugueLabel', label)
            setAvalOpen(false)
          }}
        />
      </Modal>
    </CredixPage>
  )
}
