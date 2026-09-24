import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import {
  FileAddOutlined,
  FileSearchOutlined,
  UserAddOutlined,
} from '@ant-design/icons'
import { Alert, Button, Col, Input, Row, Space, Steps, Typography, message } from 'antd'
import { consultarDniApiPeru } from '../../api/apiperu'
import { obtenerCliente, obtenerPersonaPorDocumento } from '../../api/clientes'
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
  prendasValidas,
  totalTasacion,
} from '../../utils/prendas'
import { formatMoney } from '../../utils/formatMoney'
import {
  PRENDARIO_NUEVO_PATH,
  buildClientesNuevoHref,
  labelClienteFicha,
} from '../../utils/internalReturnTo'

const { Paragraph, Text } = Typography

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
      const bienes = prendasValidas(prendas)
      if (bienes.length === 0) {
        throw new Error('Registre al menos un bien con tasación')
      }
      const solicitud = await crearSolicitudPrendaria({ oficinaId, personaId })
      await guardarBienesPrendario({
        oficinaId,
        creditoId: solicitud.solicitudCreditoId,
        prendas: bienes,
        fechaRemate: null,
      })
      return { solicitudCreditoId: solicitud.solicitudCreditoId, bienes }
    },
    onSuccess: ({ solicitudCreditoId, bienes }) => {
      message.success(`Solicitud prendaria #${solicitudCreditoId} creada`)
      if (!personaId) return
      navigate(
        buildSimuladorPrendarioPath({
          personaId,
          solicitudCreditoId,
          prendas: bienes,
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
      subtitle="Registre el cliente y el bien en custodia. Tras crear la solicitud, el plan (cuotas y 1.er pago) define el vencimiento; el remate es vencimiento + 30 días."
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
        current={personaId ? 1 : 0}
        style={{ marginBottom: 24, maxWidth: 640 }}
        items={[{ title: 'Cliente' }, { title: 'Bienes' }, { title: 'Simulador' }]}
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
        <PrendasEditor value={prendas} onChange={setPrendas} disabled={!personaId} />
        <Paragraph style={{ marginTop: 12, marginBottom: 4 }}>
          <Text strong>Siguiente paso:</Text> se guardan los bienes y se abre el simulador para
          definir monto, plazo y tasa. Luego no podrá agregar ni editar bienes en este crédito.
        </Paragraph>
        <Button
          type="primary"
          icon={<FileAddOutlined />}
          disabled={!personaId || oficinaId < 1 || prendasValidas(prendas).length === 0}
          loading={crear.isPending}
          onClick={() => crear.mutate()}
        >
          Crear solicitud y simular
        </Button>
      </CredixPanel>
    </CredixPage>
  )
}
