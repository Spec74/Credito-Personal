import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalculatorOutlined, FilePdfOutlined, PlusOutlined, UploadOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Alert, Button, Input, InputNumber, Modal, Select, Space, Tooltip, Typography, Upload, message } from 'antd'
import {
  fetchCreditoContexto,
  fetchCreditosGrillaPersona,
  fetchPersonaCreditoFicha,
  fetchPrendas,
  guardarPrendas,
  type PrendaItem,
} from '../../api/creditoGestion'
import { prorrogarCredito } from '../../api/creditoPlanes'
import { fetchCajaDiarioSesion } from '../../api/cajaDiario'
import { fetchValoresTabla } from '../../api/maestros'
import { TipoPagoChips } from '../../components/caja/TipoPagoChips'
import { FechaHoraDigitalField, nowDatetimeLocal } from '../caja/components/FechaHoraDigitalField'
import {
  abonarPrendario,
  actualizarFichaPrenda,
  devolverPrenda,
  downloadActaEntregaPrendarioPdf,
  downloadContratoPrendarioPdf,
  downloadDevolucionPrendarioPdf,
  fetchPrendarioCobroPreview,
  liberarPrendario,
  rematarPrenda,
  renovarPrendario,
  subirFotoPrenda,
} from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { confirmMoney, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { PrendaFotoThumb } from '../../components/credito/PrendaFotoThumb'
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
  const [modalCobro, setModalCobro] = useState<'renovar' | 'abonar' | 'liberar' | null>(null)
  const [montoAbono, setMontoAbono] = useState(0)
  const [tipoPagoId, setTipoPagoId] = useState(1)
  const [fechaTransferencia, setFechaTransferencia] = useState('')
  const [obsFicha, setObsFicha] = useState<Record<number, string>>({})
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
    const next: Record<number, string> = {}
    for (const b of guardadas) {
      next[b.prendaId] = b.observaciones ?? ''
    }
    setObsFicha(next)
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

  const caja = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0,
  })

  const tiposPago = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    staleTime: 5 * 60_000,
  })

  const tipoPagoOptions = useMemo(
    () => (tiposPago.data ?? []).map((t) => ({ value: t.itemId, label: t.denominacion })),
    [tiposPago.data],
  )

  useEffect(() => {
    if (tipoPagoId > 1) {
      setFechaTransferencia((prev) => prev.trim() || nowDatetimeLocal())
    }
  }, [tipoPagoId])

  const preview = useQuery({
    queryKey: ['prendario-cobro-preview', oficinaId, creditoId],
    queryFn: () => fetchPrendarioCobroPreview(oficinaId, creditoId),
    enabled: oficinaId > 0 && creditoId > 0,
  })

  const invalidarPrendario = () => {
    void queryClient.invalidateQueries({ queryKey: ['prendas', oficinaId, creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['credito-contexto', creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['prendario-creditos'] })
    void queryClient.invalidateQueries({ queryKey: ['prendario-resumen'] })
    void queryClient.invalidateQueries({ queryKey: ['prendario-cobro-preview', oficinaId, creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['prenda-foto'] })
  }

  const guardarFicha = useMutation({
    mutationFn: (prendaId: number) =>
      actualizarFichaPrenda({
        oficinaId,
        prendaId,
        observaciones: obsFicha[prendaId] ?? '',
      }),
    onSuccess: () => {
      message.success('Ficha actualizada')
      invalidarPrendario()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const subirFoto = useMutation({
    mutationFn: ({ prendaId, file }: { prendaId: number; file: File }) =>
      subirFotoPrenda(oficinaId, prendaId, file),
    onSuccess: () => {
      message.success('Foto guardada')
      invalidarPrendario()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const renovarMut = useMutation({
    mutationFn: () => {
      const cajaId = caja.data?.cajaDiarioId ?? 0
      if (cajaId < 1) throw new Error('Abra caja diario en esta sede para cobrar.')
      return renovarPrendario({
        oficinaId,
        cajaDiarioId: cajaId,
        creditoId,
        dias: preview.data?.diasRenovar ?? 30,
        tipoPagoId,
        fechaPagoTransferencia: tipoPagoId > 1 ? fechaTransferencia : undefined,
      })
    },
    onSuccess: () => {
      setModalCobro(null)
      message.success('Interés cobrado y vencimiento prorrogado')
      invalidarPrendario()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const abonarMut = useMutation({
    mutationFn: () => {
      const cajaId = caja.data?.cajaDiarioId ?? 0
      if (cajaId < 1) throw new Error('Abra caja diario en esta sede para cobrar.')
      if (montoAbono <= 0) throw new Error('Indique un monto a abonar')
      return abonarPrendario({
        oficinaId,
        cajaDiarioId: cajaId,
        creditoId,
        importeRecibido: montoAbono,
        tipoPagoId,
        fechaPagoTransferencia: tipoPagoId > 1 ? fechaTransferencia : undefined,
      })
    },
    onSuccess: () => {
      setModalCobro(null)
      message.success('Abono registrado')
      invalidarPrendario()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const liberarMut = useMutation({
    mutationFn: () => {
      const cajaId = caja.data?.cajaDiarioId ?? 0
      if (cajaId < 1) throw new Error('Abra caja diario en esta sede para cobrar.')
      return liberarPrendario({
        oficinaId,
        cajaDiarioId: cajaId,
        creditoId,
        tipoPagoId,
        fechaPagoTransferencia: tipoPagoId > 1 ? fechaTransferencia : undefined,
      })
    },
    onSuccess: () => {
      setModalCobro(null)
      message.success('Préstamo liberado. La prenda vuelve a su dueño.')
      invalidarPrendario()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const rematarMut = useMutation({
    mutationFn: (prendaId: number) => rematarPrenda({ oficinaId, prendaId }),
    onSuccess: (res) => {
      message.success(`Rematado. Venda ${res.codArticulo} en venta rápida a ${formatMoney(res.precioVenta)}.`)
      invalidarPrendario()
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
      className="credito-prendario-gestion-page"
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
          className="credito-prendario-alert"
          message="Seleccione el crédito prendario a gestionar"
          description={
            <Select
              className="credito-prendario-select"
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
              {creditoDesembolsado && !preview.data?.fechaRemateEjecutado ? (
                <>
                  <Button
                    onClick={() => {
                      setTipoPagoId(1)
                      setFechaTransferencia('')
                      setModalCobro('renovar')
                    }}
                  >
                    Renovar interés
                  </Button>
                  <Button
                    onClick={() => {
                      setTipoPagoId(1)
                      setFechaTransferencia('')
                      setMontoAbono(0)
                      setModalCobro('abonar')
                    }}
                  >
                    Abonar
                  </Button>
                  <Button
                    onClick={() => {
                      setTipoPagoId(1)
                      setFechaTransferencia('')
                      setModalCobro('liberar')
                    }}
                  >
                    Liberar
                  </Button>
                </>
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
              className="credito-prendario-select credito-prendario-alert"
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

          <Paragraph type="secondary" className="credito-prendario-estado">
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
              className="credito-prendario-alert"
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
              className="credito-prendario-alert"
              message="Complete el simulador para asignar contrato e imprimir documentos."
            />
          ) : null}

          {puedeEditarBienes ? (
            <div className="credito-prendario-fecha-remate">
              <Text type="secondary" className="credito-prendario-fecha-remate__label">
                Fecha remate (opcional)
              </Text>
              <Input
                type="date"
                value={fechaRematePrenda}
                onChange={(e) => setFechaRematePrenda(e.target.value)}
              />
              <Text type="secondary" className="credito-prendario-fecha-remate__hint">
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
            <div className="credito-prendario-fichas">
              {(bienes.data ?? []).map((b) => {
                const estado = (b.estado ?? 'EN CUSTODIA').trim().toUpperCase()
                const enCustodia = estado === 'EN CUSTODIA'
                return (
                  <article key={b.prendaId} className="credito-prendario-ficha">
                    <div className="credito-prendario-ficha__meta">
                      <Text>
                        {b.descripcion || 'Bien'} · {estado}
                      </Text>
                    </div>
                    {b.prendaId > 0 ? (
                      <PrendaFotoThumb
                        prendaId={b.prendaId}
                        oficinaId={oficinaId}
                        hasFoto={Boolean(b.fotoPath)}
                      />
                    ) : null}
                    <div className="credito-prendario-ficha__cuerpo">
                      {b.prendaId > 0 ? (
                        <Input.TextArea
                          rows={2}
                          maxLength={500}
                          placeholder="Cómo llegó (ej. Bueno, con rayones)"
                          value={obsFicha[b.prendaId] ?? ''}
                          onChange={(e) =>
                            setObsFicha((prev) => ({ ...prev, [b.prendaId]: e.target.value }))
                          }
                        />
                      ) : null}
                      <div className="credito-prendario-ficha__acciones">
                        {b.prendaId > 0 ? (
                          <>
                            <Button
                              loading={guardarFicha.isPending}
                              onClick={() => guardarFicha.mutate(b.prendaId)}
                            >
                              Guardar ficha
                            </Button>
                            <Upload
                              accept="image/jpeg,image/png,image/webp"
                              showUploadList={false}
                              beforeUpload={(file) => {
                                void subirFoto.mutateAsync({ prendaId: b.prendaId, file })
                                return false
                              }}
                            >
                              <Button icon={<UploadOutlined />} loading={subirFoto.isPending}>
                                Foto
                              </Button>
                            </Upload>
                          </>
                        ) : null}
                        {enCustodia && preview.data?.puedeRematar ? (
                          <Button
                            danger
                            loading={rematarMut.isPending}
                            onClick={() =>
                              confirmMoney({
                                title: `Rematar ${b.descripcion || 'bien'}`,
                                amount: b.valorTasacion,
                                officeLabel: `Crédito #${creditoId}`,
                                consequence:
                                  'La prenda pasa a stock a su tasación para venderla en el punto de venta. El préstamo queda cerrado como rematado.',
                                okText: 'Rematar',
                                onOk: async () => {
                                  await rematarMut.mutateAsync(b.prendaId)
                                },
                              })
                            }
                          >
                            Rematar
                          </Button>
                        ) : null}
                        {enCustodia ? (
                          <Button
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
                            icon={<FilePdfOutlined />}
                            loading={imprimirDevolucion.isPending}
                            onClick={() => imprimirDevolucion.mutate(b.prendaId)}
                          >
                            Comprobante
                          </Button>
                        ) : null}
                      </div>
                    </div>
                  </article>
                )
              })}
              {creditoDesembolsado ? (
                <Text type="secondary" className="credito-prendario-ficha__hint">
                  La devolución se habilita cuando el crédito ya no está desembolsado.
                </Text>
              ) : null}
            </div>
          ) : null}

          {puedeEditarBienes ? (
            <Button
              type="primary"
              className="credito-prendario-guardar"
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
        wrapClassName="credix-modal-operativo"
        width="min(480px, 96vw)"
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
          className="credito-prendario-campo"
          min={1}
          value={diasProrroga}
          onChange={(v) => setDiasProrroga(v ?? 7)}
        />
      </Modal>

      <Modal
        title={
          modalCobro === 'renovar'
            ? `Renovar · crédito #${creditoId}`
            : modalCobro === 'abonar'
              ? `Abonar al capital · crédito #${creditoId}`
              : `Liberar la prenda · crédito #${creditoId}`
        }
        open={modalCobro != null}
        onCancel={() => setModalCobro(null)}
        wrapClassName="credix-modal-operativo"
        width="min(520px, 96vw)"
        confirmLoading={renovarMut.isPending || abonarMut.isPending || liberarMut.isPending}
        okButtonProps={{
          disabled:
            !caja.data?.cajaDiarioId ||
            (modalCobro === 'abonar' && montoAbono <= 0) ||
            (tipoPagoId > 1 && !fechaTransferencia.trim()),
        }}
        okText={
          modalCobro === 'renovar'
            ? `Cobrar ${formatMoney(preview.data?.interesPendiente ?? 0)}`
            : modalCobro === 'abonar'
              ? `Cobrar ${formatMoney(montoAbono)}`
              : `Cobrar ${formatMoney(preview.data?.saldoAprox ?? 0)}`
        }
        onOk={() => {
          if (!caja.data?.cajaDiarioId) {
            message.warning('Abra caja diario en esta sede')
            return
          }
          if (tipoPagoId > 1 && !fechaTransferencia.trim()) {
            message.warning('Indique fecha y n.° / hora de la operación')
            return
          }
          if (modalCobro === 'renovar') renovarMut.mutate()
          if (modalCobro === 'abonar') abonarMut.mutate()
          if (modalCobro === 'liberar') {
            void confirmMoney({
              title: `Liberar crédito #${creditoId}`,
              amount: preview.data?.saldoAprox ?? 0,
              officeLabel: 'Pago total',
              consequence: 'El préstamo queda liberado y la prenda vuelve a su dueño. Esta acción no se deshace.',
              okText: 'Liberar',
              onOk: async () => {
                await liberarMut.mutateAsync()
              },
            })
          }
        }}
      >
        {!caja.data?.cajaDiarioId ? (
          <Alert
            type="warning"
            showIcon
            className="credito-prendario-alert"
            message="No hay caja abierto en esta sede."
          />
        ) : null}
        {modalCobro === 'renovar' ? (
          <Paragraph>
            Interés del periodo (del plan, no un 5 % inventado):{' '}
            <Text strong>{formatMoney(preview.data?.interesPendiente ?? 0)}</Text>
            <br />
            Nuevo vencimiento: {formatFecha(preview.data?.nuevoVencimiento)}
          </Paragraph>
        ) : null}
        {modalCobro === 'abonar' ? (
          <>
            <Paragraph>
              Capital pendiente: <Text strong>{formatMoney(preview.data?.capitalPendiente ?? 0)}</Text>
              <br />
              Capital que queda:{' '}
              <Text strong>
                {formatMoney(Math.max(0, (preview.data?.capitalPendiente ?? 0) - montoAbono))}
              </Text>
            </Paragraph>
            <label htmlFor="monto-abono-prendario">Monto a abonar (S/)</label>
            <InputNumber
              id="monto-abono-prendario"
              className="credito-prendario-campo"
              min={0}
              max={preview.data?.capitalPendiente ?? undefined}
              value={montoAbono}
              onChange={(v) => setMontoAbono(Number(v ?? 0))}
            />
          </>
        ) : null}
        {modalCobro === 'liberar' ? (
          <Paragraph>
            Capital {formatMoney(preview.data?.capitalPendiente ?? 0)} + interés{' '}
            {formatMoney(preview.data?.interesPendiente ?? 0)}. A cobrar{' '}
            <Text strong>{formatMoney(preview.data?.saldoAprox ?? 0)}</Text>
            <br />
            Al confirmar, el préstamo queda liberado y la prenda vuelve a su dueño.
          </Paragraph>
        ) : null}
        <div className="credito-prendario-cobro__medio">
          <Text type="secondary" className="credito-prendario-cobro__label">
            Medio de pago
          </Text>
          <TipoPagoChips value={tipoPagoId} options={tipoPagoOptions} onChange={setTipoPagoId} wrap />
          {tipoPagoId > 1 ? (
            <div className="credito-prendario-cobro__fecha">
              <FechaHoraDigitalField value={fechaTransferencia} onChange={setFechaTransferencia} />
            </div>
          ) : null}
        </div>
      </Modal>
    </CredixPage>
  )
}
