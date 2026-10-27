import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  FileAddOutlined,
  FileSearchOutlined,
  UserAddOutlined,
} from '@ant-design/icons'
import {
  Alert,
  Button,
  Checkbox,
  Col,
  Input,
  InputNumber,
  Row,
  Space,
  Tag,
  Typography,
  message,
} from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import { consultarDniApiPeru } from '../../api/apiperu'
import { obtenerCliente, obtenerPersonaPorDocumento, toggleClienteActivo } from '../../api/clientes'
import { calcularGastosAdm } from '../../api/gastosAdm'
import { fetchProductos } from '../../api/productos'
import { simularCredito } from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { PrendaItem } from '../../api/creditoGestion'
import { CredixDatePicker, CredixPage, CredixPanel } from '../../components/credix'
import { ClienteBuscarAutoComplete } from '../../components/caja/ClienteBuscarAutoComplete'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import { CreditoPlanPreview } from '../../components/credito/CreditoPlanPreview'
import { cajaConfirm } from '../../components/caja/cajaConfirm'
import {
  prendaVacia,
  totalTasacion,
  ltvObservado,
  validarMontoVsTasacion,
  validarPrendasForm,
  type PrendaCampoError,
} from '../../utils/prendas'
import { motivoBloqueoPrendario } from '../../utils/clienteOriginacion'
import { originarCredito } from '../../utils/originarCredito'
import type { SimuladorCreditoCuota } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'
import {
  PRENDARIO_NUEVO_PATH,
  buildClientesNuevoHref,
  labelClienteFicha,
  withSearchParam,
} from '../../utils/internalReturnTo'
import '../../styles/credito-prendario-nuevo.css'

const { Text } = Typography

function defaultFechaPrimerPagoPrendario(): Dayjs {
  return dayjs().startOf('day').add(1, 'month')
}

type ConsultaDni =
  | { kind: 'existente'; personaId: number; label: string }
  | { kind: 'nuevo'; dni: string; nombres: string; apePaterno: string; apeMaterno: string }
  | { kind: 'sinReniec'; dni: string }

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

function onlyDigits(value: string): string {
  return value.replace(/\D/g, '')
}

export function CreditoPrendarioNuevoPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const personaIdUrl = Number(searchParams.get('personaId') ?? 0)
  const recienRegistrado = searchParams.get('origen') === 'alta'
  const altaToast = useRef(false)

  const [terminoCliente, setTerminoCliente] = useState('')
  const [dniConsulta, setDniConsulta] = useState('')
  const [consulta, setConsulta] = useState<ConsultaDni | null>(null)
  const [personaId, setPersonaId] = useState<number | null>(personaIdUrl > 0 ? personaIdUrl : null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])
  const [erroresPrendas, setErroresPrendas] = useState<PrendaCampoError[]>([])
  const [montoCredito, setMontoCredito] = useState<number | null>(500)
  const [interesMensual, setInteresMensual] = useState<number | null>(8)
  const [nroCuotas, setNroCuotas] = useState(1)
  const [gastosAdm, setGastosAdm] = useState<number | null>(5)
  const [fechaPrimerPago, setFechaPrimerPago] = useState<Dayjs | null>(defaultFechaPrimerPagoPrendario)
  const [observacion, setObservacion] = useState('')
  const [indCentralRiesgo, setIndCentralRiesgo] = useState(true)
  const formaPago = 'M'
  const [cuotas, setCuotas] = useState<SimuladorCreditoCuota[]>([])
  const [planLoading, setPlanLoading] = useState(false)

  const productosQuery = useQuery({
    queryKey: ['productos'],
    queryFn: fetchProductos,
    staleTime: 10 * 60_000,
  })
  const productoPrendario = useMemo(
    () => productosQuery.data?.find((p) => p.productoId === 2) ?? null,
    [productosQuery.data],
  )

  const clienteQuery = useQuery({
    queryKey: ['cliente-detalle', personaId],
    queryFn: () => obtenerCliente(personaId!),
    enabled: personaId != null && personaId > 0,
  })

  useEffect(() => {
    if (personaIdUrl < 1) return
    if (personaId === personaIdUrl) return
    setPersonaId(personaIdUrl)
  }, [personaIdUrl, personaId])

  useEffect(() => {
    if (!clienteQuery.data) return
    const label = labelClienteFicha(clienteQuery.data)
    setClienteLabel(label)
    setTerminoCliente(label)
    if (recienRegistrado && !altaToast.current) {
      altaToast.current = true
      message.success(`Cliente listo: ${label}`)
    }
  }, [clienteQuery.data, recienRegistrado])

  useEffect(() => {
    if (clienteQuery.error) {
      message.error(errMsg(clienteQuery.error))
    }
  }, [clienteQuery.error])

  useEffect(() => {
    if (montoCredito == null || !(montoCredito > 0)) {
      setGastosAdm(0)
      return
    }
    let cancelled = false
    void calcularGastosAdm(montoCredito, indCentralRiesgo)
      .then((r) => {
        if (!cancelled) setGastosAdm(r.gastosAdm)
      })
      .catch(() => {
        if (!cancelled) setGastosAdm(0)
      })
    return () => {
      cancelled = true
    }
  }, [montoCredito, indCentralRiesgo])

  const montoDesembolsar =
    montoCredito != null && montoCredito > 0 ? montoCredito : null

  const bloqueoCliente = motivoBloqueoPrendario(clienteQuery.data)
  const clienteInactivo =
    clienteQuery.data != null &&
    (!clienteQuery.data.clienteEstado || !clienteQuery.data.personaEstado)
  const clienteBloqueado = clienteQuery.data?.bloqueado === true

  const elegirCliente = (id: number, label: string) => {
    setPersonaId(id)
    setClienteLabel(label)
    setTerminoCliente(label)
    setConsulta(null)
    const next = new URLSearchParams(searchParams)
    next.set('personaId', String(id))
    next.delete('origen')
    setSearchParams(next, { replace: true })
  }

  const limpiarCliente = () => {
    setPersonaId(null)
    setClienteLabel('')
    setTerminoCliente('')
    altaToast.current = false
    if (!searchParams.has('personaId') && !searchParams.has('origen')) return
    const next = new URLSearchParams(searchParams)
    next.delete('personaId')
    next.delete('origen')
    setSearchParams(next, { replace: true })
  }

  const consultarDni = useMutation({
    mutationFn: async (dniRaw: string): Promise<ConsultaDni> => {
      const dni = onlyDigits(dniRaw)
      if (dni.length !== 8) {
        throw new Error('Ingrese un DNI de 8 dígitos')
      }
      const existente = await obtenerPersonaPorDocumento(dni)
      if (existente?.tieneCliente) {
        return {
          kind: 'existente',
          personaId: existente.personaId,
          label: labelClienteFicha(existente),
        }
      }
      const reniec = await consultarDniApiPeru(dni)
      if (reniec.success) {
        return {
          kind: 'nuevo',
          dni,
          nombres: reniec.nombres ?? '',
          apePaterno: reniec.apellidoPaterno ?? '',
          apeMaterno: reniec.apellidoMaterno ?? '',
        }
      }
      return { kind: 'sinReniec', dni }
    },
    onSuccess: (r) => {
      setConsulta(r)
      if (r.kind === 'existente') {
        message.info('Este DNI ya es cliente. Pulse Elegir para continuar.')
      } else if (r.kind === 'nuevo') {
        message.success('Datos validados con ApiPerú')
      } else {
        message.warning('DNI no encontrado en RENIEC. Complete la ficha en Clientes.')
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const irAFichaCliente = (dni?: string) => {
    navigate(
      buildClientesNuevoHref({
        returnTo: PRENDARIO_NUEVO_PATH,
        dni,
      }),
    )
  }

  const hrefFichaCliente = personaId
    ? withSearchParam(`/clientes/editar/${personaId}`, 'returnTo', PRENDARIO_NUEVO_PATH)
    : buildClientesNuevoHref({ returnTo: PRENDARIO_NUEVO_PATH })

  const activarCliente = useMutation({
    mutationFn: () => toggleClienteActivo(personaId!),
    onSuccess: (activo) => {
      void queryClient.invalidateQueries({ queryKey: ['cliente-detalle', personaId] })
      message.success(activo ? 'Cliente activado. Ya puede solicitar.' : 'Cliente desactivado')
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const crear = useMutation({
    mutationFn: async () => {
      if (!personaId) {
        throw new Error('Seleccione o registre un cliente')
      }
      if (oficinaId < 1) {
        throw new Error('Sesión sin oficina. Vuelva a iniciar sesión.')
      }
      if (bloqueoCliente) {
        throw new Error(
          bloqueoCliente === 'Cliente inactivo'
            ? 'El cliente está inactivo. Actívelo antes de crear la solicitud prendaria.'
            : bloqueoCliente,
        )
      }
      const validacion = validarPrendasForm(prendas)
      setErroresPrendas(validacion.errores)
      if (!validacion.ok) {
        throw new Error(validacion.mensaje ?? 'Complete los bienes en custodia')
      }
      if (montoCredito == null || montoCredito <= 0) {
        throw new Error('Indique el monto del crédito')
      }
      if (interesMensual == null || interesMensual < 0) {
        throw new Error('Indique el interés mensual')
      }
      if (productoPrendario) {
        const min = productoPrendario.interesMinima
        const max = productoPrendario.interesMaxima
        if (interesMensual < min || interesMensual > max) {
          throw new Error(
            `El interés debe estar entre ${min.toFixed(2)}% y ${max.toFixed(2)}%`,
          )
        }
      }
      const montoVsTasacion = validarMontoVsTasacion(montoCredito, totalTasacion(validacion.bienes))
      if (montoVsTasacion) {
        throw new Error(montoVsTasacion)
      }
      if (!fechaPrimerPago) {
        throw new Error('Indique la fecha del primer pago')
      }
      if (gastosAdm == null || gastosAdm < 0) {
        throw new Error('Indique el trámite administrativo')
      }
      return originarCredito({
        oficinaId,
        personaId,
        productoId: 2,
        esPrendario: true,
        monto: montoCredito,
        modalidad: formaPago,
        nroCuotas,
        interesMensual,
        fechaPrimerPago: fechaPrimerPago.format('YYYY-MM-DD'),
        gastosAdm,
        observacion: observacion.trim() || null,
        indCentralRiesgo,
        prendas: validacion.bienes,
      })
    },
    onSuccess: ({ solicitudCreditoId, mensaje }) => {
      setErroresPrendas([])
      if (mensaje?.trim()) message.info(mensaje)
      else message.success(`Crédito prendario #${solicitudCreditoId} listo para aprobación`)
      if (!personaId) return
      navigate(`/credito/prendario/gestionar/${personaId}?creditoId=${solicitudCreditoId}`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const validacionBienes = useMemo(() => validarPrendasForm(prendas), [prendas])
  const tasacion = useMemo(() => totalTasacion(prendas), [prendas])
  const cantidadBienes = validacionBienes.bienes.length

  const errorMontoVsTasacion = useMemo(() => {
    if (montoCredito == null || !(montoCredito > 0)) {
      if (personaId && !(tasacion > 0)) {
        return 'Registre bienes con tasación antes de indicar el monto del crédito'
      }
      return null
    }
    return validarMontoVsTasacion(montoCredito, tasacion)
  }, [montoCredito, tasacion, personaId])

  const errorInteres = useMemo(() => {
    if (interesMensual == null || !productoPrendario) return null
    const min = productoPrendario.interesMinima
    const max = productoPrendario.interesMaxima
    if (interesMensual < min || interesMensual > max) {
      return `El interés debe estar entre ${min.toFixed(2)}% y ${max.toFixed(2)}%`
    }
    return null
  }, [interesMensual, productoPrendario])

  const errorFecha = useMemo(() => {
    if (!fechaPrimerPago) return 'Indique la fecha del primer pago'
    if (fechaPrimerPago.startOf('day').isBefore(dayjs().startOf('day'))) {
      return 'La fecha del primer pago no puede ser anterior a hoy'
    }
    return null
  }, [fechaPrimerPago])

  const bienesListos = validacionBienes.ok

  useEffect(() => {
    const hayIntento = prendas.some(
      (p) => p.descripcion.trim().length > 0 || (p.valorTasacion ?? 0) > 0,
    )
    if (!hayIntento) {
      setErroresPrendas([])
      return
    }
    setErroresPrendas(validarPrendasForm(prendas).errores)
  }, [prendas])

  const puedeContinuar =
    personaId != null &&
    oficinaId > 0 &&
    !bloqueoCliente &&
    bienesListos &&
    montoCredito != null &&
    montoCredito > 0 &&
    !errorMontoVsTasacion &&
    interesMensual != null &&
    interesMensual >= 0 &&
    !errorInteres &&
    !errorFecha &&
    gastosAdm != null &&
    gastosAdm >= 0

  useEffect(() => {
    if (
      !puedeContinuar ||
      montoCredito == null ||
      interesMensual == null ||
      !fechaPrimerPago
    ) {
      setCuotas([])
      return
    }
    let cancelled = false
    const t = window.setTimeout(() => {
      setPlanLoading(true)
      void simularCredito({
        monto: montoCredito,
        formaPago,
        nroCuotas,
        interesMensual,
        fechaPrimerPago: fechaPrimerPago.format('YYYY-MM-DD'),
        gastosAdm,
      })
        .then((rows) => {
          if (!cancelled) setCuotas(rows)
        })
        .catch(() => {
          if (!cancelled) setCuotas([])
        })
        .finally(() => {
          if (!cancelled) setPlanLoading(false)
        })
    }, 380)
    return () => {
      cancelled = true
      window.clearTimeout(t)
    }
  }, [
    puedeContinuar,
    montoCredito,
    formaPago,
    nroCuotas,
    interesMensual,
    fechaPrimerPago,
    gastosAdm,
  ])

  const ltv =
    montoCredito != null && montoCredito > 0 && tasacion > 0 && !errorMontoVsTasacion
      ? ltvObservado(montoCredito, tasacion)
      : null

  const clienteNombre = clienteQuery.data
    ? labelClienteFicha(clienteQuery.data).replace(/^\S+\s/, '')
    : clienteLabel.replace(/^\S+\s/, '')
  const clienteDoc =
    clienteQuery.data?.numeroDocumento ?? (clienteLabel.match(/^\d+/)?.[0] || '')

  const avisoContinuar = !puedeContinuar && personaId
    ? bloqueoCliente
      ? bloqueoCliente
      : !bienesListos
        ? 'Falta un bien con tasación.'
        : errorMontoVsTasacion
          ? 'El monto supera la tasación.'
          : errorInteres
            ? errorInteres
            : errorFecha
              ? errorFecha
              : 'Revise monto e interés.'
    : null

  const bloqueado = Boolean(bloqueoCliente)

  return (
    <CredixPage
      className="prendario-nuevo-page"
      title="Nuevo crédito prendario"
      subtitle="Cliente, prenda y desembolso. Sin pasar por el simulador."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: <Link to="/credito/prendario">Prendario</Link> },
        { title: 'Nuevo' },
      ]}
    >
      <div className="prendario-nuevo-layout">
        {personaId ? (
          <div className="prendario-nuevo-cliente-bar">
            <div className="prendario-nuevo-cliente-bar__id">
              {clienteDoc ? <span className="prendario-nuevo-cliente-bar__dni">{clienteDoc}</span> : null}
              <strong>{clienteNombre || clienteLabel || `Persona #${personaId}`}</strong>
              <Space size={6} wrap>
                {clienteQuery.isFetching ? <Tag>Cargando…</Tag> : null}
                {clienteInactivo ? <Tag color="error">Inactivo</Tag> : <Tag color="success">Activo</Tag>}
                {clienteBloqueado ? <Tag color="error">Bloqueado</Tag> : null}
                {clienteQuery.data?.celular1 ? (
                  <Tag>{clienteQuery.data.celular1}</Tag>
                ) : (
                  <Tag color="orange">Sin celular</Tag>
                )}
              </Space>
            </div>
            <Space wrap>
              <Link to={hrefFichaCliente}>
                <Button size="small">Ficha</Button>
              </Link>
              <Button size="small" onClick={limpiarCliente}>
                Cambiar
              </Button>
            </Space>
            {bloqueoCliente ? (
              <Alert
                type="error"
                showIcon
                className="prendario-nuevo-cliente-bar__alert"
                message={bloqueoCliente}
                description={
                  clienteInactivo
                    ? 'Actívelo aquí para poder solicitar.'
                    : clienteBloqueado
                      ? 'Desbloquéelo en la ficha.'
                      : 'Falta celular (9 dígitos que empiezan con 9).'
                }
                action={
                  clienteInactivo ? (
                    <Button
                      size="small"
                      type="primary"
                      loading={activarCliente.isPending}
                      onClick={() => activarCliente.mutate()}
                    >
                      Activar
                    </Button>
                  ) : (
                    <Link to={hrefFichaCliente}>
                      <Button size="small" type="primary">
                        Completar ficha
                      </Button>
                    </Link>
                  )
                }
              />
            ) : null}
          </div>
        ) : (
          <CredixPanel title="Cliente">
            <div className="prendario-nuevo-buscar">
              <ClienteBuscarAutoComplete
                variant="credito"
                value={terminoCliente}
                onChange={setTerminoCliente}
                onSelectPersona={elegirCliente}
                debounceMs={280}
                minChars={2}
                placeholder="DNI, apellido o nombre"
              />
              <div className="prendario-nuevo-alta">
                <Space.Compact className="prendario-nuevo-dni">
                  <Input
                    placeholder="DNI de un cliente nuevo"
                    value={dniConsulta}
                    maxLength={8}
                    inputMode="numeric"
                    onChange={(e) => setDniConsulta(onlyDigits(e.target.value))}
                    onPressEnter={() => {
                      if (dniConsulta.length === 8) consultarDni.mutate(dniConsulta)
                    }}
                  />
                  <Button
                    icon={<FileSearchOutlined />}
                    loading={consultarDni.isPending}
                    onClick={() => consultarDni.mutate(dniConsulta)}
                  >
                    RENIEC
                  </Button>
                </Space.Compact>
                <Button
                  icon={<UserAddOutlined />}
                  onClick={() =>
                    irAFichaCliente(
                      consulta && consulta.kind !== 'existente' ? consulta.dni : dniConsulta || undefined,
                    )
                  }
                >
                  Registrar
                </Button>
              </div>
              {consulta?.kind === 'existente' ? (
                <Alert
                  type="info"
                  showIcon
                  className="prendario-nuevo-consulta"
                  message={consulta.label}
                  action={
                    <Button
                      size="small"
                      type="primary"
                      onClick={() => elegirCliente(consulta.personaId, consulta.label)}
                    >
                      Usar
                    </Button>
                  }
                />
              ) : null}
              {consulta?.kind === 'nuevo' ? (
                <Alert
                  type="success"
                  showIcon
                  className="prendario-nuevo-consulta"
                  message={`${consulta.nombres} ${consulta.apePaterno} ${consulta.apeMaterno}`.trim()}
                  description="Complete celular y domicilio en Clientes. Quedará activo."
                />
              ) : null}
              {consulta?.kind === 'sinReniec' ? (
                <Alert
                  type="warning"
                  showIcon
                  className="prendario-nuevo-consulta"
                  message="RENIEC no devolvió datos. Puede registrar la ficha a mano."
                />
              ) : null}
            </div>
          </CredixPanel>
        )}

        {personaId ? (
          <>
            <CredixPanel
              title="Prenda"
              extra={
                tasacion > 0 ? (
                  <Text type="secondary">
                    {cantidadBienes} bien{cantidadBienes === 1 ? '' : 'es'} · {formatMoney(tasacion)}
                  </Text>
                ) : null
              }
            >
              <PrendasEditor
                value={prendas}
                onChange={(next) => {
                  setPrendas(next)
                  if (erroresPrendas.length > 0) setErroresPrendas([])
                }}
                disabled={bloqueado}
                errores={erroresPrendas}
              />
            </CredixPanel>

            <CredixPanel
              title="Crédito"
              extra={
                ltv != null ? <Text type="secondary">LTV {ltv}%</Text> : null
              }
            >
              <Row gutter={[16, 12]} className="prendario-nuevo-campos">
                <Col xs={24} sm={12} md={8}>
                  <Text type="secondary">Desembolso (S/)</Text>
                  <InputNumber
                    className="prendario-nuevo-campo"
                    min={1}
                    step={10}
                    disabled={bloqueado}
                    status={errorMontoVsTasacion ? 'error' : undefined}
                    value={montoCredito}
                    onChange={(v) => setMontoCredito(v)}
                    prefix="S/"
                  />
                  {tasacion > 0 ? (
                    <Text
                      type={errorMontoVsTasacion ? 'danger' : 'secondary'}
                      className="prendario-nuevo-ayuda"
                    >
                      Tope {formatMoney(tasacion)}
                      {montoCredito != null && montoCredito > 0 && !errorMontoVsTasacion
                        ? ` · disponible ${formatMoney(Math.max(0, tasacion - montoCredito))}`
                        : null}
                    </Text>
                  ) : (
                    <Text type="danger" className="prendario-nuevo-ayuda">
                      Primero tase un bien.
                    </Text>
                  )}
                </Col>
                <Col xs={24} sm={12} md={8}>
                  <Text type="secondary">
                    Interés mensual (%)
                    {productoPrendario
                      ? ` · ${productoPrendario.interesMinima.toFixed(2)} a ${productoPrendario.interesMaxima.toFixed(2)}`
                      : ''}
                  </Text>
                  <InputNumber
                    className="prendario-nuevo-campo"
                    min={productoPrendario?.interesMinima ?? 0}
                    max={productoPrendario?.interesMaxima ?? 100}
                    step={0.1}
                    disabled={bloqueado}
                    status={errorInteres ? 'error' : undefined}
                    value={interesMensual}
                    onChange={(v) => setInteresMensual(v)}
                  />
                  {errorInteres ? (
                    <Text type="danger" className="prendario-nuevo-ayuda">
                      {errorInteres}
                    </Text>
                  ) : null}
                </Col>
                <Col xs={24} sm={12} md={8}>
                  <Text type="secondary">Cuotas (mensual)</Text>
                  <InputNumber
                    className="prendario-nuevo-campo"
                    min={1}
                    disabled={bloqueado}
                    value={nroCuotas}
                    onChange={(v) => setNroCuotas(typeof v === 'number' && v >= 1 ? v : 1)}
                  />
                </Col>
                <Col xs={24} sm={12} md={8}>
                  <Text type="secondary">Primer pago</Text>
                  <CredixDatePicker
                    className="prendario-nuevo-campo"
                    disabled={bloqueado}
                    status={errorFecha ? 'error' : undefined}
                    value={fechaPrimerPago}
                    disabledDate={(d) => d.startOf('day').isBefore(dayjs().startOf('day'))}
                    onChange={(d) => setFechaPrimerPago(d)}
                  />
                  {errorFecha ? (
                    <Text type="danger" className="prendario-nuevo-ayuda">
                      {errorFecha}
                    </Text>
                  ) : null}
                </Col>
                <Col xs={24} sm={12} md={8}>
                  <Text type="secondary">Trámite (S/)</Text>
                  <InputNumber
                    className="prendario-nuevo-campo"
                    min={0}
                    step={1}
                    disabled={bloqueado}
                    value={gastosAdm}
                    onChange={(v) => setGastosAdm(v)}
                    prefix="S/"
                  />
                </Col>
                <Col xs={24} sm={12} md={8}>
                  <Text type="secondary">Nota del crédito</Text>
                  <Input
                    className="prendario-nuevo-campo"
                    disabled={bloqueado}
                    value={observacion}
                    onChange={(e) => setObservacion(e.target.value)}
                    maxLength={200}
                    placeholder="Opcional"
                  />
                </Col>
                <Col xs={24}>
                  <Checkbox
                    disabled={bloqueado}
                    checked={indCentralRiesgo}
                    onChange={(e) => setIndCentralRiesgo(e.target.checked)}
                  >
                    Incluir central de riesgo en el trámite
                  </Checkbox>
                </Col>
              </Row>
              {errorMontoVsTasacion ? (
                <Alert
                  type="error"
                  showIcon
                  className="prendario-nuevo-consulta"
                  message={errorMontoVsTasacion}
                  action={
                    tasacion > 0 ? (
                      <Button
                        size="small"
                        type="primary"
                        ghost
                        onClick={() => setMontoCredito(Number(tasacion.toFixed(2)))}
                      >
                        Ajustar a {formatMoney(tasacion)}
                      </Button>
                    ) : null
                  }
                />
              ) : null}
              <CreditoPlanPreview cuotas={cuotas} loading={planLoading} />
            </CredixPanel>
          </>
        ) : null}

        <aside className="prendario-nuevo-accion" aria-label="Solicitar">
          <div className="prendario-nuevo-accion__copy">
            {montoDesembolsar != null && !errorMontoVsTasacion && !bloqueoCliente ? (
              <Text strong className="prendario-nuevo-accion__monto">
                Desembolso {formatMoney(montoDesembolsar)}
                {ltv != null ? ` · LTV ${ltv}%` : ''}
              </Text>
            ) : (
              <Text type="secondary">
                {personaId ? 'Complete prenda y desembolso.' : 'Elija o registre al cliente.'}
              </Text>
            )}
            {avisoContinuar ? (
              <Text type="warning" className="prendario-nuevo-accion__aviso">
                {avisoContinuar}
              </Text>
            ) : null}
          </div>
          <Button
            type="primary"
            icon={<FileAddOutlined />}
            disabled={!puedeContinuar}
            loading={crear.isPending}
            onClick={() =>
              cajaConfirm({
                title: 'Solicitar crédito prendario',
                content: `Se generará el crédito de ${formatMoney(montoCredito ?? 0)} para aprobación. ¿Continuar?`,
                onOk: () => crear.mutateAsync(),
              })
            }
          >
            Solicitar crédito
          </Button>
        </aside>
      </div>
    </CredixPage>
  )
}
