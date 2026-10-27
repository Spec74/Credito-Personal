import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { FileAddOutlined, UserAddOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Checkbox,
  Col,
  Input,
  InputNumber,
  Row,
  Space,
  Steps,
  Tag,
  Typography,
  message,
} from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import { obtenerCliente } from '../../api/clientes'
import { calcularGastosAdm } from '../../api/gastosAdm'
import { simularCredito } from '../../api/creditoPlanes'
import { fetchProductos } from '../../api/productos'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import {
  CredixDatePicker,
  CredixPage,
  CredixPanel,
  CredixSelect,
  type CredixStatItem,
} from '../../components/credix'
import { ClienteBuscarAutoComplete } from '../../components/caja/ClienteBuscarAutoComplete'
import { CreditoPlanPreview } from '../../components/credito/CreditoPlanPreview'
import { cajaConfirm } from '../../components/caja/cajaConfirm'
import { CLIENTE_CRUZADO_AVISO } from '../../utils/clienteCruzado'
import { motivoBloqueoOriginacion, montoSobreTope } from '../../utils/clienteOriginacion'
import {
  fechaPrimerPagoPorModalidad,
  originarCredito,
} from '../../utils/originarCredito'
import { formatMoney } from '../../utils/formatMoney'
import {
  buildClientesNuevoHref,
  labelClienteFicha,
  withSearchParam,
} from '../../utils/internalReturnTo'
import {
  esCreditoAdministrador,
  esCreditoAprobador1,
} from '../../utils/creditoOperacionPermisos'
import type { SimuladorCreditoCuota } from '../../types/api'
import '../../styles/credito-prendario-nuevo.css'

const { Text } = Typography

const FORMAS_PAGO = [
  { value: 'M', label: 'Mensual' },
  { value: 'Q', label: 'Quincenal' },
  { value: 'S', label: 'Semanal' },
  { value: 'D', label: 'Diario' },
]

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

function toDayjs(iso: string): Dayjs {
  return dayjs(iso.slice(0, 10))
}

export function CreditoNuevoPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const roles = session?.roles ?? []
  const puedeIrAAprobar = esCreditoAprobador1(roles) || esCreditoAdministrador(roles)
  const personaIdUrl = Number(searchParams.get('personaId') ?? 0)
  const solicitudUrl = Number(searchParams.get('solicitudCreditoId') ?? 0)
  const productoUrl = Number(searchParams.get('productoId') ?? 0)

  useEffect(() => {
    if (productoUrl !== 2) return
    const q = personaIdUrl > 0 ? `?personaId=${personaIdUrl}` : ''
    navigate(`/credito/prendario/nuevo${q}`, { replace: true })
  }, [productoUrl, personaIdUrl, navigate])

  const [terminoCliente, setTerminoCliente] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(personaIdUrl > 0 ? personaIdUrl : null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [productoId, setProductoId] = useState<number | null>(null)
  const [monto, setMonto] = useState<number | null>(1000)
  const [formaPago, setFormaPago] = useState('M')
  const [nroCuotas, setNroCuotas] = useState(12)
  const [interesMensual, setInteresMensual] = useState<number | null>(5)
  const [fechaPrimerPago, setFechaPrimerPago] = useState<Dayjs | null>(
    toDayjs(fechaPrimerPagoPorModalidad('M')),
  )
  const [gastosAdm, setGastosAdm] = useState<number | null>(0)
  const [observacion, setObservacion] = useState('')
  const [indCentralRiesgo, setIndCentralRiesgo] = useState(true)
  const [cuotas, setCuotas] = useState<SimuladorCreditoCuota[]>([])
  const [planLoading, setPlanLoading] = useState(false)

  const productosQuery = useQuery({
    queryKey: ['productos'],
    queryFn: fetchProductos,
    staleTime: 10 * 60_000,
  })
  const productos = useMemo(
    () => (productosQuery.data ?? []).filter((p) => p.productoId !== 2 && p.estado !== false),
    [productosQuery.data],
  )
  const producto = productos.find((p) => p.productoId === productoId) ?? null

  const clienteQuery = useQuery({
    queryKey: ['cliente-detalle', personaId],
    queryFn: () => obtenerCliente(personaId!),
    enabled: personaId != null && personaId > 0,
  })

  useEffect(() => {
    if (personaIdUrl > 0 && personaId !== personaIdUrl) setPersonaId(personaIdUrl)
  }, [personaIdUrl, personaId])

  useEffect(() => {
    if (!clienteQuery.data) return
    setClienteLabel(labelClienteFicha(clienteQuery.data))
    setTerminoCliente(labelClienteFicha(clienteQuery.data))
  }, [clienteQuery.data])

  useEffect(() => {
    if (productoId != null || productos.length === 0) return
    setProductoId(productos[0].productoId)
    setInteresMensual(productos[0].interesMinima)
  }, [productoId, productos])

  useEffect(() => {
    if (!producto || interesMensual == null) return
    if (interesMensual < producto.interesMinima) setInteresMensual(producto.interesMinima)
    if (interesMensual > producto.interesMaxima) setInteresMensual(producto.interesMaxima)
  }, [producto, interesMensual])

  useEffect(() => {
    setFechaPrimerPago(toDayjs(fechaPrimerPagoPorModalidad(formaPago)))
  }, [formaPago])

  useEffect(() => {
    if (monto == null || !(monto > 0)) {
      setGastosAdm(0)
      return
    }
    let cancelled = false
    void calcularGastosAdm(monto, indCentralRiesgo)
      .then((r) => {
        if (!cancelled) setGastosAdm(r.gastosAdm)
      })
      .catch(() => {
        if (!cancelled) setGastosAdm(0)
      })
    return () => {
      cancelled = true
    }
  }, [monto, indCentralRiesgo])

  const bloqueoCliente = motivoBloqueoOriginacion(clienteQuery.data)
  const sobreTope = montoSobreTope(monto, clienteQuery.data?.topeCredito)

  const errorInteres = useMemo(() => {
    if (interesMensual == null || !producto) return null
    if (interesMensual < producto.interesMinima || interesMensual > producto.interesMaxima) {
      return `El interés debe estar entre ${producto.interesMinima.toFixed(2)}% y ${producto.interesMaxima.toFixed(2)}%`
    }
    return null
  }, [interesMensual, producto])

  const camposListos =
    personaId != null &&
    !bloqueoCliente &&
    productoId != null &&
    monto != null &&
    monto > 0 &&
    nroCuotas >= 1 &&
    interesMensual != null &&
    !errorInteres &&
    fechaPrimerPago != null &&
    gastosAdm != null &&
    gastosAdm >= 0

  useEffect(() => {
    if (!camposListos || monto == null || interesMensual == null || !fechaPrimerPago) {
      setCuotas([])
      return
    }
    let cancelled = false
    const t = window.setTimeout(() => {
      setPlanLoading(true)
      void simularCredito({
        monto,
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
  }, [camposListos, monto, formaPago, nroCuotas, interesMensual, fechaPrimerPago, gastosAdm])

  const elegirCliente = (id: number, label: string) => {
    setPersonaId(id)
    setClienteLabel(label)
    setTerminoCliente(label)
    const next = new URLSearchParams(searchParams)
    next.set('personaId', String(id))
    setSearchParams(next, { replace: true })
  }

  const limpiarCliente = () => {
    setPersonaId(null)
    setClienteLabel('')
    setTerminoCliente('')
    const next = new URLSearchParams(searchParams)
    next.delete('personaId')
    setSearchParams(next, { replace: true })
  }

  const hrefFicha = personaId
    ? withSearchParam(`/clientes/editar/${personaId}`, 'returnTo', '/credito/nuevo')
    : buildClientesNuevoHref({ returnTo: '/credito/nuevo' })

  const solicitar = useMutation({
    mutationFn: async () => {
      if (!personaId || !productoId || monto == null || interesMensual == null || !fechaPrimerPago) {
        throw new Error('Complete cliente, producto y condiciones')
      }
      if (bloqueoCliente) throw new Error(bloqueoCliente)
      if (errorInteres) throw new Error(errorInteres)
      return originarCredito({
        oficinaId,
        personaId,
        productoId,
        esPrendario: false,
        solicitudCreditoId: solicitudUrl > 0 ? solicitudUrl : null,
        monto,
        modalidad: formaPago,
        nroCuotas,
        interesMensual,
        fechaPrimerPago: fechaPrimerPago.format('YYYY-MM-DD'),
        gastosAdm: gastosAdm ?? 0,
        observacion: observacion.trim() || null,
        indCentralRiesgo,
      })
    },
    onSuccess: ({ solicitudCreditoId, mensaje }) => {
      if (mensaje?.trim()) message.info(mensaje)
      else message.success(`Crédito #${solicitudCreditoId} listo para aprobación`)
      if (puedeIrAAprobar) {
        navigate('/credito/aprobar')
        return
      }
      if (personaId) {
        navigate(`/credito/consulta/persona/${personaId}`)
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const stats: CredixStatItem[] = []
  if (personaId) {
    stats.push({ label: 'Cliente', value: clienteLabel || `Persona #${personaId}` })
  }
  if (monto != null && monto > 0) {
    stats.push({ label: 'Monto', value: formatMoney(monto), tone: 'green' })
  }
  if (cuotas[0]?.cuota != null) {
    stats.push({ label: 'Cuota', value: formatMoney(cuotas[0].cuota) })
  }

  return (
    <CredixPage
      className="prendario-nuevo-page"
      title="Nueva solicitud de crédito"
      subtitle="Cliente y condiciones en una sola pantalla. El simulador queda para cotizar sin originar."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Nueva solicitud' },
      ]}
      stats={stats}
    >
      <Steps
        size="small"
        current={personaId ? (camposListos ? 2 : 1) : 0}
        className="prendario-nuevo-steps"
        items={[
          { title: 'Cliente' },
          { title: 'Condiciones' },
          { title: 'Solicitar' },
        ]}
      />

      <div className="prendario-nuevo-layout">
        {personaId ? (
          <CredixPanel title="Cliente">
            <div className="prendario-nuevo-cliente-card__row">
              <div className="prendario-nuevo-cliente-card__id">
                <Text strong>{clienteLabel || `Persona #${personaId}`}</Text>
                <Space size={6} wrap>
                  {bloqueoCliente ? <Tag color="error">{bloqueoCliente}</Tag> : <Tag color="success">Listo</Tag>}
                  {clienteQuery.data?.topeCredito ? (
                    <Tag>Tope {formatMoney(clienteQuery.data.topeCredito)}</Tag>
                  ) : null}
                </Space>
              </div>
              <Space wrap>
                <Link to={hrefFicha}>
                  <Button size="small">Ver ficha</Button>
                </Link>
                <Button size="small" onClick={limpiarCliente}>
                  Cambiar
                </Button>
              </Space>
            </div>
            {bloqueoCliente ? (
              <Alert
                type="error"
                showIcon
                className="prendario-nuevo-consulta"
                message={bloqueoCliente}
                action={
                  <Link to={hrefFicha}>
                    <Button size="small" type="primary">
                      Ir a ficha
                    </Button>
                  </Link>
                }
              />
            ) : (
              <Text type="secondary" className="prendario-nuevo-cliente-card__hint">
                {CLIENTE_CRUZADO_AVISO}
              </Text>
            )}
          </CredixPanel>
        ) : (
          <CredixPanel title="Cliente">
            <ClienteBuscarAutoComplete
              variant="credito"
              value={terminoCliente}
              onChange={setTerminoCliente}
              onSelectPersona={elegirCliente}
              debounceMs={280}
              minChars={2}
              placeholder="DNI, apellido o nombre"
            />
            <Space wrap className="prendario-nuevo-campos">
              <Button icon={<UserAddOutlined />} onClick={() => navigate(hrefFicha)}>
                Registrar cliente
              </Button>
              <Link to="/credito/prendario/nuevo">Ir a prendario</Link>
            </Space>
            <Text type="secondary" className="prendario-nuevo-hint">
              {CLIENTE_CRUZADO_AVISO}
            </Text>
          </CredixPanel>
        )}

        <CredixPanel title="Condiciones">
          <Row gutter={[16, 12]}>
            <Col xs={24} sm={12} md={8}>
              <Text type="secondary">Producto</Text>
              <CredixSelect
                className="prendario-nuevo-campo"
                disabled={!personaId || Boolean(bloqueoCliente)}
                value={productoId ?? undefined}
                options={productos.map((p) => ({
                  value: p.productoId,
                  label: p.denominacion,
                }))}
                onChange={(v) => {
                  const id = Number(v)
                  setProductoId(id)
                  const p = productos.find((x) => x.productoId === id)
                  if (p) setInteresMensual(p.interesMinima)
                }}
              />
            </Col>
            <Col xs={24} sm={12} md={8}>
              <Text type="secondary">Modalidad</Text>
              <CredixSelect
                className="prendario-nuevo-campo"
                disabled={!personaId || Boolean(bloqueoCliente)}
                value={formaPago}
                options={FORMAS_PAGO}
                onChange={(v) => setFormaPago(String(v))}
              />
            </Col>
            <Col xs={24} sm={12} md={8}>
              <Text type="secondary">&nbsp;</Text>
              <div className="prendario-nuevo-check">
                <Checkbox
                  disabled={!personaId || Boolean(bloqueoCliente)}
                  checked={indCentralRiesgo}
                  onChange={(e) => setIndCentralRiesgo(e.target.checked)}
                >
                  Incluir central de riesgo al trámite
                </Checkbox>
              </div>
            </Col>
            <Col xs={24} sm={12} md={8}>
              <Text type="secondary">Monto (S/)</Text>
              <InputNumber
                className="prendario-nuevo-campo"
                min={1}
                step={50}
                disabled={!personaId || Boolean(bloqueoCliente)}
                status={sobreTope ? 'warning' : undefined}
                value={monto}
                onChange={(v) => setMonto(v)}
                prefix="S/"
              />
              {sobreTope ? (
                <Text type="warning" className="prendario-nuevo-ayuda">
                  El monto supera el tope del cliente. El USP decide si lo admite.
                </Text>
              ) : null}
            </Col>
            <Col xs={24} sm={12} md={8}>
              <Text type="secondary">Número de cuotas</Text>
              <InputNumber
                className="prendario-nuevo-campo"
                min={1}
                disabled={!personaId || Boolean(bloqueoCliente)}
                value={nroCuotas}
                onChange={(v) => setNroCuotas(typeof v === 'number' && v >= 1 ? v : 1)}
              />
            </Col>
            <Col xs={24} sm={12} md={8}>
              <Text type="secondary">Trámite adm. (S/)</Text>
              <InputNumber
                className="prendario-nuevo-campo"
                min={0}
                step={1}
                disabled={!personaId || Boolean(bloqueoCliente)}
                value={gastosAdm}
                onChange={(v) => setGastosAdm(v)}
                prefix="S/"
              />
            </Col>
            <Col xs={24} sm={12} md={8}>
              <Text type="secondary">
                Interés mensual (%)
                {producto ? (
                  <Text type="secondary" style={{ fontWeight: 400 }}>
                    {' '}
                    (min {producto.interesMinima.toFixed(2)} · max {producto.interesMaxima.toFixed(2)})
                  </Text>
                ) : null}
              </Text>
              <InputNumber
                className="prendario-nuevo-campo"
                min={producto?.interesMinima ?? 0}
                max={producto?.interesMaxima ?? 100}
                step={0.1}
                disabled={!personaId || Boolean(bloqueoCliente)}
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
              <Text type="secondary">Primer pago</Text>
              <CredixDatePicker
                className="prendario-nuevo-campo"
                disabled={!personaId || Boolean(bloqueoCliente)}
                value={fechaPrimerPago}
                disabledDate={(d) => d.startOf('day').isBefore(dayjs().startOf('day'))}
                onChange={(d) => setFechaPrimerPago(d)}
              />
            </Col>
            <Col xs={24} md={16}>
              <Text type="secondary">Observación</Text>
              <Input
                className="prendario-nuevo-campo"
                disabled={!personaId || Boolean(bloqueoCliente)}
                value={observacion}
                onChange={(e) => setObservacion(e.target.value)}
                maxLength={200}
                placeholder="Opcional"
              />
            </Col>
          </Row>
          <CreditoPlanPreview cuotas={cuotas} loading={planLoading} />
        </CredixPanel>

        <aside className="prendario-nuevo-accion" aria-label="Solicitar">
          <div className="prendario-nuevo-accion__copy">
            {camposListos ? (
              <Text strong className="prendario-nuevo-accion__monto">
                Solicitud {formatMoney(monto ?? 0)}
                {cuotas[0]?.cuota != null ? ` · cuota ${formatMoney(cuotas[0].cuota)}` : ''}
              </Text>
            ) : (
              <Text type="secondary">Complete cliente y condiciones. No pasa por el simulador.</Text>
            )}
          </div>
          <Button
            type="primary"
            icon={<FileAddOutlined />}
            disabled={!camposListos}
            loading={solicitar.isPending}
            onClick={() =>
              cajaConfirm({
                title: 'Solicitar crédito',
                content: `Se generará el crédito de ${formatMoney(monto ?? 0)} para aprobación. ¿Continuar?`,
                onOk: () => solicitar.mutateAsync(),
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
