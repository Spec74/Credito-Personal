import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Badge,
  Button,
  Checkbox,
  Grid,
  Input,
  Modal,
  Select,
  Space,
  Spin,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import {
  ArrowLeftOutlined,
  CheckOutlined,
  CloseOutlined,
  EnvironmentOutlined,
  SearchOutlined,
  ArrowUpOutlined,
  WhatsAppOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  cobrarPlanillaBloque,
  enrichCreditosGestorContactos,
  fetchCajaDiarioSesion,
  fetchCreditosGestorDesembolsados,
  pagarCuotaImporteLibre,
  type CreditoGestorPendienteRow,
} from '../../api/cajaDiario'
import { fetchEstadoPlanPago } from '../../api/creditoPlanes'
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, CredixPanel } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import { formatFecha, toLegacyFechaTransferencia } from '../../utils/formatFecha'
import { clearCobroBloqueEjecutadoHoy } from '../../utils/cobroBloqueDayLock'
import {
  clearCobroColaError,
  isCreditoEnviadoHoy,
  loadCobroCola,
  markCobroEnviados,
  setCobroColaError,
  type CobroColaDia,
  type CobroEnviadoItem,
} from '../../utils/cobroCola'
import {
  clearCobroBloqueDraft,
  draftHasMontos,
  fechaOperacionLocal,
  formatDraftAge,
  loadCobroBloqueDraft,
  saveCobroBloqueDraft,
  type CobroBloqueRowDraft,
} from '../../utils/cobroBloqueDraft'
import {
  clearCobroBloquePendingProcess,
  enqueueCobroBloqueProcess,
  isBrowserOnline,
  isLikelyNetworkError,
  findCobroBloqueOfflineSession,
  loadCobroBloqueCarteraCache,
  loadCobroBloquePendingProcess,
  saveCobroBloqueCarteraCache,
} from '../../utils/cobroBloqueOffline'
import type { EstadoPlanPagoCuota } from '../../types/api'
import type { CajaSession } from './cajaDiario/types'
import { maybeDownloadCajaTicket } from './cajaDiario/cajaPagoHelpers'
import {
  FechaHoraDigitalField,
  nowDatetimeLocal,
} from './components/FechaHoraDigitalField'
import { CobroBloqueFieldCard } from './components/CobroBloqueFieldCard'
import { MontoCobrarInput } from './components/MontoCobrarInput'
import {
  clienteMapaHref,
  clienteMapaLabel,
  clienteMapaTitle,
  clienteTieneGps,
} from '../../utils/clienteMapaNavegacion'
import '../../styles/cobro-bloque.css'

type VistaCampo = 'todos' | 'mora' | 'cobro'
type TabPrincipal = 'cartera' | 'por-enviar' | 'enviados'

const { Text } = Typography

/** Paridad legacy `tiposPagoDigital = [2, 3, 4, 5]`. */
const TIPOS_PAGO_DIGITAL = new Set([2, 3, 4, 5])

type RowEdit = CobroBloqueRowDraft

function esCuotaPendiente(c: EstadoPlanPagoCuota): boolean {
  const e = (c.estado ?? '').trim().toUpperCase()
  return e !== 'PAG' && e !== 'CAN'
}

function montoCuotaPendiente(c: EstadoPlanPagoCuota): number {
  const cuota = Number(c.cuota) || 0
  const cargo = Number(c.cargo) || 0
  const descuento = Number(c.descuento) || 0
  const pagoLibre = Number(c.pagoLibre) || 0
  const mora = Number(c.importeMora) || 0
  return Math.max(0, cuota + cargo - descuento - pagoLibre + mora)
}

function defaultEdit(): RowEdit {
  return {
    montoPagar: 0,
    tipoPagoId: 1,
    fechaHoraTrans: nowDatetimeLocal(),
    cuotasSeleccionadas: [],
  }
}

function CuotasSubgrid({
  creditoId,
  deudaMax,
  selected,
  onChange,
}: {
  creditoId: number
  deudaMax: number
  selected: number[]
  onChange: (planPagoIds: number[], monto: number) => void
}) {
  const cuotasQuery = useQuery({
    queryKey: ['cobro-bloque-cuotas', creditoId],
    queryFn: () => fetchEstadoPlanPago(creditoId),
  })

  const pendientes = useMemo(
    () => (cuotasQuery.data ?? []).filter(esCuotaPendiente),
    [cuotasQuery.data],
  )

  if (cuotasQuery.isLoading) {
    return (
      <div className="cobro-bloque-subgrid cobro-bloque-subgrid--loading">
        <Spin size="small" /> <Text type="secondary">Cargando cuotas…</Text>
      </div>
    )
  }

  if (cuotasQuery.isError) {
    return (
      <Alert
        type="warning"
        showIcon
        message="No se pudieron cargar las cuotas"
        description="Puede digitar el monto libre en la fila principal."
      />
    )
  }

  if (pendientes.length === 0) {
    return (
      <Text type="secondary" className="cobro-bloque-subgrid">
        Sin cuotas pendientes seleccionables.
      </Text>
    )
  }

  return (
    <div className="cobro-bloque-subgrid">
      <Text type="secondary" className="cobro-bloque-subgrid__hint">
        Marque cuotas para sumar el monto (o digite libre). Máx. S/ {formatMoney(deudaMax)}.
      </Text>
      <div className="cobro-bloque-subgrid__list">
        {pendientes.map((c) => {
          const monto = montoCuotaPendiente(c)
          const checked = selected.includes(c.planPagoId)
          return (
            <label key={c.planPagoId} className="cobro-bloque-subgrid__item">
              <Checkbox
                checked={checked}
                onChange={(ev) => {
                  const next = ev.target.checked
                    ? [...selected, c.planPagoId]
                    : selected.filter((id) => id !== c.planPagoId)
                  const suma = pendientes
                    .filter((p) => next.includes(p.planPagoId))
                    .reduce((acc, p) => acc + montoCuotaPendiente(p), 0)
                  onChange(next, Math.min(suma, deudaMax))
                }}
              />
              <span>
                Cuota {c.numero} · venc. {formatFecha(c.fechaVencimiento)} · S/{' '}
                {formatMoney(monto)}
                {(c.diasAtrazo ?? 0) > 0 ? ` · ${c.diasAtrazo}d atraso` : ''}
              </span>
            </label>
          )
        })}
      </div>
    </div>
  )
}

export function CobroBloquePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const screens = Grid.useBreakpoint()
  const fieldMode = screens.md !== true
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const usuarioId = session?.usuarioId ?? 0
  const [filtro, setFiltro] = useState('')
  const [edits, setEdits] = useState<Record<number, RowEdit>>({})
  const [draftBanner, setDraftBanner] = useState<string | null>(null)
  const [online, setOnline] = useState(() => isBrowserOnline())
  const [usingCache, setUsingCache] = useState(false)
  const [pendingSync, setPendingSync] = useState(false)
  const [syncingQueue, setSyncingQueue] = useState(false)
  const draftHydrated = useRef(false)
  const syncingRef = useRef(false)
  const [scrolledDown, setScrolledDown] = useState(false)
  const [dockBox, setDockBox] = useState<{ left: number; width: number } | null>(null)
  const [vistaCampo, setVistaCampo] = useState<VistaCampo>('todos')
  const [tabPrincipal, setTabPrincipal] = useState<TabPrincipal>('cartera')
  const [cola, setCola] = useState<CobroColaDia>(() =>
    loadCobroCola({ usuarioId: 0, oficinaId: 0, cajaDiarioId: 0 }),
  )
  const [cobrarYaId, setCobrarYaId] = useState<number | null>(null)
  const carteraRowsRef = useRef<CreditoGestorPendienteRow[]>([])

  // Day-lock legacy deprecado: limpiar cualquier marca previa y no bloquear nunca.
  useEffect(() => {
    clearCobroBloqueEjecutadoHoy()
  }, [])

  useEffect(() => {
    const on = () => setOnline(true)
    const off = () => setOnline(false)
    window.addEventListener('online', on)
    window.addEventListener('offline', off)
    return () => {
      window.removeEventListener('online', on)
      window.removeEventListener('offline', off)
    }
  }, [])

  useEffect(() => {
    const scroller = document.querySelector('.credix-content')
    if (!scroller) return
    const onScroll = () => setScrolledDown(scroller.scrollTop > 240)
    onScroll()
    scroller.addEventListener('scroll', onScroll, { passive: true })
    return () => scroller.removeEventListener('scroll', onScroll)
  }, [])

  useEffect(() => {
    const main = document.querySelector('.credix-main')
    if (!main) return
    const sync = () => {
      const r = main.getBoundingClientRect()
      setDockBox({ left: Math.max(0, r.left), width: Math.max(280, r.width) })
    }
    sync()
    const ro = new ResizeObserver(sync)
    ro.observe(main)
    window.addEventListener('resize', sync)
    return () => {
      ro.disconnect()
      window.removeEventListener('resize', sync)
    }
  }, [])

  const scrollPlanillaTop = () => {
    const scroller = document.querySelector('.credix-content')
    scroller?.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const sesionQuery = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const caja = sesionQuery.data
  const ctxOnline: CajaSession | null = caja
    ? {
        oficinaId,
        cajaDiarioId: caja.cajaDiarioId,
        cajaId: caja.cajaId,
        cajaDenominacion: caja.cajaDenominacion,
        fechaIniOperacion: caja.fechaIniOperacion,
        saldoInicial: caja.saldoInicial,
        entradas: caja.entradas,
        salidas: caja.salidas,
        saldoFinal: caja.saldoFinal,
        indCierre: caja.indCierre,
        esCajaCentral: caja.esCajaCentral,
      }
    : null
  /** Solo usar snap local si no hay red o la sesión falló por red (nunca si el API dijo “sin caja”). */
  const allowOfflineSession =
    !online ||
    (sesionQuery.isError && isLikelyNetworkError(sesionQuery.error))

  const resolveOfflineSession = useCallback((): CajaSession | null => {
    if (!allowOfflineSession || usuarioId < 1) return null
    return findCobroBloqueOfflineSession(usuarioId)
  }, [allowOfflineSession, usuarioId])

  const ctxOffline = resolveOfflineSession()
  const ctx = ctxOnline ?? ctxOffline
  const cajaDiarioId = ctx?.cajaDiarioId ?? 0
  const offlineSesion = Boolean(!ctxOnline && ctxOffline)

  const colaArgs = useMemo(
    () => ({ usuarioId, oficinaId, cajaDiarioId }),
    [usuarioId, oficinaId, cajaDiarioId],
  )

  useEffect(() => {
    setCola(loadCobroCola({ usuarioId, oficinaId, cajaDiarioId }))
  }, [usuarioId, oficinaId, cajaDiarioId])

  const enviadosSet = useMemo(
    () => new Set(cola.enviados.map((e) => e.creditoId)),
    [cola.enviados],
  )

  useEffect(() => {
    if (usuarioId < 1 || cajaDiarioId < 1) {
      setPendingSync(false)
      return
    }
    setPendingSync(
      loadCobroBloquePendingProcess({ usuarioId, cajaDiarioId }) != null,
    )
  }, [usuarioId, cajaDiarioId, online])

  const carteraQueryKey = useMemo(
    () =>
      [
        'caja-creditos-gestor-des',
        'cobro-bloque',
        cajaDiarioId,
        oficinaId,
        usuarioId,
      ] as const,
    [cajaDiarioId, oficinaId, usuarioId],
  )

  const carteraQuery = useQuery({
    queryKey: carteraQueryKey,
    queryFn: async () => {
      try {
        const rows = await fetchCreditosGestorDesembolsados({
          oficinaId,
          usuarioId,
        })
        return { rows, fromCache: false as const }
      } catch (e) {
        if (!isLikelyNetworkError(e)) throw e
        const cache = loadCobroBloqueCarteraCache({ usuarioId, cajaDiarioId })
        if (cache?.rows?.length) {
          const { normalizeCreditoGestorPendienteRow } = await import(
            '../../utils/creditoGestorPendiente'
          )
          return {
            rows: cache.rows.map((r) =>
              normalizeCreditoGestorPendienteRow(r as unknown as Record<string, unknown>),
            ),
            fromCache: true as const,
          }
        }
        throw e
      }
    },
    enabled: !!ctx && !ctx.indCierre && usuarioId > 0 && oficinaId > 0,
    staleTime: 60_000,
  })

  // Contactos: refuerzo en segundo plano (no bloquea la primera pintura de filas).
  useEffect(() => {
    const data = carteraQuery.data
    if (!data || data.fromCache || !online) return
    if (!data.rows.some((r) => !r.celular || !r.direccion)) return
    let cancelled = false
    void enrichCreditosGestorContactos(data.rows, { oficinaId, usuarioId }).then(
      (enriched) => {
        if (cancelled) return
        if (enriched === data.rows) return
        queryClient.setQueryData(carteraQueryKey, {
          rows: enriched,
          fromCache: false as const,
        })
      },
    )
    return () => {
      cancelled = true
    }
  }, [carteraQuery.data, carteraQueryKey, oficinaId, usuarioId, online, queryClient])

  useEffect(() => {
    const data = carteraQuery.data
    if (!data || !ctx || data.fromCache) {
      if (data?.fromCache) setUsingCache(true)
      return
    }
    setUsingCache(false)
    saveCobroBloqueCarteraCache({
      version: 1,
      usuarioId,
      oficinaId,
      cajaDiarioId: ctx.cajaDiarioId,
      fechaOperacion: fechaOperacionLocal(),
      cachedAt: new Date().toISOString(),
      rows: data.rows,
      session: {
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        cajaId: ctx.cajaId,
        cajaDenominacion: ctx.cajaDenominacion,
        fechaIniOperacion: ctx.fechaIniOperacion,
        saldoInicial: ctx.saldoInicial,
        entradas: ctx.entradas,
        salidas: ctx.salidas,
        saldoFinal: ctx.saldoFinal,
        indCierre: ctx.indCierre,
        esCajaCentral: ctx.esCajaCentral,
      },
    })
  }, [carteraQuery.data, ctx, usuarioId, oficinaId])

  const carteraRows = carteraQuery.data?.rows
  carteraRowsRef.current = carteraRows ?? []

  const tiposPagoQuery = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    staleTime: 5 * 60_000,
  })

  const tipoPagoOptions = useMemo(
    () =>
      (tiposPagoQuery.data ?? []).map((t) => ({
        value: t.itemId,
        label: t.denominacion,
      })),
    [tiposPagoQuery.data],
  )

  // Recuperar borrador al abrir sesión de caja (solo una vez).
  useEffect(() => {
    if (draftHydrated.current || usuarioId < 1 || cajaDiarioId < 1) return
    draftHydrated.current = true
    const { draft, discardedStale } = loadCobroBloqueDraft({
      usuarioId,
      oficinaId,
      cajaDiarioId,
    })
    if (discardedStale) {
      message.info(
        'Se descartó un borrador antiguo (otro día o caja). No se registró nada en el servidor.',
      )
    }
    const next: Record<number, RowEdit> = {}
    const colaHoy = loadCobroCola({ usuarioId, oficinaId, cajaDiarioId })
    let hayDraft = false
    if (draft && draftHasMontos(draft)) {
      for (const [k, v] of Object.entries(draft.edits)) {
        const id = Number(k)
        if (Number.isFinite(id) && v && !isCreditoEnviadoHoy(colaHoy, id)) next[id] = v
      }
      if (draft.filtro) setFiltro(draft.filtro)
      hayDraft = true
    }
    // Migración: cola offline legacy («Procesar planilla») → restaurar en Por enviar.
    let restauradosPending = 0
    const pending = loadCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
    if (pending) {
      for (const item of pending.planilla) {
        if (item.montoPagar <= 0 || next[item.creditoId]) continue
        if (isCreditoEnviadoHoy(colaHoy, item.creditoId)) continue
        next[item.creditoId] = {
          montoPagar: item.montoPagar,
          tipoPagoId: item.tipoPagoId,
          fechaHoraTrans: item.fechaHoraTrans?.trim() || nowDatetimeLocal(),
          cuotasSeleccionadas: [],
        }
        restauradosPending++
      }
      setPendingSync(true)
    }
    if (Object.keys(next).length > 0) {
      setEdits((prev) => ({ ...next, ...prev }))
    }
    if (hayDraft && draft) {
      setDraftBanner(
        `Borrador recuperado (${formatDraftAge(draft.updatedAt)}). Puede continuar donde lo dejó.`,
      )
    } else if (restauradosPending > 0) {
      setDraftBanner(
        `Se restauraron ${restauradosPending} cobro(s) pendientes de envío en Por enviar.`,
      )
    }
  }, [usuarioId, oficinaId, cajaDiarioId])

  // Autoguardado al navegar a otro módulo / minimizar.
  useEffect(() => {
    if (usuarioId < 1 || cajaDiarioId < 1) return
    const persist = () => {
      const editsMap: Record<string, RowEdit> = {}
      for (const [k, v] of Object.entries(edits)) {
        if ((v.montoPagar ?? 0) > 0 || (v.cuotasSeleccionadas?.length ?? 0) > 0 || v.tipoPagoId !== 1) {
          editsMap[k] = v
        }
      }
      if (Object.keys(editsMap).length === 0 && !filtro.trim()) {
        clearCobroBloqueDraft({ usuarioId, cajaDiarioId })
        return
      }
      saveCobroBloqueDraft({
        version: 1,
        usuarioId,
        oficinaId,
        cajaDiarioId,
        fechaOperacion: fechaOperacionLocal(),
        updatedAt: new Date().toISOString(),
        filtro,
        edits: editsMap,
      })
    }
    const t = window.setTimeout(persist, 400)
    const onVis = () => {
      if (document.visibilityState === 'hidden') persist()
    }
    document.addEventListener('visibilitychange', onVis)
    window.addEventListener('pagehide', persist)
    return () => {
      window.clearTimeout(t)
      persist()
      document.removeEventListener('visibilitychange', onVis)
      window.removeEventListener('pagehide', persist)
    }
  }, [edits, filtro, usuarioId, oficinaId, cajaDiarioId])

  const getEdit = useCallback(
    (row: CreditoGestorPendienteRow): RowEdit => edits[row.creditoId] ?? defaultEdit(),
    [edits],
  )

  const patchEdit = (
    creditoId: number,
    patch: Partial<RowEdit>,
    row: CreditoGestorPendienteRow,
  ) => {
    if (enviadosSet.has(creditoId) && (patch.montoPagar ?? 0) > 0) {
      message.info('Este crédito ya fue enviado hoy. Revise la pestaña Enviados.')
      return
    }
    setDraftBanner(null)
    if (cola.errores[String(creditoId)]) {
      setCola(clearCobroColaError(colaArgs, creditoId))
    }
    setEdits((prev) => {
      const base = prev[creditoId] ?? defaultEdit()
      const next = { ...base, ...patch }
      if (next.montoPagar > row.deudaPendiente) next.montoPagar = row.deudaPendiente
      if (next.montoPagar < 0) next.montoPagar = 0
      // Pago digital: asegurar fecha/hora del voucher (por defecto ahora).
      if (
        TIPOS_PAGO_DIGITAL.has(next.tipoPagoId) &&
        !next.fechaHoraTrans?.trim()
      ) {
        next.fechaHoraTrans = nowDatetimeLocal()
      }
      return { ...prev, [creditoId]: next }
    })
  }

  /** Rellena monto: cuota parcial si aporta; si no, la deuda completa. */
  const aplicarMontoRapido = (row: CreditoGestorPendienteRow) => {
    const deuda = Math.max(0, row.deudaPendiente || 0)
    const sug = Math.max(0, row.cuotaSugerida || 0)
    const monto =
      sug > 0 && sug < deuda - 0.005
        ? Math.min(sug, deuda)
        : deuda
    if (monto <= 0) {
      message.info('No hay monto pendiente para este crédito.')
      return
    }
    patchEdit(row.creditoId, { montoPagar: monto, cuotasSeleccionadas: [] }, row)
  }

  /** Tipado (monto > 0) y aún no enviado. Tipar ≠ cobrado. */
  const esPorEnviar = useCallback(
    (row: CreditoGestorPendienteRow): boolean =>
      !enviadosSet.has(row.creditoId) && (edits[row.creditoId]?.montoPagar ?? 0) > 0,
    [edits, enviadosSet],
  )

  const filas = useMemo(() => {
    const raw = carteraRows ?? []
    const q = filtro.trim().toLowerCase()
    return raw.filter((r) => {
      if (q) {
        const hit =
          r.personaNombre.toLowerCase().includes(q) ||
          r.personaCodigo.toLowerCase().includes(q) ||
          (r.celular ?? '').toLowerCase().includes(q) ||
          (r.direccion ?? '').toLowerCase().includes(q) ||
          String(r.creditoId).includes(q)
        if (!hit) return false
      }
      if (tabPrincipal === 'por-enviar') return esPorEnviar(r)
      if (fieldMode && vistaCampo === 'mora' && r.diasAtrazo <= 0) return false
      if (fieldMode && vistaCampo === 'cobro' && !esPorEnviar(r)) return false
      return true
    })
  }, [carteraRows, filtro, fieldMode, vistaCampo, tabPrincipal, esPorEnviar])

  const enviadosFiltrados = useMemo(() => {
    const q = filtro.trim().toLowerCase()
    if (!q) return cola.enviados
    return cola.enviados.filter(
      (e) =>
        e.personaNombre.toLowerCase().includes(q) || String(e.creditoId).includes(q),
    )
  }, [cola.enviados, filtro])

  const enviadosTotal = useMemo(
    () => cola.enviados.reduce((a, e) => a + (e.montoPagar || 0), 0),
    [cola.enviados],
  )

  const resumen = useMemo(() => {
    let conCobro = 0
    let total = 0
    const porMetodo = new Map<number, { label: string; monto: number; n: number }>()
    for (const row of carteraRows ?? []) {
      if (!esPorEnviar(row)) continue
      const e = getEdit(row)
      conCobro++
      total += e.montoPagar
      const label =
        tipoPagoOptions.find((o) => o.value === e.tipoPagoId)?.label ?? `Tipo ${e.tipoPagoId}`
      const cur = porMetodo.get(e.tipoPagoId) ?? { label, monto: 0, n: 0 }
      cur.monto += e.montoPagar
      cur.n += 1
      porMetodo.set(e.tipoPagoId, cur)
    }
    const totalFilas = carteraRows?.length ?? 0
    return {
      conCobro,
      total,
      porMetodo: [...porMetodo.values()],
      totalFilas,
    }
  }, [carteraRows, esPorEnviar, getEdit, tipoPagoOptions])

  const validarPlanilla = (): string | null => {
    for (const row of carteraRows ?? []) {
      if (!esPorEnviar(row)) continue
      const e = getEdit(row)
      if (TIPOS_PAGO_DIGITAL.has(e.tipoPagoId) && !e.fechaHoraTrans?.trim()) {
        return `Ingrese la fecha/hora de transferencia para ${row.personaNombre} (crédito ${row.creditoId}).`
      }
    }
    return null
  }

  /** Solo tipados con monto > 0 que aún no fueron enviados. */
  const buildPlanillaPayload = () =>
    (carteraRows ?? [])
      .filter((row) => esPorEnviar(row))
      .map((row) => {
        const e = getEdit(row)
        return {
          creditoId: row.creditoId,
          montoPagar: e.montoPagar,
          tipoPagoId: e.tipoPagoId,
          fechaHoraTrans: e.fechaHoraTrans,
        }
      })
      .filter((x) => x.montoPagar > 0)

  type PlanillaItem = ReturnType<typeof buildPlanillaPayload>[number]

  /** Quita créditos de la cola offline legacy (evita doble cobro al sincronizar). */
  const quitarDePending = (creditoIds: number[]) => {
    if (usuarioId < 1 || cajaDiarioId < 1) return
    const pending = loadCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
    if (!pending) return
    const ids = new Set(creditoIds)
    const resto = pending.planilla.filter((p) => !ids.has(p.creditoId))
    if (resto.length === 0) {
      clearCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
      setPendingSync(false)
      return
    }
    if (resto.length === pending.planilla.length) return
    enqueueCobroBloqueProcess({
      ...pending,
      conCobro: resto.length,
      total: resto.reduce((a, x) => a + x.montoPagar, 0),
      planilla: resto,
    })
  }

  /** Registra enviados en cola local, limpia tipados y refresca cartera. Sin navegar ni day-lock. */
  const aplicarEnviados = (
    planilla: PlanillaItem[],
    origen: CobroEnviadoItem['origen'],
    extra?: Record<number, number | null | undefined>,
  ) => {
    if (planilla.length === 0) return
    const nombres = new Map(
      carteraRowsRef.current.map((r) => [r.creditoId, r.personaNombre] as const),
    )
    const nextCola = markCobroEnviados(
      { usuarioId, oficinaId, cajaDiarioId },
      planilla.map((p) => ({
        creditoId: p.creditoId,
        personaNombre: nombres.get(p.creditoId) ?? `Crédito ${p.creditoId}`,
        montoPagar: p.montoPagar,
        tipoPagoId: p.tipoPagoId,
        movimientoCajaId: extra?.[p.creditoId] ?? null,
        origen,
      })),
    )
    setCola(nextCola)
    const ids = planilla.map((p) => p.creditoId)
    setEdits((prev) => {
      const next = { ...prev }
      for (const id of ids) delete next[id]
      return next
    })
    quitarDePending(ids)
    void queryClient.invalidateQueries({ queryKey: carteraQueryKey })
  }

  const marcarErrorCola = (planilla: PlanillaItem[], mensaje: string) => {
    let last: CobroColaDia | null = null
    for (const p of planilla) {
      last = setCobroColaError({ usuarioId, oficinaId, cajaDiarioId }, p.creditoId, mensaje)
    }
    if (last) setCola(last)
  }

  const encolarPorOffline = (planilla: PlanillaItem[]) => {
    if (!ctx) return
    enqueueCobroBloqueProcess({
      version: 1,
      usuarioId,
      oficinaId,
      cajaDiarioId: ctx.cajaDiarioId,
      fechaOperacion: fechaOperacionLocal(),
      queuedAt: new Date().toISOString(),
      conCobro: planilla.length,
      total: planilla.reduce((a, x) => a + x.montoPagar, 0),
      planilla,
    })
    setPendingSync(true)
    message.warning(
      'Sin conexión: los cobros quedaron en Por enviar en este dispositivo. Se enviarán al recuperar red.',
    )
  }

  const enviarCola = useMutation({
    mutationFn: async () => {
      if (!ctx || ctx.indCierre) throw new Error('No hay caja diario abierta.')
      const err = validarPlanilla()
      if (err) throw new Error(err)

      const planilla = buildPlanillaPayload()
      if (planilla.length === 0) throw new Error('No hay cobros en Por enviar.')

      if (!isBrowserOnline()) {
        encolarPorOffline(planilla)
        return { queued: true as const }
      }

      try {
        const r = await cobrarPlanillaBloque({
          oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          planilla,
          completarImpagos: false,
        })
        return { queued: false as const, planilla, result: r }
      } catch (e) {
        if (isLikelyNetworkError(e)) {
          encolarPorOffline(planilla)
          return { queued: true as const }
        }
        marcarErrorCola(
          planilla,
          e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error al enviar',
        )
        throw e
      }
    },
    onSuccess: (r) => {
      if (r.queued) return
      aplicarEnviados(r.planilla, 'lote')
      message.success(r.result.mensaje || `${r.planilla.length} cobro(s) enviado(s).`)
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error'),
  })

  const cobrarYa = async (row: CreditoGestorPendienteRow) => {
    if (cobrarYaId != null) return
    if (!ctx || ctx.indCierre) {
      message.warning('No hay caja diario abierta.')
      return
    }
    if (enviadosSet.has(row.creditoId)) {
      message.info('Este crédito ya fue enviado hoy.')
      return
    }
    const e = getEdit(row)
    if (e.montoPagar <= 0) {
      message.warning('Digite un monto mayor a cero.')
      return
    }
    const digital = TIPOS_PAGO_DIGITAL.has(e.tipoPagoId)
    if (digital && !e.fechaHoraTrans?.trim()) {
      message.warning(
        `Ingrese la fecha/hora de transferencia para ${row.personaNombre} (crédito ${row.creditoId}).`,
      )
      return
    }
    if (!isBrowserOnline()) {
      message.info('Sin conexión: quedó en Por enviar. Se registrará al enviar la cola con red.')
      return
    }

    setCobrarYaId(row.creditoId)
    try {
      const r = await pagarCuotaImporteLibre({
        oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId: row.creditoId,
        importeRecibido: e.montoPagar,
        tipoPagoId: e.tipoPagoId,
        fechaPagoTransferencia: digital
          ? toLegacyFechaTransferencia(e.fechaHoraTrans) ?? null
          : null,
      })
      await maybeDownloadCajaTicket(oficinaId, r.resultId)
      aplicarEnviados(
        [
          {
            creditoId: row.creditoId,
            montoPagar: e.montoPagar,
            tipoPagoId: e.tipoPagoId,
            fechaHoraTrans: e.fechaHoraTrans,
          },
        ],
        'cobrar_ya',
        { [row.creditoId]: r.resultId },
      )
      message.success(`Cobro registrado: ${row.personaNombre}`)
    } catch (err) {
      if (isLikelyNetworkError(err)) {
        message.warning('Sin conexión estable: quedó en Por enviar.')
      } else {
        const msg =
          err instanceof ApiError ? err.message : err instanceof Error ? err.message : 'Error'
        setCola(setCobroColaError(colaArgs, row.creditoId, msg))
        message.error(msg)
      }
    } finally {
      setCobrarYaId(null)
    }
  }

  const quitarTipado = (row: CreditoGestorPendienteRow) => {
    patchEdit(row.creditoId, { montoPagar: 0, cuotasSeleccionadas: [] }, row)
  }

  // Sincronización offline: al recuperar red envía la cola pendiente (sin impagos ni day-lock).
  useEffect(() => {
    if (!online || usuarioId < 1 || cajaDiarioId < 1) return
    if (syncingRef.current) return
    const pending = loadCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
    if (!pending) return

    const colaHoy = loadCobroCola({ usuarioId, oficinaId, cajaDiarioId })
    const planilla = pending.planilla.filter(
      (p) => p.montoPagar > 0 && !isCreditoEnviadoHoy(colaHoy, p.creditoId),
    )
    if (planilla.length === 0) {
      clearCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
      setPendingSync(false)
      return
    }

    syncingRef.current = true
    setSyncingQueue(true)
    void (async () => {
      try {
        const r = await cobrarPlanillaBloque({
          oficinaId: pending.oficinaId,
          cajaDiarioId: pending.cajaDiarioId,
          planilla,
          completarImpagos: false,
        })
        // No cancelar el éxito: si el request ya fue a BD, hay que registrar enviados.
        aplicarEnviados(
          planilla.map((p) => ({
            creditoId: p.creditoId,
            montoPagar: p.montoPagar,
            tipoPagoId: p.tipoPagoId,
            fechaHoraTrans: p.fechaHoraTrans ?? '',
          })),
          'sync',
        )
        message.success(r.mensaje || 'Cobros sincronizados al recuperar red.')
      } catch (e) {
        if (isLikelyNetworkError(e)) {
          message.warning('Aún sin red estable. La cola se reintentará al conectar.')
        } else {
          const msg =
            e instanceof ApiError
              ? e.message
              : e instanceof Error
                ? e.message
                : 'No se pudo sincronizar la cola.'
          // Conservar tipados (siguen en Por enviar) y mostrar error por crédito.
          marcarErrorCola(
            planilla.map((p) => ({
              creditoId: p.creditoId,
              montoPagar: p.montoPagar,
              tipoPagoId: p.tipoPagoId,
              fechaHoraTrans: p.fechaHoraTrans ?? '',
            })),
            msg,
          )
          clearCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
          setPendingSync(false)
          message.error(msg)
        }
      } finally {
        syncingRef.current = false
        setSyncingQueue(false)
      }
    })()
    // Flush solo al recuperar red / cambiar caja.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [online, usuarioId, cajaDiarioId])

  const confirmarEnviarCola = () => {
    if (resumen.conCobro < 1) {
      message.warning('No hay cobros en Por enviar. Tipie montos o use Cobrar ya.')
      return
    }
    const err = validarPlanilla()
    if (err) {
      message.warning(err)
      return
    }

    Modal.confirm({
      title: 'Enviar cola',
      content: `¿Registrar ${resumen.conCobro} cobro(s) por S/ ${formatMoney(resumen.total)}? Pasarán a la pestaña Enviados.`,
      okText: 'Sí, enviar',
      cancelText: 'Cancelar',
      onOk: () => enviarCola.mutateAsync(),
    })
  }

  const descartarBorrador = () => {
    clearCobroBloqueDraft({ usuarioId, cajaDiarioId })
    setEdits({})
    setFiltro('')
    setDraftBanner(null)
    message.success('Borrador descartado (solo en este dispositivo).')
  }

  const columns: ColumnsType<CreditoGestorPendienteRow> = [
    {
      title: 'Cliente',
      dataIndex: 'personaNombre',
      className: 'cobro-bloque-col-cliente',
      onCell: () => ({ className: 'cobro-bloque-col-cliente' }),
      render: (_, row) => (
        <div className="cobro-bloque-cliente-cell">
          <strong className="cobro-bloque-cliente-cell__name" title={row.personaNombre}>
            {row.personaNombre}
          </strong>
          <span className="cobro-bloque-cliente-cell__meta">
            #{row.creditoId}
            {row.diasAtrazo > 0 ? ` · ${row.diasAtrazo}d atraso` : ''}
          </span>
        </div>
      ),
    },
    {
      title: 'Celular',
      dataIndex: 'celular',
      className: 'cobro-bloque-col-celular',
      onCell: () => ({ className: 'cobro-bloque-col-celular' }),
      render: (v: string | null) => {
        const digits = (v ?? '').replace(/\D/g, '')
        if (!digits) {
          return <Text type="secondary">—</Text>
        }
        const wa = digits.startsWith('51') ? digits : `51${digits}`
        return (
          <span className="cobro-bloque-celular">
            <a href={`tel:${digits}`} className="cobro-bloque-celular__tel">
              {digits}
            </a>
            <a
              className="cobro-bloque-celular__wa"
              href={`https://wa.me/${wa}`}
              target="_blank"
              rel="noreferrer"
              aria-label="WhatsApp"
              title="WhatsApp"
            >
              <WhatsAppOutlined />
            </a>
          </span>
        )
      },
    },
    {
      title: 'Ubicación',
      key: 'ubicacion',
      className: 'cobro-bloque-col-ubicacion',
      onCell: () => ({ className: 'cobro-bloque-col-ubicacion' }),
      ellipsis: true,
      render: (_, row) => {
        const punto = {
          direccion: row.direccion,
          latitud: row.latitud,
          longitud: row.longitud,
        }
        const href = clienteMapaHref(punto)
        const label = clienteMapaLabel(punto)
        if (!href || !label) {
          return <Text type="secondary">—</Text>
        }
        return (
          <a
            className="cobro-bloque-ubicacion-link"
            href={href}
            target="_blank"
            rel="noreferrer"
            title={clienteMapaTitle(punto)}
          >
            <EnvironmentOutlined /> {label}
            {clienteTieneGps(punto) ? (
              <em className="cobro-bloque-ubicacion-link__gps">GPS</em>
            ) : null}
          </a>
        )
      },
    },
    {
      title: 'Venc.',
      dataIndex: 'fechaVencimiento',
      className: 'cobro-bloque-col-nowrap',
      onCell: () => ({ className: 'cobro-bloque-col-nowrap' }),
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Deuda',
      dataIndex: 'deudaPendiente',
      align: 'right',
      className: 'cobro-bloque-col-num',
      onCell: () => ({ className: 'cobro-bloque-col-num' }),
      render: (v: number, row) => (
        <Button
          type="link"
          size="small"
          disabled={(v || 0) <= 0}
          title="Usar como monto a cobrar"
          onClick={() => aplicarMontoRapido(row)}
        >
          <strong>{formatMoney(v)}</strong>
        </Button>
      ),
    },
    {
      title: 'Monto a cobrar',
      key: 'monto',
      className: 'cobro-bloque-col-monto',
      onCell: () => ({ className: 'cobro-bloque-col-monto' }),
      render: (_, row) => {
        const e = getEdit(row)
        return (
          <MontoCobrarInput
            min={0}
            max={row.deudaPendiente}
            step={0.01}
            value={e.montoPagar}
            className="cobro-bloque-input-monto"
            controls={false}
            inputMode="decimal"
            onChange={(montoPagar) =>
              patchEdit(
                row.creditoId,
                { montoPagar, cuotasSeleccionadas: [] },
                row,
              )
            }
            onPressEnter={(ev) => {
              const tr = (ev.target as HTMLElement).closest('tr')
              const next = tr?.nextElementSibling?.querySelector<HTMLInputElement>(
                '.ant-input-number-input',
              )
              next?.focus()
              next?.select()
            }}
          />
        )
      },
    },
    {
      title: 'Tipo pago',
      key: 'tipo',
      className: 'cobro-bloque-col-tipo',
      onCell: () => ({ className: 'cobro-bloque-col-tipo' }),
      render: (_, row) => {
        const e = getEdit(row)
        return (
          <Select
            value={e.tipoPagoId}
            options={tipoPagoOptions}
            className="cobro-bloque-input-tipo"
            onChange={(v) => patchEdit(row.creditoId, { tipoPagoId: v }, row)}
          />
        )
      },
    },
    {
      title: 'Hora del voucher',
      key: 'fecha',
      className: 'cobro-bloque-col-fecha',
      onCell: () => ({ className: 'cobro-bloque-col-fecha' }),
      render: (_, row) => {
        const e = getEdit(row)
        if (!TIPOS_PAGO_DIGITAL.has(e.tipoPagoId)) {
          return <Text type="secondary">—</Text>
        }
        return (
          <FechaHoraDigitalField
            compact
            value={e.fechaHoraTrans}
            status={!e.fechaHoraTrans ? 'error' : undefined}
            onChange={(fechaHoraTrans) =>
              patchEdit(row.creditoId, { fechaHoraTrans }, row)
            }
          />
        )
      },
    },
    {
      title: 'Acción',
      key: 'accion',
      className: 'cobro-bloque-col-accion',
      onCell: () => ({ className: 'cobro-bloque-col-accion' }),
      render: (_, row) => {
        if (enviadosSet.has(row.creditoId)) {
          return <Tag color="success">Enviado</Tag>
        }
        const errorMsg = cola.errores[String(row.creditoId)]
        if (!esPorEnviar(row)) {
          return errorMsg ? (
            <Text type="danger" title={errorMsg}>
              Error
            </Text>
          ) : (
            <Text type="secondary">—</Text>
          )
        }
        return (
          <Space size={4} wrap>
            <Button
              type="primary"
              size="small"
              loading={cobrarYaId === row.creditoId}
              disabled={cobrarYaId != null && cobrarYaId !== row.creditoId}
              onClick={() => void cobrarYa(row)}
            >
              {online ? 'Cobrar ya' : 'Guardar'}
            </Button>
            <Button
              size="small"
              icon={<CloseOutlined />}
              aria-label="Quitar de Por enviar"
              title="Quitar de Por enviar (borra el monto tipado)"
              onClick={() => quitarTipado(row)}
            />
            {errorMsg ? (
              <Text type="danger" title={errorMsg}>
                Error
              </Text>
            ) : null}
          </Space>
        )
      },
    },
  ]

  const columnsEnviados: ColumnsType<CobroEnviadoItem> = [
    {
      title: 'Cliente',
      dataIndex: 'personaNombre',
      render: (_, it) => (
        <div className="cobro-bloque-cliente-cell">
          <strong className="cobro-bloque-cliente-cell__name" title={it.personaNombre}>
            {it.personaNombre}
          </strong>
          <span className="cobro-bloque-cliente-cell__meta">#{it.creditoId}</span>
        </div>
      ),
    },
    {
      title: 'Monto',
      dataIndex: 'montoPagar',
      align: 'right',
      render: (v: number) => <strong>S/ {formatMoney(v)}</strong>,
    },
    {
      title: 'Tipo pago',
      dataIndex: 'tipoPagoId',
      render: (v: number) =>
        tipoPagoOptions.find((o) => o.value === v)?.label ?? `Tipo ${v}`,
    },
    {
      title: 'Origen',
      dataIndex: 'origen',
      render: (v: CobroEnviadoItem['origen']) =>
        v === 'cobrar_ya' ? 'Cobrar ya' : v === 'sync' ? 'Sincronizado' : 'Cola',
    },
    {
      title: 'Hora',
      dataIndex: 'enviadoAt',
      render: (v: string) =>
        new Date(v).toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit' }),
    },
  ]

  const sinCaja = (!ctx && !sesionQuery.isLoading) || Boolean(ctx?.indCierre)

  const toolbar = (
    <div className="cobro-bloque-command">
      <div className="cobro-bloque-command__row">
        <Button
          className="cobro-bloque-command__volver"
          icon={<ArrowLeftOutlined />}
          aria-label="Volver a caja diario"
          onClick={() => navigate('/caja/diario')}
        >
          {fieldMode ? null : 'Volver'}
        </Button>
        <Input
          allowClear
          size="middle"
          prefix={<SearchOutlined />}
          placeholder={fieldMode ? 'Buscar cliente o crédito…' : 'Buscar cliente, celular, dirección o crédito…'}
          value={filtro}
          onChange={(e) => setFiltro(e.target.value)}
          className="cobro-bloque-command__search"
          aria-label="Filtrar planilla"
        />
      </div>
      <div className="cobro-bloque-command__meta">
        {fieldMode && tabPrincipal === 'cartera' ? (
          <div className="cobro-bloque-command__vistas" role="tablist" aria-label="Vista de planilla">
            {(
              [
                { id: 'todos' as const, label: 'Todos' },
                { id: 'mora' as const, label: 'Mora' },
                { id: 'cobro' as const, label: 'Tipados' },
              ] as const
            ).map((v) => (
              <button
                key={v.id}
                type="button"
                role="tab"
                aria-selected={vistaCampo === v.id}
                className={[
                  'cobro-bloque-command__vista',
                  vistaCampo === v.id ? 'cobro-bloque-command__vista--on' : '',
                ]
                  .filter(Boolean)
                  .join(' ')}
                onClick={() => setVistaCampo(v.id)}
              >
                {v.label}
              </button>
            ))}
          </div>
        ) : null}
        <span className="cobro-bloque-command__chip">
          {tabPrincipal === 'enviados'
            ? `${enviadosFiltrados.length}/${cola.enviados.length}`
            : tabPrincipal === 'por-enviar'
              ? `${filas.length}/${resumen.conCobro}`
              : `${filas.length}/${resumen.totalFilas}`}
        </span>
        <span className="cobro-bloque-command__chip cobro-bloque-command__chip--accent">
          {tabPrincipal === 'enviados'
            ? `Enviados ${cola.enviados.length} · S/ ${formatMoney(enviadosTotal)}`
            : `Por enviar ${resumen.conCobro} · S/ ${formatMoney(resumen.total)}`}
        </span>
      </div>
    </div>
  )

  const tabsPrincipal = (
    <Tabs
      className="cobro-bloque-tabs"
      size={fieldMode ? 'small' : 'middle'}
      activeKey={tabPrincipal}
      onChange={(k) => setTabPrincipal(k as TabPrincipal)}
      items={[
        {
          key: 'cartera',
          label: (
            <span className="cobro-bloque-tabs__label">
              Cartera
              <Badge
                className="cobro-bloque-tabs__badge"
                count={resumen.totalFilas}
                overflowCount={999}
                showZero
                color="#8c8c8c"
              />
            </span>
          ),
        },
        {
          key: 'por-enviar',
          label: (
            <span className="cobro-bloque-tabs__label">
              {fieldMode ? 'Enviar' : 'Por enviar'}
              <Badge
                className="cobro-bloque-tabs__badge"
                count={resumen.conCobro}
                showZero
                color="#faad14"
              />
            </span>
          ),
        },
        {
          key: 'enviados',
          label: (
            <span className="cobro-bloque-tabs__label">
              Enviados
              <Badge
                className="cobro-bloque-tabs__badge"
                count={cola.enviados.length}
                showZero
                color="#52c41a"
              />
            </span>
          ),
        },
      ]}
    />
  )

  const actionDock = !sinCaja && tabPrincipal !== 'enviados' ? (
    <div
      className="cobro-bloque-dock"
      role="region"
      aria-label="Acciones de cobro"
      style={
        dockBox
          ? { left: dockBox.left, width: dockBox.width }
          : undefined
      }
    >
      <div className="cobro-bloque-dock__inner">
        <div className="cobro-bloque-dock__stats">
          <div>
            <Text type="secondary" className="cobro-bloque-dock__stats-label">
              Por enviar
            </Text>
            <strong>
              {resumen.conCobro} · S/ {formatMoney(resumen.total)}
            </strong>
          </div>
          {cola.enviados.length > 0 ? (
            <div className="cobro-bloque-dock__enviados">
              <Text type="secondary" className="cobro-bloque-dock__stats-label">
                Enviados
              </Text>
              <strong>{cola.enviados.length}</strong>
            </div>
          ) : null}
        </div>
        <Space wrap className="cobro-bloque-dock__actions">
          {scrolledDown ? (
            <Button icon={<ArrowUpOutlined />} onClick={scrollPlanillaTop} aria-label="Ir arriba">
              {fieldMode ? null : 'Arriba'}
            </Button>
          ) : null}
          <Button
            type="primary"
            size="large"
            icon={<CheckOutlined />}
            loading={enviarCola.isPending}
            disabled={resumen.conCobro < 1}
            onClick={confirmarEnviarCola}
          >
            {fieldMode ? `Enviar (${resumen.conCobro})` : 'Enviar cola'}
          </Button>
        </Space>
      </div>
    </div>
  ) : null

  const resumenBar = fieldMode ? (
    <div className="cobro-bloque-resumen-mobile" role="region" aria-label="Resumen operativo">
      <span className="cobro-bloque-resumen-mobile__pill cobro-bloque-resumen-mobile__pill--send">
        Enviar {resumen.conCobro} · S/ {formatMoney(resumen.total)}
      </span>
      <span className="cobro-bloque-resumen-mobile__pill cobro-bloque-resumen-mobile__pill--ok">
        Enviados {cola.enviados.length}
      </span>
    </div>
  ) : (
    <div className="credix-cobro-bloque-resumen" role="region" aria-label="Resumen operativo">
      <div>
        <Text type="secondary">Por enviar</Text>
        <div>
          <strong>
            {resumen.conCobro} · S/ {formatMoney(resumen.total)}
          </strong>
        </div>
      </div>
      <div>
        <Text type="secondary">Enviados hoy</Text>
        <div>
          <strong>
            {cola.enviados.length} · S/ {formatMoney(enviadosTotal)}
          </strong>
        </div>
      </div>
      <div>
        <Text type="secondary">Total por enviar</Text>
        <div className="credix-cobro-bloque-total">S/ {formatMoney(resumen.total)}</div>
      </div>
      <div>
        <Text type="secondary">Por método</Text>
        <div className="credix-cobro-bloque-metodos">
          {resumen.porMetodo.length === 0 ? (
            <Text type="secondary">Sin montos aún</Text>
          ) : (
            resumen.porMetodo.map((m) => (
              <span key={m.label}>
                {m.label}: {m.n} · S/ {formatMoney(m.monto)}
              </span>
            ))
          )}
        </div>
      </div>
    </div>
  )

  return (
    <CredixPage
      className={`cobro-bloque-page${fieldMode ? ' cobro-bloque-page--field' : ''}${actionDock ? ' cobro-bloque-page--dock' : ''}`}
      title="Cobro"
      subtitle={
        fieldMode
          ? undefined
          : 'Tipar ≠ cobrado. Use Cobrar ya o Enviar cola para registrar. Completar impagos solo en Caja Diario.'
      }
      breadcrumb={
        fieldMode
          ? undefined
          : [
              { title: <Link to="/inicio">Inicio</Link> },
              { title: <Link to="/caja">Caja</Link> },
              { title: <Link to="/caja/diario">Diario</Link> },
              { title: 'Cobro' },
            ]
      }
    >
      {sinCaja ? (
        <Alert
          type="warning"
          showIcon
          message="Se requiere caja diario abierta"
          description="Abra su caja diario para cargar la planilla de cobranza."
          action={
            <Button type="primary" onClick={() => navigate('/caja/diario')}>
              Ir a caja diario
            </Button>
          }
        />
      ) : (
        <>
          {!fieldMode ? (
            <Alert
              type="info"
              showIcon
              className="cobro-bloque-page__hint"
              message="Ruta digital (reemplazo del reporte impreso / PWA externo)"
              description="Tipar no cobra: los montos tipados quedan en Por enviar y se guardan en este dispositivo (el borrador caduca al día siguiente). Enviar cola o Cobrar ya sí registra el cobro en el servidor. Completar impagos solo se hace en Caja Diario."
            />
          ) : null}

          {draftBanner ? (
            <Alert
              type="success"
              showIcon
              closable
              className="cobro-bloque-page__draft"
              message={draftBanner}
              action={
                <Button size="small" danger onClick={descartarBorrador}>
                  Descartar borrador
                </Button>
              }
              onClose={() => setDraftBanner(null)}
            />
          ) : null}

          {!online ? (
            <Alert
              type="warning"
              showIcon
              className="cobro-bloque-page__draft"
              message="Sin conexión"
              description="Puede seguir tipando montos sobre la última planilla. Al enviar la cola se guarda en este dispositivo y se envía al recuperar red. Cobrar ya sin red deja el cobro en Por enviar."
            />
          ) : null}

          {online && (usingCache || offlineSesion) ? (
            <Alert
              type="info"
              showIcon
              className="cobro-bloque-page__draft"
              message="Planilla desde caché local"
              description="Se muestra la última foto descargada. Al recuperar red estable se actualizará."
            />
          ) : null}

          {pendingSync ? (
            <Alert
              type="warning"
              showIcon
              className="cobro-bloque-page__draft"
              message={syncingQueue ? 'Sincronizando cola…' : 'Cola pendiente de envío'}
              description="Hay cobros en cola listos para enviar. No cierre sesión hasta que se confirme el envío."
              action={
                online && !syncingQueue ? (
                  <Button
                    size="small"
                    type="primary"
                    onClick={() => {
                      setOnline(false)
                      window.setTimeout(() => setOnline(true), 0)
                    }}
                  >
                    Reintentar envío
                  </Button>
                ) : undefined
              }
            />
          ) : null}

          <div className="cobro-bloque-sticky-chrome">
            {tabsPrincipal}
            {toolbar}
            {resumenBar}
          </div>

          <CredixPanel
            title={
              tabPrincipal === 'enviados'
                ? `Enviados (${enviadosFiltrados.length})`
                : tabPrincipal === 'por-enviar'
                  ? `Por enviar (${filas.length})`
                  : fieldMode
                    ? `Ruta (${filas.length})`
                    : `Planilla de campo (${filas.length})`
            }
            className={fieldMode ? 'cobro-bloque-panel--field' : undefined}
          >
            {tabPrincipal === 'enviados' ? (
              enviadosFiltrados.length === 0 ? (
                <Text type="secondary">Aún no hay cobros enviados hoy.</Text>
              ) : fieldMode ? (
                <ul className="cobro-bloque-enviados-list">
                  {enviadosFiltrados.map((it) => (
                    <li
                      key={`${it.creditoId}-${it.enviadoAt}`}
                      className="cobro-bloque-enviados-list__item"
                    >
                      <div className="cobro-bloque-enviados-list__main">
                        <strong title={it.personaNombre}>{it.personaNombre}</strong>
                        <span className="cobro-bloque-enviados-list__meta">
                          #{it.creditoId} ·{' '}
                          {it.origen === 'cobrar_ya'
                            ? 'Cobrar ya'
                            : it.origen === 'sync'
                              ? 'Sync'
                              : 'Cola'}{' '}
                          ·{' '}
                          {new Date(it.enviadoAt).toLocaleTimeString('es-PE', {
                            hour: '2-digit',
                            minute: '2-digit',
                          })}
                        </span>
                      </div>
                      <strong className="cobro-bloque-enviados-list__monto">
                        S/ {formatMoney(it.montoPagar)}
                      </strong>
                    </li>
                  ))}
                </ul>
              ) : (
                <CredixDataTable
                  mode="operacion"
                  className="cobro-bloque-table"
                  rowKey={(it: CobroEnviadoItem) => `${it.creditoId}-${it.enviadoAt}`}
                  columns={columnsEnviados}
                  dataSource={enviadosFiltrados}
                  pagination={{ pageSize: 50, showSizeChanger: true }}
                  size="middle"
                  scroll={{ x: 'max-content' }}
                />
              )
            ) : carteraQuery.isError ? (
              <Alert
                type="error"
                showIcon
                message="No se pudo cargar la cartera"
                description={
                  carteraQuery.error instanceof ApiError
                    ? carteraQuery.error.message
                    : 'Error de red'
                }
              />
            ) : fieldMode ? (
              <div className="cobro-bloque-field-list">
                {carteraQuery.isLoading ? (
                  <div className="cobro-bloque-field-list__loading">
                    <Spin /> Cargando planilla…
                  </div>
                ) : filas.length === 0 ? (
                  <Text type="secondary">
                    {tabPrincipal === 'por-enviar'
                      ? 'No hay cobros por enviar. Tipie montos en Cartera.'
                      : vistaCampo === 'cobro'
                      ? 'Aún no tipó cobros. Cambie a Todos o Mora.'
                      : vistaCampo === 'mora'
                        ? 'No hay clientes en mora con este filtro.'
                        : 'Sin clientes en la planilla.'}
                  </Text>
                ) : (
                  filas.map((row) => {
                    const e = getEdit(row)
                    return (
                      <CobroBloqueFieldCard
                        key={row.creditoId}
                        row={row}
                        edit={e}
                        tipoPagoOptions={tipoPagoOptions}
                        onPatch={(patch) => patchEdit(row.creditoId, patch, row)}
                        enviado={enviadosSet.has(row.creditoId)}
                        errorMsg={cola.errores[String(row.creditoId)] ?? null}
                        onCobrarYa={() => void cobrarYa(row)}
                        cobrarYaLoading={cobrarYaId === row.creditoId}
                        onUbicacionGuardada={(personaId, lat, lng) => {
                          queryClient.setQueryData(
                            carteraQueryKey,
                            (prev: { rows: CreditoGestorPendienteRow[]; fromCache: boolean } | undefined) => {
                              if (!prev) return prev
                              const rows = prev.rows.map((r) =>
                                r.personaId === personaId
                                  ? { ...r, latitud: lat, longitud: lng }
                                  : r,
                              )
                              if (ctx && usuarioId > 0) {
                                saveCobroBloqueCarteraCache({
                                  version: 1,
                                  usuarioId,
                                  oficinaId,
                                  cajaDiarioId: ctx.cajaDiarioId,
                                  fechaOperacion: fechaOperacionLocal(),
                                  cachedAt: new Date().toISOString(),
                                  rows,
                                  session: {
                                    oficinaId: ctx.oficinaId,
                                    cajaDiarioId: ctx.cajaDiarioId,
                                    cajaId: ctx.cajaId,
                                    cajaDenominacion: ctx.cajaDenominacion,
                                    fechaIniOperacion: ctx.fechaIniOperacion,
                                    saldoInicial: ctx.saldoInicial,
                                    entradas: ctx.entradas,
                                    salidas: ctx.salidas,
                                    saldoFinal: ctx.saldoFinal,
                                    indCierre: ctx.indCierre,
                                    esCajaCentral: ctx.esCajaCentral,
                                  },
                                })
                              }
                              return { ...prev, rows, fromCache: false }
                            },
                          )
                        }}
                        cuotasSlot={
                          <CuotasSubgrid
                            creditoId={row.creditoId}
                            deudaMax={row.deudaPendiente}
                            selected={e.cuotasSeleccionadas}
                            onChange={(ids, monto) =>
                              patchEdit(
                                row.creditoId,
                                { cuotasSeleccionadas: ids, montoPagar: monto },
                                row,
                              )
                            }
                          />
                        }
                      />
                    )
                  })
                )}
              </div>
            ) : (
              <CredixDataTable
                mode="operacion"
                className="cobro-bloque-table"
                rowKey="creditoId"
                loading={carteraQuery.isLoading}
                columns={columns}
                dataSource={filas}
                pagination={{ pageSize: 50, showSizeChanger: true }}
                size="middle"
                scroll={{ x: 'max-content' }}
                expandable={{
                  expandedRowRender: (row) => {
                    const e = getEdit(row)
                    return (
                      <CuotasSubgrid
                        creditoId={row.creditoId}
                        deudaMax={row.deudaPendiente}
                        selected={e.cuotasSeleccionadas}
                        onChange={(ids, monto) =>
                          patchEdit(
                            row.creditoId,
                            { cuotasSeleccionadas: ids, montoPagar: monto },
                            row,
                          )
                        }
                      />
                    )
                  },
                }}
              />
            )}
          </CredixPanel>

          {actionDock}
        </>
      )}
    </CredixPage>
  )
}
