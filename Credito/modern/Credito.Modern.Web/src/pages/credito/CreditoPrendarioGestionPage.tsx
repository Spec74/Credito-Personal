import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { CalculatorOutlined, FilePdfOutlined, PlusOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Alert, Button, Select, Space, Tooltip, Typography, message } from 'antd'
import {
  fetchCreditoContexto,
  fetchCreditosGrillaPersona,
  fetchPersonaCreditoFicha,
  fetchPrendas,
  type PrendaItem,
} from '../../api/creditoGestion'
import {
  downloadActaEntregaPrendarioPdf,
  downloadContratoPrendarioPdf,
} from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'
import { prendaAItem, prendaVacia, totalTasacion, buildSimuladorPrendarioPath } from '../../utils/prendas'
import { abrirWhatsAppPrendario } from '../../utils/prendarioWhatsapp'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

function labelFormaPago(codigo: string | null | undefined): string {
  switch ((codigo ?? '').toUpperCase()) {
    case 'M':
      return 'Mensual'
    case 'Q':
      return 'Quincenal'
    case 'S':
      return 'Semanal'
    case 'D':
      return 'Diario'
    default:
      return codigo?.trim() || '—'
  }
}

const ESTADOS_SOLICITUD = new Set(['CRE', 'PEN', 'AP1', 'APR'])

export function CreditoPrendarioGestionPage() {
  const navigate = useNavigate()
  const { personaId: personaParam } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const personaId = Number(personaParam)
  const creditoIdUrl = Number(searchParams.get('creditoId') ?? 0)

  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])

  const ficha = useQuery({
    queryKey: ['persona-credito-ficha', oficinaId, personaId],
    queryFn: () => fetchPersonaCreditoFicha(oficinaId, personaId),
    enabled: oficinaId > 0 && personaId > 0,
  })

  const grilla = useQuery({
    queryKey: ['creditos-grilla-persona', oficinaId, personaId],
    queryFn: () =>
      fetchCreditosGrillaPersona({ oficinaId, personaId, grupoActivo: true, page: 1, pageSize: 50 }),
    enabled: oficinaId > 0 && personaId > 0,
  })

  const creditosOpciones = useMemo(() => grilla.data?.items ?? [], [grilla.data])

  const creditoId = useMemo(() => {
    if (creditoIdUrl > 0) {
      return creditoIdUrl
    }
    if (ficha.data?.solicitudCreditoId) {
      return ficha.data.solicitudCreditoId
    }
    const solicitudes = creditosOpciones.filter((c) =>
      ESTADOS_SOLICITUD.has((c.estado ?? '').toUpperCase()),
    )
    if (solicitudes.length === 1) {
      return solicitudes[0].creditoId
    }
    if (creditosOpciones.length === 1) {
      return creditosOpciones[0].creditoId
    }
    return 0
  }, [creditoIdUrl, ficha.data, creditosOpciones])

  const contexto = useQuery({
    queryKey: ['credito-contexto', creditoId],
    queryFn: () => fetchCreditoContexto(creditoId),
    enabled: creditoId > 0,
  })

  const bienes = useQuery({
    queryKey: ['prendas', oficinaId, creditoId],
    queryFn: () => fetchPrendas(oficinaId, creditoId),
    enabled: oficinaId > 0 && creditoId > 0,
  })

  useEffect(() => {
    const guardadas = bienes.data ?? []
    setPrendas(guardadas.length > 0 ? guardadas.map(prendaAItem) : [prendaVacia()])
  }, [bienes.data])

  const imprimirContrato = useMutation({
    mutationFn: () =>
      downloadContratoPrendarioPdf(
        oficinaId,
        creditoId,
        contexto.data?.numeroContratoPrendario || String(creditoId),
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const imprimirActa = useMutation({
    mutationFn: () =>
      downloadActaEntregaPrendarioPdf(
        oficinaId,
        creditoId,
        contexto.data?.numeroContratoPrendario || String(creditoId),
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const bienesPersistidos = (bienes.data ?? []).length > 0
  const puedeImprimir =
    bienesPersistidos && Boolean(contexto.data?.numeroContratoPrendario)
  const estadoMeta = getCreditoEstadoMeta(contexto.data?.estado)
  const necesitaElegirCredito =
    creditoIdUrl < 1 &&
    !ficha.data?.solicitudCreditoId &&
    creditosOpciones.length > 1 &&
    creditoId < 1

  const remateMostrado =
    contexto.data?.fechaRemate ??
    (contexto.data?.fechaVencimiento
      ? (() => {
          const d = new Date(contexto.data.fechaVencimiento)
          d.setDate(d.getDate() + 30)
          return d.toISOString()
        })()
      : null)

  const stats: CredixStatItem[] = []
  if (ficha.data) {
    stats.push({ label: 'Cliente', value: ficha.data.nombreCompleto })
    stats.push({ label: 'DNI', value: ficha.data.numeroDocumento })
  }
  if (contexto.data?.numeroContratoPrendario) {
    stats.push({ label: 'Contrato', value: contexto.data.numeroContratoPrendario })
  }
  const tasacion = totalTasacion(prendas)
  if (tasacion > 0) {
    stats.push({ label: 'Tasación', value: formatMoney(tasacion), tone: 'green' })
  }

  if (personaId < 1) {
    return (
      <CredixPage
        title="Gestión prendaria"
        breadcrumb={[{ title: <Link to="/credito/prendario">Prendario</Link> }]}
      >
        <Alert type="error" message="Persona no indicada." />
      </CredixPage>
    )
  }

  return (
    <CredixPage
      title="Gestión prendaria"
      subtitle="Consulta de bienes en custodia, contrato y acta. Los bienes se registran solo en la solicitud."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: <Link to="/credito/prendario">Prendario</Link> },
        { title: ficha.data?.nombreCompleto ?? 'Gestión' },
      ]}
      stats={stats}
      actions={
        <Space wrap>
          <Button icon={<PlusOutlined />} onClick={() => navigate('/credito/prendario/nuevo')}>
            Nuevo
          </Button>
          {creditoId > 0 ? (
            <Button
              icon={<CalculatorOutlined />}
              onClick={() =>
                navigate(
                  buildSimuladorPrendarioPath({
                    personaId,
                    solicitudCreditoId: creditoId,
                    prendas,
                    fechaRemate: contexto.data?.fechaRemate?.slice(0, 10) ?? '',
                  }),
                )
              }
            >
              Simulador
            </Button>
          ) : null}
        </Space>
      }
    >
      {necesitaElegirCredito ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Seleccione el crédito prendario a gestionar"
          description={
            <Select
              style={{ width: '100%', maxWidth: 420, marginTop: 8 }}
              placeholder="Crédito activo"
              options={creditosOpciones.map((c) => ({
                value: c.creditoId,
                label: `#${c.creditoId} · ${c.estado} · S/ ${formatMoney(c.montoCredito)}`,
              }))}
              onChange={(id: number) => {
                setSearchParams({ creditoId: String(id) }, { replace: true })
              }}
            />
          }
        />
      ) : null}

      {!creditoId && !necesitaElegirCredito ? (
        <Alert
          type="info"
          showIcon
          message="Este cliente no tiene una solicitud prendaria."
          action={
            <Button type="primary" onClick={() => navigate(`/credito/prendario/nuevo?personaId=${personaId}`)}>
              Ir a nueva solicitud
            </Button>
          }
        />
      ) : creditoId > 0 ? (
        <CredixPanel
          title={`Crédito #${creditoId}`}
          extra={
            <Space wrap>
              <Button
                icon={<FilePdfOutlined />}
                loading={imprimirContrato.isPending}
                disabled={!puedeImprimir}
                onClick={() => imprimirContrato.mutate()}
              >
                Contrato
              </Button>
              <Button
                icon={<FilePdfOutlined />}
                loading={imprimirActa.isPending}
                disabled={!puedeImprimir}
                onClick={() => imprimirActa.mutate()}
              >
                Acta de entrega
              </Button>
              <Tooltip title="Abre un chat libre con el cliente. El aviso oficial de vencimiento (plantilla a 3 días) se envía desde el listado o de forma automática a las 08:00.">
                <Button
                  icon={<WhatsAppOutlined />}
                  onClick={() => {
                    const ok = abrirWhatsAppPrendario(
                      contexto.data?.personaCelular,
                      ficha.data?.nombreCompleto ?? '',
                      creditoId,
                    )
                    if (!ok) {
                      message.warning('Este cliente no tiene celular registrado')
                    }
                  }}
                >
                  Chat WhatsApp
                </Button>
              </Tooltip>
            </Space>
          }
        >
          {creditosOpciones.length > 1 ? (
            <Select
              style={{ width: '100%', maxWidth: 420, marginBottom: 12 }}
              value={creditoId}
              options={creditosOpciones.map((c) => ({
                value: c.creditoId,
                label: `#${c.creditoId} · ${c.estado} · S/ ${formatMoney(c.montoCredito)}`,
              }))}
              onChange={(id: number) => {
                setSearchParams({ creditoId: String(id) }, { replace: true })
              }}
            />
          ) : null}

          <Paragraph type="secondary" style={{ marginBottom: 8 }}>
            Estado{' '}
            {contexto.isLoading
              ? '…'
              : estadoMeta
                ? `${estadoMeta.codigo} — ${estadoMeta.label}`
                : (contexto.data?.estado ?? '—')}
            {contexto.data?.numeroContratoPrendario
              ? `. Contrato ${contexto.data.numeroContratoPrendario}.`
              : bienesPersistidos
                ? null
                : '. El contrato se asigna al registrar bienes en la solicitud.'}
          </Paragraph>

          <div style={{ marginBottom: 16 }}>
            <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>
              Condiciones del plan (definidas en el simulador)
            </Text>
            <Space wrap size={[16, 8]}>
              <Text>
                Modalidad: <Text strong>{labelFormaPago(contexto.data?.formaPago)}</Text>
              </Text>
              <Text>
                Cuotas: <Text strong>{contexto.data?.numeroCuotas || '—'}</Text>
              </Text>
              <Text>
                1.er pago: <Text strong>{formatFecha(contexto.data?.fechaPrimerPago)}</Text>
              </Text>
              <Text>
                Vencimiento:{' '}
                <Text strong>{formatFecha(contexto.data?.fechaVencimiento)}</Text>
              </Text>
              <Text>
                Remate (venc. + 30 d.): <Text strong>{formatFecha(remateMostrado)}</Text>
              </Text>
            </Space>
            <Paragraph type="secondary" style={{ marginTop: 8, marginBottom: 0 }}>
              El vencimiento es la última cuota del plan (1.er pago + cuotas). El remate es vencimiento
              + 30 días. Al desembolsar en caja, las fechas se realinean desde la fecha real de
              desembolso (base del contrato oficial).
            </Paragraph>
          </div>

          {!bienesPersistidos ? (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message="Sin bienes registrados"
              description={
                <span>
                  Registre el bien en{' '}
                  <Link to={`/credito/prendario/nuevo?personaId=${personaId}`}>nueva solicitud</Link>
                  . Aquí solo se consulta lo ya guardado.
                </span>
              }
            />
          ) : !puedeImprimir ? (
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message="Complete el simulador para asignar contrato e imprimir documentos."
            />
          ) : null}

          <PrendasEditor value={prendas} onChange={setPrendas} readOnly />
        </CredixPanel>
      ) : null}
    </CredixPage>
  )
}
