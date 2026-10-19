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
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { ClienteBuscarAutoComplete } from '../../components/caja/ClienteBuscarAutoComplete'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import {
  buildSimuladorPrendarioPath,
  prendaVacia,
  totalTasacion,
  validarMontoVsTasacion,
  validarPrendasForm,
  type PrendaCampoError,
} from '../../utils/prendas'
import { formatMoney } from '../../utils/formatMoney'
import {
  PRENDARIO_NUEVO_PATH,
  buildClientesNuevoHref,
  labelClienteFicha,
} from '../../utils/internalReturnTo'

const { Paragraph, Text } = Typography

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

  const montoDesembolsar = useMemo(() => {
    if (montoCredito == null || !(montoCredito > 0)) return null
    // Solo CAP descuenta trámite del desembolso; prendario usa ADE (pago adelantado).
    if (indGastosAdm === 'CAP') {
      return Math.max(0, montoCredito - (gastosAdm ?? 0))
    }
    return montoCredito
  }, [montoCredito, gastosAdm, indGastosAdm])

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

  const tasacion = useMemo(() => totalTasacion(prendas), [prendas])
  const stats: CredixStatItem[] = []
  if (personaId) {
    stats.push({ label: 'Cliente', value: clienteLabel || `Persona #${personaId}` })
  }
  if (tasacion > 0) {
    stats.push({ label: 'Tasación', value: formatMoney(tasacion), tone: 'green' })
  }

  return (
    <CredixPage
      title="Nuevo crédito prendario"
      subtitle="Cliente, bienes y plan de crédito (monto, interés, trámite, primer pago) en esta pantalla. Luego confirma la simulación y solicita; PDFs en la misma ventana siguen en curso."
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

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <CredixPanel title="Buscar cliente existente">
            <Paragraph type="secondary" style={{ marginBottom: 12 }}>
              Escriba DNI, apellido o nombre: aparecen coincidencias al instante. Toque una vez el
              resultado para seleccionarlo (escritorio y móvil).
            </Paragraph>
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
          </CredixPanel>
        </Col>

        <Col xs={24} lg={12}>
          <CredixPanel title="Nuevo cliente (Clientes + ApiPerú)">
            <Paragraph type="secondary" style={{ marginBottom: 12 }}>
              El alta vive en el módulo de Clientes: consulta RENIEC y guarda la ficha completa. Aquí
              solo se consulta el DNI para no duplicar.
            </Paragraph>
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
        </Col>
      </Row>

      <CredixPanel title="Bienes en custodia">
        {personaId ? (
          <Alert
            type="success"
            showIcon
            style={{ marginBottom: 12 }}
            message={
              recienRegistrado
                ? `Cliente registrado y seleccionado: ${clienteLabel || `Persona #${personaId}`}`
                : `Cliente seleccionado: ${clienteLabel || `Persona #${personaId}`}`
            }
            description={
              recienRegistrado
                ? 'No hace falta buscarlo de nuevo. Continúe con los bienes en custodia.'
                : undefined
            }
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
            message="Seleccione un cliente existente o regístrelo en Clientes antes de cargar los bienes."
          />
        )}
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Los bienes se registran una sola vez aquí"
          description="En gestión solo se consultan. El vencimiento lo calcula el simulador con la modalidad (mensual en prendario), las cuotas y la fecha del primer pago."
        />
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
              value={montoCredito}
              onChange={(v) => setMontoCredito(v)}
              prefix="S/"
            />
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
              disabled={!personaId}
              value={gastosAdm}
              onChange={(v) => setGastosAdm(v)}
              prefix="S/"
            />
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
              min={0}
              max={100}
              step={0.1}
              disabled={!personaId}
              value={interesMensual}
              onChange={(v) => setInteresMensual(v)}
            />
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Text type="secondary">Primer pago</Text>
            <Input
              type="date"
              style={{ width: '100%', marginTop: 4 }}
              disabled={!personaId}
              value={fechaPrimerPago}
              onChange={(e) => setFechaPrimerPago(e.target.value)}
            />
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
        {montoDesembolsar != null ? (
          <Paragraph style={{ marginTop: 12, marginBottom: 4 }}>
            <Text strong style={{ color: 'var(--credix-primary, #1677ff)' }}>
              Monto a desembolsar: {formatMoney(montoDesembolsar)}
            </Text>
            <Text type="secondary">
              {' '}
              (con ADE = monto del crédito; el trámite adm. se cobra aparte
              {indCentralRiesgo ? ', con cargo de central de riesgo' : ''})
            </Text>
          </Paragraph>
        ) : null}
        <Paragraph type="secondary" style={{ marginTop: 8, marginBottom: 4 }}>
          Modalidad prendaria fija: mensual · 1 cuota. Al continuar se crea la solicitud con los
          bienes (ya no editables) y se abre el simulador con estos datos para confirmar el plan.
        </Paragraph>
        <Button
          type="primary"
          icon={<FileAddOutlined />}
          disabled={!personaId || oficinaId < 1}
          loading={crear.isPending}
          onClick={() => crear.mutate()}
        >
          Continuar: simular y solicitar
        </Button>
      </CredixPanel>
    </CredixPage>
  )
}
