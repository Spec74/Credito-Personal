import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalculatorOutlined, FilePdfOutlined, PlusOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Alert, Button, Input, InputNumber, Modal, Select, Space, Tooltip, Typography, message } from 'antd'
import {
  fetchCreditoContexto,
  fetchCreditosGrillaPersona,
  fetchPersonaCreditoFicha,
  fetchPrendas,
  guardarPrendas,
  type PrendaItem,
} from '../../api/creditoGestion'
import { prorrogarCredito } from '../../api/creditoPlanes'
import {
  devolverPrenda,
  downloadActaEntregaPrendarioPdf,
  downloadContratoPrendarioPdf,
  downloadDevolucionPrendarioPdf,
} from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { confirmMoney, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'
import {
  puedeCompletarBienesPrendarioUi,
  puedeProrrogarCreditoUi,
  esCreditoProductoPrendario,
} from '../../utils/creditoOperacionPermisos'
import { ltvObservado, prendaAItem, prendaVacia, totalTasacion, buildSimuladorPrendarioPath, validarPrendasForm, type PrendaCampoError } from '../../utils/prendas'
import { abrirWhatsAppPrendario } from '../../utils/prendarioWhatsapp'
import '../../styles/credito-prendario-gestion.css'

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
  const queryClient = useQueryClient()
  const { personaId: personaParam } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const { session } = useAuth()
  const roles = session?.roles ?? []
  const oficinaId = session?.oficinaId ?? 0
  const personaId = Number(personaParam)
  const creditoIdUrl = Number(searchParams.get('creditoId') ?? 0)
  const puedeCompletarBienes = puedeCompletarBienesPrendarioUi(roles)

  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])
  const [fechaRematePrenda, setFechaRematePrenda] = useState('')
  const [erroresPrendas, setErroresPrendas] = useState<PrendaCampoError[]>([])
  const [modalProrrogar, setModalProrrogar] = useState(false)
  const [diasProrroga, setDiasProrroga] = useState(7)
  const puedeProrrogar = puedeProrrogarCreditoUi(roles)

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
    setFechaRematePrenda(contexto.data?.fechaRemate?.slice(0, 10) ?? '')
  }, [bienes.data, contexto.data?.fechaRemate])

  const guardarBienes = useMutation({
    mutationFn: () => {
      const validacion = validarPrendasForm(prendas)
      setErroresPrendas(validacion.errores)
      if (!validacion.ok) {
        throw new Error(validacion.mensaje ?? 'Complete los bienes en custodia')
      }
      return guardarPrendas({
        oficinaId,
        creditoId,
        prendas: validacion.bienes,
        fechaRemate: fechaRematePrenda || null,
      })
    },
    onSuccess: () => {
      setErroresPrendas([])
      message.success('Bienes en custodia guardados')
      void queryClient.invalidateQueries({ queryKey: ['prendas', oficinaId, creditoId] })
      void queryClient.invalidateQueries({ queryKey: ['credito-contexto', creditoId] })
      void queryClient.invalidateQueries({ queryKey: ['prendario-creditos'] })
      void queryClient.invalidateQueries({ queryKey: ['prendario-resumen'] })
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

  const devolverBien = useMutation({
    mutationFn: (prendaId: number) => devolverPrenda({ oficinaId, prendaId }),
    onSuccess: async (res) => {
      message.success('Bien devuelto')
      void queryClient.invalidateQueries({ queryKey: ['prendas', oficinaId, creditoId] })
      try {
        await downloadDevolucionPrendarioPdf(oficinaId, res.prendaId)
      } catch (e) {
        message.warning(errMsg(e))
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const imprimirDevolucion = useMutation({
    mutationFn: (prendaId: number) => downloadDevolucionPrendarioPdf(oficinaId, prendaId),
    onError: (e) => message.error(errMsg(e)),
  })

  const prorrogar = useMutation({
    mutationFn: () => prorrogarCredito({ oficinaId, creditoId, dias: diasProrroga }),
    onSuccess: () => {
      setModalProrrogar(false)
      message.success('Vencimiento prorrogado')
      void queryClient.invalidateQueries({ queryKey: ['credito-contexto', creditoId] })
      void queryClient.invalidateQueries({ queryKey: ['prendario-creditos'] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const bienesPersistidos = (bienes.data ?? []).length > 0
  const esPrendario = esCreditoProductoPrendario(contexto.data ?? {})
  const puedeEditarBienes =
    puedeCompletarBienes && esPrendario && !bienesPersistidos && creditoId > 0
  const puedeImprimir = bienesPersistidos
  const creditoDesembolsado = (contexto.data?.estado ?? '').toUpperCase() === 'DES'
  const puedePedirDevolucion = puedeCompletarBienes && esPrendario && !creditoDesembolsado
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
  const montoCredito = creditosOpciones.find((c) => c.creditoId === creditoId)?.montoCredito
  const ltv = montoCredito != null ? ltvObservado(montoCredito, tasacion) : null
  if (ltv != null) {
    stats.push({ label: 'LTV', value: `${ltv}%` })
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
      subtitle={undefined}
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
                    fechaRemate:
                      fechaRematePrenda || (contexto.data?.fechaRemate?.slice(0, 10) ?? ''),
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
            <div className="credito-prendario-gestion-actions">
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
              {puedeProrrogar && creditoDesembolsado ? (
                <Button onClick={() => setModalProrrogar(true)}>Prorrogar</Button>
              ) : null}
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
            </div>
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
                : '. El contrato se asigna al registrar bienes.'}
          </Paragraph>

          <div className="credito-prendario-plan">
            <span className="credito-prendario-plan__title">
              Condiciones del plan
            </span>
            <div className="credito-prendario-plan__grid">
              {(
                [
                  ['Modalidad', labelFormaPago(contexto.data?.formaPago)],
                  ['Cuotas', String(contexto.data?.numeroCuotas || '—')],
                  ['1.er pago', formatFecha(contexto.data?.fechaPrimerPago)],
                  ['Vencimiento', formatFecha(contexto.data?.fechaVencimiento)],
                  ['Remate (venc. + 30 d.)', formatFecha(remateMostrado)],
                ] as const
              ).map(([label, value]) => (
                <div key={label} className="credito-prendario-plan__item">
                  <span className="credito-prendario-plan__item-label">{label}</span>
                  <span className="credito-prendario-plan__item-value">{value}</span>
                </div>
              ))}
            </div>
          </div>

          {!bienesPersistidos ? (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message="Sin bienes registrados"
              description={
                puedeEditarBienes
                  ? 'Registre los bienes una sola vez en este crédito. No se abrirá una solicitud nueva.'
                  : 'Este crédito prendario aún no tiene bienes. Un analista o administrador puede completarlos.'
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

          {puedeEditarBienes ? (
            <div style={{ marginBottom: 12, maxWidth: 280 }}>
              <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>
                Fecha remate (opcional)
              </Text>
              <Input
                type="date"
                value={fechaRematePrenda}
                onChange={(e) => setFechaRematePrenda(e.target.value)}
              />
              <Text type="secondary" style={{ fontSize: 12 }}>
                Si se deja vacía, vencimiento + 30 días.
              </Text>
            </div>
          ) : null}

          <PrendasEditor
            value={prendas}
            onChange={(next) => {
              setPrendas(next)
              if (erroresPrendas.length > 0) setErroresPrendas([])
            }}
            disabled={!puedeEditarBienes}
            readOnly={!puedeEditarBienes}
            errores={erroresPrendas}
          />
          {(bienes.data ?? []).length > 0 ? (
            <div style={{ marginTop: 12 }}>
              {(bienes.data ?? []).map((b) => {
                const estado = (b.estado ?? 'EN CUSTODIA').trim().toUpperCase()
                const enCustodia = estado === 'EN CUSTODIA'
                return (
                  <div
                    key={b.prendaId}
                    style={{
                      display: 'flex',
                      flexWrap: 'wrap',
                      gap: 8,
                      alignItems: 'center',
                      marginBottom: 8,
                    }}
                  >
                    <Text>
                      {b.descripcion || 'Bien'} · {estado}
                    </Text>
                    {enCustodia ? (
                      <Button
                        size="small"
                        disabled={!puedePedirDevolucion || devolverBien.isPending}
                        onClick={() =>
                          confirmMoney({
                            title: `Devolver ${b.descripcion || 'bien'}`,
                            amount: b.valorTasacion,
                            officeLabel: `Crédito #${creditoId}`,
                            consequence: creditoDesembolsado
                              ? 'Cierre el crédito antes de devolver.'
                              : 'El bien sale de custodia. Esta acción no se deshace.',
                            okText: 'Devolver',
                            onOk: async () => {
                              await devolverBien.mutateAsync(b.prendaId)
                            },
                          })
                        }
                      >
                        Devolver
                      </Button>
                    ) : null}
                    {estado === 'DEVUELTO' ? (
                      <Button
                        size="small"
                        icon={<FilePdfOutlined />}
                        loading={imprimirDevolucion.isPending}
                        onClick={() => imprimirDevolucion.mutate(b.prendaId)}
                      >
                        Comprobante
                      </Button>
                    ) : null}
                  </div>
                )
              })}
              {creditoDesembolsado ? (
                <Text type="secondary" style={{ fontSize: 12 }}>
                  La devolución se habilita cuando el crédito ya no está desembolsado.
                </Text>
              ) : null}
            </div>
          ) : null}

          {puedeEditarBienes ? (
            <Button
              type="primary"
              style={{ marginTop: 12 }}
              loading={guardarBienes.isPending}
              onClick={() => guardarBienes.mutate()}
            >
              Guardar bienes
            </Button>
          ) : null}
        </CredixPanel>
      ) : null}

      <Modal
        title={`Prorrogar crédito ${creditoId || ''}`}
        open={modalProrrogar}
        onCancel={() => setModalProrrogar(false)}
        onOk={() => {
          if (diasProrroga < 1) {
            message.warning('Indique al menos 1 día')
            return
          }
          prorrogar.mutate()
        }}
        confirmLoading={prorrogar.isPending}
        okText="Prorrogar"
      >
        <label htmlFor="dias-prorroga-gestion">Días</label>
        <InputNumber
          id="dias-prorroga-gestion"
          min={1}
          value={diasProrroga}
          onChange={(v) => setDiasProrroga(v ?? 7)}
          style={{ width: '100%', marginTop: 8 }}
        />
      </Modal>
    </CredixPage>
  )
}
