import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalculatorOutlined, FilePdfOutlined, PlusOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Alert, Button, Input, Select, Space, Tooltip, Typography, message } from 'antd'
import {
  fetchCreditoContexto,
  fetchCreditosGrillaPersona,
  fetchPersonaCreditoFicha,
  fetchPrendas,
  type PrendaItem,
} from '../../api/creditoGestion'
import {
  crearSolicitudPrendaria,
  downloadActaEntregaPrendarioPdf,
  downloadContratoPrendarioPdf,
  guardarBienesPrendario,
} from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'
import { prendaAItem, prendaVacia, prendasValidas, totalTasacion, buildSimuladorPrendarioPath } from '../../utils/prendas'
import { abrirWhatsAppPrendario } from '../../utils/prendarioWhatsapp'

const { Paragraph } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

const ESTADOS_SOLICITUD = new Set(['CRE', 'PEN', 'AP1', 'APR'])

export function CreditoPrendarioGestionPage() {
  const navigate = useNavigate()
  const { personaId: personaParam } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const oficinaId = session?.oficinaId ?? 0
  const personaId = Number(personaParam)
  const creditoIdUrl = Number(searchParams.get('creditoId') ?? 0)

  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])
  const [fechaRemate, setFechaRemate] = useState('')

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
    setFechaRemate(contexto.data?.fechaRemate?.slice(0, 10) ?? '')
    const guardadas = bienes.data ?? []
    setPrendas(guardadas.length > 0 ? guardadas.map(prendaAItem) : [prendaVacia()])
  }, [contexto.data, bienes.data])

  const crearSolicitud = useMutation({
    mutationFn: () => crearSolicitudPrendaria({ oficinaId, personaId }),
    onSuccess: (r) => {
      message.success(`Solicitud #${r.solicitudCreditoId} lista`)
      navigate(`/credito/prendario/gestionar/${personaId}?creditoId=${r.solicitudCreditoId}`, {
        replace: true,
      })
      void queryClient.invalidateQueries({
        queryKey: ['creditos-grilla-persona', oficinaId, personaId],
      })
      void queryClient.invalidateQueries({
        queryKey: ['persona-credito-ficha', oficinaId, personaId],
      })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardar = useMutation({
    mutationFn: () =>
      guardarBienesPrendario({
        oficinaId,
        creditoId,
        prendas: prendasValidas(prendas),
        fechaRemate: fechaRemate || null,
      }),
    onSuccess: () => {
      message.success('Bienes en custodia guardados')
      void queryClient.invalidateQueries({ queryKey: ['prendas', oficinaId, creditoId] })
      void queryClient.invalidateQueries({ queryKey: ['credito-contexto', creditoId] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

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
      subtitle="Bienes en custodia, contrato y acta de entrega. El monto del préstamo se define en el simulador."
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
                    fechaRemate,
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
            <Button
              type="primary"
              loading={crearSolicitud.isPending}
              onClick={() => crearSolicitud.mutate()}
            >
              Crear solicitud
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
          <Paragraph type="secondary">
            Estado{' '}
            {contexto.isLoading
              ? '…'
              : estadoMeta
                ? `${estadoMeta.codigo} — ${estadoMeta.label}`
                : (contexto.data?.estado ?? '—')}
            {contexto.data ? (
              <>
                . Vence {formatFecha(contexto.data.fechaVencimiento)}. Contrato{' '}
                {contexto.data.numeroContratoPrendario ?? '(se asigna al guardar bienes)'}.
              </>
            ) : null}
          </Paragraph>
          {!puedeImprimir ? (
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message="Guarde los bienes en custodia antes de imprimir contrato o acta."
            />
          ) : null}
          <Paragraph type="secondary">Fecha de remate (vacía = vencimiento + 30 días)</Paragraph>
          <Input
            type="date"
            style={{ maxWidth: 220, marginBottom: 12 }}
            value={fechaRemate}
            onChange={(e) => setFechaRemate(e.target.value)}
          />
          <PrendasEditor value={prendas} onChange={setPrendas} />
          <Button
            type="primary"
            style={{ marginTop: 12 }}
            disabled={prendasValidas(prendas).length === 0}
            loading={guardar.isPending}
            onClick={() => guardar.mutate()}
          >
            Guardar bienes
          </Button>
        </CredixPanel>
      ) : null}
    </CredixPage>
  )
}
