import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
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
  Select,
  Space,
  Steps,
  Typography,
  message,
} from 'antd'
import { consultarDniApiPeru } from '../../api/apiperu'
import { obtenerCliente, obtenerPersonaPorDocumento } from '../../api/clientes'
import { calcularGastosAdm } from '../../api/gastosAdm'
import { fetchProductos } from '../../api/productos'
import { crearSolicitudPrendaria, guardarBienesPrendario } from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { PrendaItem } from '../../api/creditoGestion'
import { CredixPage, CredixPanel, CredixWorkCanvas, type CredixStatItem } from '../../components/credix'
import { ClienteBuscarAutoComplete } from '../../components/caja/ClienteBuscarAutoComplete'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import {
  buildSimuladorPrendarioPath,
  prendaVacia,
  totalTasacion,
  ltvObservado,
  validarMontoVsTasacion,
  validarPrendasForm,
  type PrendaCampoError,
} from '../../utils/prendas'
import { CLIENTE_CRUZADO_AVISO } from '../../utils/clienteCruzado'
import { formatMoney } from '../../utils/formatMoney'
import {
  PRENDARIO_NUEVO_PATH,
  buildClientesNuevoHref,
  labelClienteFicha,
} from '../../utils/internalReturnTo'

const { Text } = Typography

const IND_GASTOS_ADM = 'ADE' as const

function defaultFechaPrimerPagoPrendario(): string {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  d.setMonth(d.getMonth() + 1)
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
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
  const [searchParams, setSearchParams] = useSearchParams()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const personaIdUrl = Number(searchParams.get('personaId') ?? 0)
  const recienRegistrado = searchParams.get('origen') === 'alta'

  const [terminoCliente, setTerminoCliente] = useState('')
  const [dniConsulta, setDniConsulta] = useState('')
  const [consulta, setConsulta] = useState<ConsultaDni | null>(null)
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])
  const [erroresPrendas, setErroresPrendas] = useState<PrendaCampoError[]>([])
  /** Campos de crédito (paridad Gestionar legacy / simulador); se llevan al simulador. */
  const [montoCredito, setMontoCredito] = useState<number | null>(500)
  const [interesMensual, setInteresMensual] = useState<number | null>(8)
  const [nroCuotas] = useState(1)
  const [formaPago] = useState('M')
  const [gastosAdm, setGastosAdm] = useState<number | null>(5)
  const [fechaPrimerPago, setFechaPrimerPago] = useState(defaultFechaPrimerPagoPrendario)
  const [observacion, setObservacion] = useState('')
  const [indCentralRiesgo, setIndCentralRiesgo] = useState(true)
  const [indGastosAdm] = useState(IND_GASTOS_ADM)

  const productosQuery = useQuery({
    queryKey: ['productos'],
    queryFn: fetchProductos,
    staleTime: 10 * 60_000,
  })
  const productoPrendario = useMemo(
    () => productosQuery.data?.find((p) => p.productoId === 2) ?? null,
    [productosQuery.data],
  )

  /** Paridad legacy: central de riesgo solo cambia el trámite (GastosAdm), no el desembolso (ADE = monto). */
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

  // Prendario usa ADE: el desembolso es el monto del crédito (CAP restaría el trámite; no aplica aquí).
  const montoDesembolsar =
    montoCredito != null && montoCredito > 0 ? montoCredito : null

  const elegirCliente = (id: number, label: string) => {
    setPersonaId(id)
    setClienteLabel(label)
    setTerminoCliente(label)
    setConsulta(null)
    const next = new URLSearchParams(searchParams)
    next.set('personaId', String(id))
    setSearchParams(next, { replace: true })
  }

  const limpiarCliente = () => {
    setPersonaId(null)
    setClienteLabel('')
    setTerminoCliente('')
    if (!searchParams.has('personaId')) return
    const next = new URLSearchParams(searchParams)
    next.delete('personaId')
    setSearchParams(next, { replace: true })
  }

  useEffect(() => {
    if (personaIdUrl < 1) return
    if (personaId === personaIdUrl) return
    let cancelled = false
    void obtenerCliente(personaIdUrl)
      .then((c) => {
        if (cancelled) return
        const label = labelClienteFicha(c)
        setPersonaId(c.personaId)
        setClienteLabel(label)
        setTerminoCliente(label)
        if (recienRegistrado) {
          message.success(`Cliente listo: ${label}`)
        }
      })
      .catch((e) => {
        if (!cancelled) message.error(errMsg(e))
      })
    return () => {
      cancelled = true
    }
  }, [personaIdUrl, personaId, recienRegistrado])

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

  const crear = useMutation({
    mutationFn: async () => {
      if (!personaId) {
        throw new Error('Seleccione o registre un cliente')
      }
      if (oficinaId < 1) {
        throw new Error('Sesión sin oficina. Vuelva a iniciar sesión.')
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
      if (!fechaPrimerPago?.trim()) {
        throw new Error('Indique la fecha del primer pago')
      }
      if (gastosAdm == null || gastosAdm < 0) {
        throw new Error('Indique el trámite administrativo')
      }
      const solicitud = await crearSolicitudPrendaria({ oficinaId, personaId })
      await guardarBienesPrendario({
        oficinaId,
        creditoId: solicitud.solicitudCreditoId,
        prendas: validacion.bienes,
        fechaRemate: null,
      })
      return {
        solicitudCreditoId: solicitud.solicitudCreditoId,
        bienes: validacion.bienes,
        credito: {
          monto: montoCredito,
          interesMensual,
          nroCuotas,
          formaPago,
          fechaPrimerPago: fechaPrimerPago.trim().slice(0, 10),
          gastosAdm,
          observacion: observacion.trim() || null,
          indCentralRiesgo,
        },
      }
    },
    onSuccess: ({ solicitudCreditoId, bienes, credito }) => {
      setErroresPrendas([])
      message.success(`Solicitud prendaria #${solicitudCreditoId} creada`)
      if (!personaId) return
      navigate(
        buildSimuladorPrendarioPath({
          personaId,
          solicitudCreditoId,
          prendas: bienes,
          credito,
        }),
      )
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const validacionBienes = useMemo(() => validarPrendasForm(prendas), [prendas])
  /** Suma de todos los bienes válidos (descripción + tasación > 0), incl. 2+. */
  const tasacion = useMemo(() => totalTasacion(prendas), [prendas])
  const cantidadBienes = validacionBienes.bienes.length

  // No usar max= en InputNumber: Ant Design recorta el valor y oculta el exceso → no se veía la alerta.
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
    if (!fechaPrimerPago?.trim()) return 'Indique la fecha del primer pago'
    return null
  }, [fechaPrimerPago])

  const bienesListos = validacionBienes.ok

  // Mostrar errores de filas en cuanto hay contenido (varios bienes incluidos).
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

  const stats: CredixStatItem[] = []
  if (personaId) {
    stats.push({ label: 'Cliente', value: clienteLabel || `Persona #${personaId}` })
  }
  if (tasacion > 0) {
    stats.push({
      label: cantidadBienes > 1 ? `Tasación (${cantidadBienes} bienes)` : 'Tasación',
      value: formatMoney(tasacion),
      tone: 'green',
    })
  }
  if (montoCredito != null && montoCredito > 0 && tasacion > 0 && !errorMontoVsTasacion) {
    const ltv = ltvObservado(montoCredito, tasacion)
    if (ltv != null) {
      stats.push({
        label: 'LTV',
        value: `${ltv}%`,
      })
    }
  }

  return (
    <CredixPage
      title="Nuevo crédito prendario"
      subtitle={undefined}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: <Link to="/credito/prendario">Prendario</Link> },
        { title: 'Nuevo' },
      ]}
      stats={stats}
    >
      <Steps
        size="small"
        current={personaId ? (tasacion > 0 ? 2 : 1) : 0}
        style={{ marginBottom: 24, maxWidth: 720 }}
        items={[
          { title: 'Cliente' },
          { title: 'Bienes y crédito' },
          { title: 'Simular y solicitar' },
        ]}
      />

      <CredixWorkCanvas
        context={(
          <div>
          <CredixPanel title="Cliente existente">
            <ClienteBuscarAutoComplete
              variant="credito"
              value={terminoCliente}
              onChange={(v) => {
                setTerminoCliente(v)
                if (personaId == null) return
                setPersonaId(null)
                setClienteLabel('')
                if (!searchParams.has('personaId')) return
                const next = new URLSearchParams(searchParams)
                next.delete('personaId')
                setSearchParams(next, { replace: true })
              }}
              onSelectPersona={elegirCliente}
              fullWidth
              showSearchButton
              searchButtonLabel="Buscar"
              debounceMs={280}
              minChars={2}
              placeholder="DNI, apellido o nombre"
            />
            <Text type="secondary" style={{ display: 'block', marginTop: 8, fontSize: 12 }}>
              {CLIENTE_CRUZADO_AVISO}
            </Text>
          </CredixPanel>
          <CredixPanel title="Nuevo cliente">
            <Space.Compact style={{ width: '100%', marginBottom: 12 }}>
              <Input
                placeholder="DNI 8 dígitos"
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
                Consultar DNI
              </Button>
            </Space.Compact>

            {consulta?.kind === 'existente' ? (
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 12 }}
                message={consulta.label}
                description="Cliente ya registrado. Pulse Elegir para continuar."
                action={
                  <Button
                    size="small"
                    type="primary"
                    onClick={() => elegirCliente(consulta.personaId, consulta.label)}
                  >
                    Elegir
                  </Button>
                }
              />
            ) : null}

            {consulta?.kind === 'nuevo' ? (
              <Alert
                type="success"
                showIcon
                style={{ marginBottom: 12 }}
                message={`${consulta.nombres} ${consulta.apePaterno} ${consulta.apeMaterno}`.trim()}
                description="Nombres desde RENIEC. Complete celular, domicilio y calificación en Clientes."
              />
            ) : null}

            {consulta?.kind === 'sinReniec' ? (
              <Alert
                type="warning"
                showIcon
                style={{ marginBottom: 12 }}
                message="RENIEC no devolvió datos. Puede registrar la ficha a mano en Clientes."
              />
            ) : null}

            <Space wrap>
              <Button
                type="primary"
                icon={<UserAddOutlined />}
                onClick={() =>
                  irAFichaCliente(
                    consulta && consulta.kind !== 'existente' ? consulta.dni : dniConsulta || undefined,
                  )
                }
              >
                Completar ficha en Clientes
              </Button>
            </Space>
          </CredixPanel>
          </div>
        )}
        work={(
      <CredixPanel title="Bienes y crédito">
        {personaId ? (
          <Alert
            type="success"
            showIcon
            style={{ marginBottom: 12 }}
            message={clienteLabel || `Persona #${personaId}`}
            action={
              <Button size="small" onClick={limpiarCliente}>
                Cambiar
              </Button>
            }
          />
        ) : (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message="Seleccione un cliente para cargar bienes."
          />
        )}
        <PrendasEditor
          value={prendas}
          onChange={(next) => {
            setPrendas(next)
            if (erroresPrendas.length > 0) setErroresPrendas([])
          }}
          disabled={!personaId}
          errores={erroresPrendas}
        />
        <Row gutter={[16, 12]} style={{ marginTop: 16 }}>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">Modalidad</Text>
            <Select
              style={{ width: '100%', marginTop: 4 }}
              disabled
              value={formaPago}
              options={[{ value: 'M', label: 'MENSUAL' }]}
            />
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">Trámite administrativo</Text>
            <Select
              style={{ width: '100%', marginTop: 4 }}
              disabled={!personaId}
              value={indGastosAdm}
              options={[
                { value: IND_GASTOS_ADM, label: 'Trámite adm. pago adelantado (ADE)' },
              ]}
            />
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">&nbsp;</Text>
            <div style={{ marginTop: 8 }}>
              <Checkbox
                disabled={!personaId}
                checked={indCentralRiesgo}
                onChange={(e) => setIndCentralRiesgo(e.target.checked)}
              >
                Incluir central de riesgo al trámite
              </Checkbox>
            </div>
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">Monto del crédito (S/)</Text>
            <InputNumber
              style={{ width: '100%', marginTop: 4 }}
              min={1}
              step={10}
              disabled={!personaId}
              status={errorMontoVsTasacion ? 'error' : undefined}
              value={montoCredito}
              onChange={(v) => setMontoCredito(v)}
              prefix="S/"
            />
            {tasacion > 0 ? (
              <Text
                type={errorMontoVsTasacion ? 'danger' : 'secondary'}
                style={{ display: 'block', marginTop: 4, fontSize: 12 }}
              >
                Tope = suma de {cantidadBienes} bien{cantidadBienes === 1 ? '' : 'es'}:{' '}
                {formatMoney(tasacion)}
                {montoCredito != null && montoCredito > 0 && !errorMontoVsTasacion
                  ? ` · queda ${formatMoney(Math.max(0, tasacion - montoCredito))}`
                  : null}
                {montoCredito != null && montoCredito > tasacion
                  ? ` · exceso ${formatMoney(montoCredito - tasacion)}`
                  : null}
              </Text>
            ) : personaId ? (
              <Text type="danger" style={{ display: 'block', marginTop: 4, fontSize: 12 }}>
                Sin tasación válida aún. Agregue uno o más bienes con descripción y tasación &gt; 0.
              </Text>
            ) : null}
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">Número de cuotas</Text>
            <InputNumber
              style={{ width: '100%', marginTop: 4 }}
              min={1}
              disabled
              value={nroCuotas}
            />
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">Trámite adm. (S/)</Text>
            <InputNumber
              style={{ width: '100%', marginTop: 4 }}
              min={0}
              step={1}
              disabled
              value={gastosAdm}
              prefix="S/"
            />
            <Text type="secondary" style={{ display: 'block', marginTop: 4, fontSize: 12 }}>
              Calculado según monto y central de riesgo (tabla GastosAdm).
            </Text>
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">
              Interés mensual (%)
              {productoPrendario ? (
                <Text type="secondary" style={{ fontWeight: 400 }}>
                  {' '}
                  (min {productoPrendario.interesMinima.toFixed(2)} · max{' '}
                  {productoPrendario.interesMaxima.toFixed(2)})
                </Text>
              ) : null}
            </Text>
            <InputNumber
              style={{ width: '100%', marginTop: 4 }}
              min={productoPrendario?.interesMinima ?? 0}
              max={productoPrendario?.interesMaxima ?? 100}
              step={0.1}
              disabled={!personaId}
              status={errorInteres ? 'error' : undefined}
              value={interesMensual}
              onChange={(v) => setInteresMensual(v)}
            />
            {errorInteres ? (
              <Text type="danger" style={{ display: 'block', marginTop: 4, fontSize: 12 }}>
                {errorInteres}
              </Text>
            ) : null}
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">Primer pago</Text>
            <Input
              type="date"
              style={{ width: '100%', marginTop: 4 }}
              disabled={!personaId}
              status={errorFecha && personaId ? 'error' : undefined}
              value={fechaPrimerPago}
              onChange={(e) => setFechaPrimerPago(e.target.value)}
            />
            {errorFecha && personaId ? (
              <Text type="danger" style={{ display: 'block', marginTop: 4, fontSize: 12 }}>
                {errorFecha}
              </Text>
            ) : null}
          </Col>
          <Col xs={24} md={16}>
            <Text type="secondary">Observación</Text>
            <Input
              style={{ width: '100%', marginTop: 4 }}
              disabled={!personaId}
              value={observacion}
              onChange={(e) => setObservacion(e.target.value)}
              maxLength={200}
              placeholder="Opcional"
            />
          </Col>
        </Row>
        {errorMontoVsTasacion ? (
          <Alert
            type="error"
            showIcon
            style={{ marginTop: 12 }}
            message="Monto mayor que la tasación"
            description={
              <Space direction="vertical" size={8} style={{ width: '100%' }}>
                <Text>{errorMontoVsTasacion}</Text>
                {tasacion > 0 ? (
                  <Button
                    size="small"
                    type="primary"
                    ghost
                    onClick={() => setMontoCredito(Number(tasacion.toFixed(2)))}
                  >
                    Ajustar monto a la tasación ({formatMoney(tasacion)})
                  </Button>
                ) : null}
              </Space>
            }
          />
        ) : null}
      </CredixPanel>
        )}
        result={(
          <CredixPanel title="Resultado">
            {montoDesembolsar != null && !errorMontoVsTasacion ? (
              <Text strong style={{ display: 'block', marginBottom: 12, fontSize: 20 }}>
                Desembolso {formatMoney(montoDesembolsar)}
              </Text>
            ) : (
              <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
                Complete cliente, bienes y monto.
              </Text>
            )}
            {!puedeContinuar && personaId ? (
              <Alert
                type="warning"
                showIcon
                style={{ marginBottom: 12 }}
                message={
                  !bienesListos
                    ? 'Falta un bien con tasación.'
                    : errorMontoVsTasacion
                      ? 'El monto supera la tasación.'
                      : errorInteres
                        ? errorInteres
                        : !fechaPrimerPago?.trim()
                          ? 'Indique el primer pago.'
                          : 'Revise monto e interés.'
                }
              />
            ) : null}
            <Button
              type="primary"
              icon={<FileAddOutlined />}
              disabled={!puedeContinuar}
              loading={crear.isPending}
              onClick={() => crear.mutate()}
              block
            >
              Simular y solicitar
            </Button>
          </CredixPanel>
        )}
      />
    </CredixPage>
  )
}
