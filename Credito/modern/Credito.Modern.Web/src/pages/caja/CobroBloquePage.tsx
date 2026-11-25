import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Badge,
  Button,
  Grid,
  Input,
  Modal,
  Space,
  Spin,
  Tabs,
  Typography,
  message,
} from 'antd'
import {
  ArrowLeftOutlined,
  CheckOutlined,
  PrinterOutlined,
  SearchOutlined,
  ArrowUpOutlined,
} from '@ant-design/icons'
import {
  cobrarPlanillaBloque,
  downloadMovimientoCajaTicketPdf,
  enrichCreditosGestorContactos,
  fetchCajaDiarioSesion,
  fetchCreditosGestorDesembolsados,
  type CreditoGestorPendienteRow,
} from '../../api/cajaDiario'
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, CredixPanel } from '../../components/credix'
import { buildTipoPagoOptions } from '../tesoreria/components/TipoPagoBrand'
import { useEsCentral } from '../../hooks/useEsCentral'
import { formatMoney } from '../../utils/formatMoney'
import { moneyEquals, roundMoney, sumMoney } from '../../utils/money'
import { esTipoPagoDigital } from '../../utils/tipoPago'
import {
  collapsePlanillaByCredito,
  expandRowToPlanillaItems,
  validateRowMixtoMoney,
} from '../../utils/cobroBloquePlanillaExpand'
import {
  defaultPagoMediosSplit,
  PagoMediosSplit,
  sumPagoMedios,
  type PagoMedioLineaUi,
} from '../../components/caja/PagoMediosSplit'
import { clearCobroBloqueEjecutadoHoy } from '../../utils/cobroBloqueDayLock'
import {
  clearCobroColaError,
  isCreditoEnviadoHoy,
  loadCobroCola,
  markCobroEnviados,
  setCobroColaError,
  ticketIdsDeEnviado,
  type CobroColaDia,
  type CobroEnviadoItem,
} from '../../utils/cobroCola'
import {
  invalidateCajaOperacion,
  removeCreditosFromPendingProcess,
} from '../../utils/cajaOperacionSync'
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
import type { CajaSession } from './cajaDiario/types'
import { nowDatetimeLocal } from './components/FechaHoraDigitalField'
import { CobroBloqueFieldCard } from './components/CobroBloqueFieldCard'
import { CobroBloqueCuotasSubgrid } from './components/CobroBloqueCuotasSubgrid'
import {
  buildCobroBloqueCarteraColumns,
  buildCobroBloqueEnviadosColumns,
} from './components/CobroBloqueColumns'
import '../../styles/cobro-bloque.css'

type VistaCampo = 'todos' | 'mora' | 'cobro'
type TabPrincipal = 'cartera' | 'enviados'

const { Text } = Typography

type RowEdit = CobroBloqueRowDraft

function defaultEdit(): RowEdit {
  return {
    montoPagar: 0,
    tipoPagoId: 1,
    fechaHoraTrans: nowDatetimeLocal(),
    cuotasSeleccionadas: [],
  }
}

export function CobroBloquePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const screens = Grid.useBreakpoint()
  const fieldMode = screens.md !== true
  const { session } = useAuth()
  const { esCentral } = useEsCentral()
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
  const [ticketPrintingId, setTicketPrintingId] = useState<number | null>(null)
  const [mixtoCreditoId, setMixtoCreditoId] = useState<number | null>(null)
  const [mixtoLineas, setMixtoLineas] = useState<PagoMedioLineaUi[]>([])
  const carteraRowsRef = useRef<CreditoGestorPendienteRow[]>([])

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

  // Diario puede haber marcado Enviados en otro módulo: rehidratar al volver.
  useEffect(() => {
    const reload = () => {
      if (usuarioId < 1 || cajaDiarioId < 1) return
      setCola(loadCobroCola({ usuarioId, oficinaId, cajaDiarioId }))
      void queryClient.invalidateQueries({ queryKey: ['caja-creditos-gestor-des'] })
    }
    const onVis = () => {
      if (document.visibilityState === 'visible') reload()
    }
    window.addEventListener('focus', reload)
    document.addEventListener('visibilitychange', onVis)
    return () => {
      window.removeEventListener('focus', reload)
      document.removeEventListener('visibilitychange', onVis)
    }
  }, [usuarioId, oficinaId, cajaDiarioId, queryClient])

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
    staleTime: 0,
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
    () => buildTipoPagoOptions(tiposPagoQuery.data ?? [], { esCentral }),
    [tiposPagoQuery.data, esCentral],
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
    // Cola offline pendiente → restaurar tipados en Cartera.
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
      setCola(loadCobroCola({ usuarioId, oficinaId, cajaDiarioId }))
    }
    if (hayDraft && draft) {
      setDraftBanner(
        `Borrador recuperado (${formatDraftAge(draft.updatedAt)}). Puede continuar donde lo dejó.`,
      )
    } else if (restauradosPending > 0) {
      setDraftBanner(
        `Se restauraron ${restauradosPending} cobro(s) tipados pendientes de registrar.`,
      )
    }
  }, [usuarioId, oficinaId, cajaDiarioId])

  // Autoguardado al navegar a otro módulo / minimizar.
  useEffect(() => {
    if (usuarioId < 1 || cajaDiarioId < 1) return
    const persist = () => {
      const editsMap: Record<string, RowEdit> = {}
      for (const [k, v] of Object.entries(edits)) {
        if (
          (v.montoPagar ?? 0) > 0 ||
          (v.cuotasSeleccionadas?.length ?? 0) > 0 ||
          v.tipoPagoId !== 1
        ) {
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
        esTipoPagoDigital(next.tipoPagoId) &&
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

  /** Tipado listo para registrar: monto &gt; 0 y aún no enviado hoy. */
  const esTipado = useCallback(
    (row: CreditoGestorPendienteRow): boolean =>
      !enviadosSet.has(row.creditoId) && (edits[row.creditoId]?.montoPagar ?? 0) > 0,
    [enviadosSet, edits],
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
      if (fieldMode && vistaCampo === 'mora' && r.diasAtrazo <= 0) return false
      if (fieldMode && vistaCampo === 'cobro' && !esTipado(r)) return false
      return true
    })
  }, [carteraRows, filtro, fieldMode, vistaCampo, esTipado])

  const enviadosFiltrados = useMemo(() => {
    const q = filtro.trim().toLowerCase()
    if (!q) return cola.enviados
    return cola.enviados.filter(
      (e) =>
        e.personaNombre.toLowerCase().includes(q) || String(e.creditoId).includes(q),
    )
  }, [cola.enviados, filtro])

  const enviadosTotal = useMemo(
    () => sumMoney(cola.enviados.map((e) => e.montoPagar || 0)),
    [cola.enviados],
  )

  const resumen = useMemo(() => {
    let listos = 0
    const montos: number[] = []
    const porMetodo = new Map<number, { label: string; monto: number; n: number }>()
    for (const row of carteraRows ?? []) {
      if (!esTipado(row)) continue
      const e = getEdit(row)
      listos++
      montos.push(e.montoPagar)
      const medios = e.medios?.filter((m) => roundMoney(m.importe) > 0) ?? []
      if (medios.length >= 2) {
        for (const m of medios) {
          const label =
            tipoPagoOptions.find((o) => o.value === m.tipoPagoId)?.label ??
            `Tipo ${m.tipoPagoId}`
          const cur = porMetodo.get(m.tipoPagoId) ?? { label, monto: 0, n: 0 }
          cur.monto = sumMoney([cur.monto, m.importe])
          cur.n += 1
          porMetodo.set(m.tipoPagoId, cur)
        }
        continue
      }
      const label =
        tipoPagoOptions.find((o) => o.value === e.tipoPagoId)?.label ?? `Tipo ${e.tipoPagoId}`
      const cur = porMetodo.get(e.tipoPagoId) ?? { label, monto: 0, n: 0 }
      cur.monto = sumMoney([cur.monto, e.montoPagar])
      cur.n += 1
      porMetodo.set(e.tipoPagoId, cur)
    }
    const totalFilas = carteraRows?.length ?? 0
    return {
      conCobro: listos,
      total: sumMoney(montos),
      porMetodo: [...porMetodo.values()],
      totalFilas,
    }
  }, [carteraRows, esTipado, getEdit, tipoPagoOptions])

  const validarPlanilla = (rows: CreditoGestorPendienteRow[]): string | null => {
    for (const row of rows) {
      const e = getEdit(row)
      if (e.montoPagar <= 0) {
        return `Digite un monto mayor a cero para ${row.personaNombre} (crédito ${row.creditoId}).`
      }
      const mixtoErr = validateRowMixtoMoney(e)
      if (mixtoErr) {
        return `${row.personaNombre} (crédito ${row.creditoId}): ${mixtoErr}`
      }
      const medios = e.medios?.filter((m) => roundMoney(m.importe) > 0) ?? []
      if (medios.length >= 2) continue
      if (esTipoPagoDigital(e.tipoPagoId) && !e.fechaHoraTrans?.trim()) {
        return `Ingrese la fecha/hora de transferencia para ${row.personaNombre} (crédito ${row.creditoId}).`
      }
    }
    return null
  }

  /** Tipados con monto &gt; 0; mixto se expande a N ítems del mismo crédito. */
  const buildPlanillaPayload = () =>
    (carteraRows ?? [])
      .filter((row) => esTipado(row))
      .flatMap((row) => expandRowToPlanillaItems(row.creditoId, getEdit(row)))
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
      total: sumMoney(resto.map((x) => x.montoPagar)),
      planilla: resto,
    })
  }

  /** Agrupa tickets API por crédito (mixto = varios movimientoCajaId). */
  const ticketsByCredito = (
    tickets: { creditoId: number; movimientoCajaId: number }[] | null | undefined,
  ): Record<number, number[]> => {
    const map: Record<number, number[]> = {}
    for (const t of tickets ?? []) {
      if (!(t.movimientoCajaId > 0) || !(t.creditoId > 0)) continue
      const list = map[t.creditoId] ?? (map[t.creditoId] = [])
      if (!list.includes(t.movimientoCajaId)) list.push(t.movimientoCajaId)
    }
    return map
  }

  /** Registra enviados locales, limpia tipados y sincroniza caches Diario+Cobro. */
  const aplicarEnviados = (
    planilla: PlanillaItem[],
    origen: CobroEnviadoItem['origen'],
    extra?: Record<number, number[]>,
  ) => {
    if (planilla.length === 0) return
    const nombres = new Map(
      carteraRowsRef.current.map((r) => [r.creditoId, r.personaNombre] as const),
    )
    const colapsada = collapsePlanillaByCredito(planilla)
    const nextCola = markCobroEnviados(
      { usuarioId, oficinaId, cajaDiarioId },
      colapsada.map((p) => {
        const ids = extra?.[p.creditoId] ?? []
        return {
          creditoId: p.creditoId,
          personaNombre: nombres.get(p.creditoId) ?? `Crédito ${p.creditoId}`,
          montoPagar: p.montoPagar,
          tipoPagoId: p.tipoPagoId,
          medios:
            p.medios && p.medios.length >= 2
              ? p.medios.map((m) => ({
                  tipoPagoId: m.tipoPagoId,
                  importe: m.importe,
                }))
              : undefined,
          movimientoCajaId: ids[0] ?? null,
          movimientoCajaIds: ids.length >= 2 ? ids : undefined,
          origen,
        }
      }),
    )
    setCola(nextCola)
    const ids = colapsada.map((p) => p.creditoId)
    setEdits((prev) => {
      const next = { ...prev }
      for (const id of ids) delete next[id]
      return next
    })
    quitarDePending(ids)
    invalidateCajaOperacion(queryClient, { oficinaId, cajaDiarioId })
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
    const conDeuda = planilla.map((p) => {
      const row = (carteraRows ?? []).find((r) => r.creditoId === p.creditoId)
      return {
        ...p,
        deudaAlTipar: row?.deudaPendiente ?? null,
      }
    })
    enqueueCobroBloqueProcess({
      version: 1,
      usuarioId,
      oficinaId,
      cajaDiarioId: ctx.cajaDiarioId,
      fechaOperacion: fechaOperacionLocal(),
      queuedAt: new Date().toISOString(),
      conCobro: collapsePlanillaByCredito(conDeuda).length,
      total: sumMoney(conDeuda.map((x) => x.montoPagar)),
      planilla: conDeuda,
    })
    setPendingSync(true)
    message.warning(
      'Sin conexión: los cobros quedaron tipados en este dispositivo. Se registrarán al recuperar red.',
    )
  }

  const enviarPlanilla = useMutation({
    mutationFn: async (planillaIn: PlanillaItem[] | undefined) => {
      if (!ctx || ctx.indCierre) throw new Error('No hay caja diario abierta.')

      const planilla = planillaIn && planillaIn.length > 0 ? planillaIn : buildPlanillaPayload()
      if (planilla.length === 0) throw new Error('No hay cobros tipados.')

      const rows = (carteraRows ?? []).filter((r) =>
        planilla.some((p) => p.creditoId === r.creditoId),
      )
      const err = validarPlanilla(rows)
      if (err) throw new Error(err)

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
      const ticketMap = ticketsByCredito(r.result.tickets)
      const creditosUnicos = collapsePlanillaByCredito(r.planilla)
      const origen = creditosUnicos.length === 1 ? 'cobrar_ya' : 'lote'
      aplicarEnviados(r.planilla, origen, ticketMap)
      setTabPrincipal('enviados')
      message.success(
        r.result.mensaje || `${creditosUnicos.length} crédito(s) cobrado(s).`,
      )
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error'),
  })

  /** Cartera → registra ya (un crédito). */
  const cobrarFila = (row: CreditoGestorPendienteRow) => {
    if (!ctx || ctx.indCierre) {
      message.warning('No hay caja diario abierta.')
      return
    }
    if (enviadosSet.has(row.creditoId)) {
      message.info('Este crédito ya fue enviado hoy.')
      return
    }
    const err = validarPlanilla([row])
    if (err) {
      message.warning(err)
      return
    }
    const e = getEdit(row)
    void enviarPlanilla.mutateAsync(expandRowToPlanillaItems(row.creditoId, e))
  }

  const imprimirTicket = async (
    idsOrOne: number | number[] | null | undefined,
  ) => {
    const ids = (
      Array.isArray(idsOrOne)
        ? idsOrOne
        : idsOrOne && idsOrOne > 0
          ? [idsOrOne]
          : []
    ).filter((id) => id > 0)
    if (ids.length === 0 || oficinaId < 1) {
      message.warning('Este cobro no tiene ticket disponible para reimprimir.')
      return
    }
    setTicketPrintingId(ids[0]!)
    try {
      // Un PDF: si hay varios IDs (mixto) el API lista todos los medios.
      await downloadMovimientoCajaTicketPdf(oficinaId, ids)
      message.success(
        ids.length > 1 ? 'Comprobante mixto descargado' : 'Ticket descargado',
      )
    } catch (e) {
      message.error(
        e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'No se pudo imprimir el ticket',
      )
    } finally {
      setTicketPrintingId(null)
    }
  }

  // Sincronización offline: al recuperar red envía la cola pendiente (sin impagos ni day-lock).
  useEffect(() => {
    if (!online || usuarioId < 1 || cajaDiarioId < 1) return
    if (syncingRef.current) return
    const pending = loadCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
    if (!pending) return

    syncingRef.current = true
    setSyncingQueue(true)
    void (async () => {
      try {
        // Refetch cartera: omitir ya enviados locales o sin deuda (cobrados en Diario).
        let deudaByCredito = new Map<number, number>()
        try {
          const rows = await fetchCreditosGestorDesembolsados({
            oficinaId: pending.oficinaId,
            usuarioId,
          })
          deudaByCredito = new Map(rows.map((r) => [r.creditoId, r.deudaPendiente ?? 0]))
        } catch {
          /* si falla, seguimos con filtro local */
        }
        const colaHoy = loadCobroCola({ usuarioId, oficinaId, cajaDiarioId })
        const planilla = pending.planilla.filter((p) => {
          if (p.montoPagar <= 0) return false
          if (isCreditoEnviadoHoy(colaHoy, p.creditoId)) return false
          if (deudaByCredito.size > 0) {
            const deuda = deudaByCredito.get(p.creditoId)
            if (deuda == null || deuda <= 0) return false
            // Anti doble cobro: si la deuda ya bajó ≈ el tipado encolado, el pago ya entró.
            if (p.deudaAlTipar != null && p.deudaAlTipar > 0) {
              const esperadaTrasPago = roundMoney(p.deudaAlTipar - p.montoPagar)
              if (
                moneyEquals(esperadaTrasPago, deuda) ||
                roundMoney(deuda) < esperadaTrasPago
              ) {
                return false
              }
            }
          }
          return true
        })
        const omitidos = pending.planilla.length - planilla.length
        if (planilla.length === 0) {
          clearCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
          setPendingSync(false)
          if (omitidos > 0) {
            message.info(
              `${omitidos} cobro(s) omitido(s): ya registrados o sin deuda pendiente.`,
            )
          }
          return
        }
        if (omitidos > 0) {
          removeCreditosFromPendingProcess(
            { usuarioId, cajaDiarioId },
            pending.planilla
              .filter((p) => !planilla.some((x) => x.creditoId === p.creditoId))
              .map((p) => p.creditoId),
          )
        }

        const r = await cobrarPlanillaBloque({
          oficinaId: pending.oficinaId,
          cajaDiarioId: pending.cajaDiarioId,
          planilla,
          completarImpagos: false,
        })
        // No cancelar el éxito: si el request ya fue a BD, hay que registrar enviados.
        const ticketMap = ticketsByCredito(r.tickets)
        aplicarEnviados(
          planilla.map((p) => ({
            creditoId: p.creditoId,
            montoPagar: p.montoPagar,
            tipoPagoId: p.tipoPagoId,
            fechaHoraTrans: p.fechaHoraTrans ?? '',
          })),
          'sync',
          ticketMap,
        )
        message.success(
          omitidos > 0
            ? `${r.mensaje || 'Sincronizado'}. ${omitidos} omitido(s) (ya cobrados).`
            : r.mensaje || 'Cobros sincronizados al recuperar red.',
        )
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
          // Conservar tipados en Cartera y mostrar error por crédito.
          marcarErrorCola(
            pending.planilla.map((p) => ({
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

  const confirmarEnviarTipados = () => {
    if (resumen.conCobro < 1) {
      message.warning('Tipie un monto mayor a cero en Cartera para cobrar.')
      return
    }
    const rows = (carteraRows ?? []).filter((r) => esTipado(r))
    const err = validarPlanilla(rows)
    if (err) {
      message.warning(err)
      return
    }

    Modal.confirm({
      title: 'Registrar cobros',
      content: `¿Registrar ${resumen.conCobro} cobro(s) por S/ ${formatMoney(resumen.total)}? Pasarán a Enviados.`,
      okText: 'Sí, registrar',
      cancelText: 'Cancelar',
      onOk: () => enviarPlanilla.mutateAsync(undefined),
    })
  }

  const descartarBorrador = () => {
    clearCobroBloqueDraft({ usuarioId, cajaDiarioId })
    setEdits({})
    setFiltro('')
    setDraftBanner(null)
    message.success('Borrador descartado (solo en este dispositivo).')
  }

  const columns = buildCobroBloqueCarteraColumns({
    getEdit,
    patchEdit,
    aplicarMontoRapido,
    tipoPagoOptions,
    enviadosSet,
    cola,
    esTipado,
    cobrarFila,
    enviarPending: enviarPlanilla.isPending,
    onAbrirMixto: (row, base) => {
      setMixtoLineas(base)
      setMixtoCreditoId(row.creditoId)
    },
  })

  const columnsEnviados = buildCobroBloqueEnviadosColumns({
    tipoPagoOptions,
    ticketPrintingId,
    imprimirTicket,
  })

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
            : `${filas.length}/${resumen.totalFilas}`}
        </span>
        {tabPrincipal === 'enviados' ? (
          <span className="cobro-bloque-command__chip cobro-bloque-command__chip--accent">
            {`S/ ${formatMoney(enviadosTotal)}`}
          </span>
        ) : resumen.conCobro > 0 ? (
          <span className="cobro-bloque-command__chip cobro-bloque-command__chip--accent">
            {`${resumen.conCobro} tipados · S/ ${formatMoney(resumen.total)}`}
          </span>
        ) : null}
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

  /** Dock en Cartera cuando hay tipados: registrar lote sin pestaña intermedia. */
  const actionDock =
    !sinCaja && tabPrincipal === 'cartera' && resumen.conCobro > 0 ? (
      <div
        className="cobro-bloque-dock"
        role="region"
        aria-label="Registrar cobros tipados"
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
                Tipados listos
              </Text>
              <strong>
                {resumen.conCobro} · S/ {formatMoney(resumen.total)}
              </strong>
            </div>
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
              loading={enviarPlanilla.isPending}
              disabled={resumen.conCobro < 1}
              onClick={confirmarEnviarTipados}
            >
              {`Registrar (${resumen.conCobro})`}
            </Button>
          </Space>
        </div>
      </div>
    ) : null

  /** Resumen PC en Enviados; en Cartera bastan tabs + dock. */
  const resumenBar =
    fieldMode || tabPrincipal === 'cartera' ? null : (
      <div className="credix-cobro-bloque-resumen" role="region" aria-label="Resumen operativo">
        <div>
          <Text type="secondary">Enviados hoy</Text>
          <div>
            <strong>
              {cola.enviados.length} · S/ {formatMoney(enviadosTotal)}
            </strong>
          </div>
        </div>
      </div>
    )

  return (
    <CredixPage
      className={`cobro-bloque-page${fieldMode ? ' cobro-bloque-page--field' : ''}${actionDock ? ' cobro-bloque-page--dock' : ''}`}
      title="Cobro"
      subtitle={undefined}
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
              description="Puede tipar cobros sin red. Al recuperar conexión se registran solos (o use Reintentar envío)."
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
                <>
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
                            {it.origen === 'sync'
                              ? 'Sync'
                              : it.origen === 'diario'
                                ? 'Diario'
                                : it.origen === 'cobrar_ya'
                                  ? 'Cobro'
                                  : 'Lote'}{' '}
                            ·{' '}
                            {new Date(it.enviadoAt).toLocaleTimeString('es-PE', {
                              hour: '2-digit',
                              minute: '2-digit',
                            })}
                          </span>
                          <span className="cobro-bloque-enviados-list__medios" translate="no">
                            {(it.medios && it.medios.length >= 2
                              ? it.medios
                              : [{ tipoPagoId: it.tipoPagoId, importe: it.montoPagar }]
                            )
                              .map((m) => {
                                const label =
                                  tipoPagoOptions.find((o) => o.value === m.tipoPagoId)?.label ??
                                  `Tipo ${m.tipoPagoId}`
                                return it.medios && it.medios.length >= 2
                                  ? `${label} S/ ${formatMoney(m.importe)}`
                                  : label
                              })
                              .join(' + ')}
                          </span>
                        </div>
                        <div className="cobro-bloque-enviados-list__right">
                          <strong className="cobro-bloque-enviados-list__monto">
                            S/ {formatMoney(it.montoPagar)}
                          </strong>
                          {ticketIdsDeEnviado(it).length > 0 ? (
                            <Button
                              type="primary"
                              size="large"
                              className="cobro-bloque-enviados-list__print"
                              icon={<PrinterOutlined />}
                              loading={
                                ticketPrintingId != null &&
                                ticketIdsDeEnviado(it).includes(ticketPrintingId)
                              }
                              aria-label={`Imprimir ticket ${it.personaNombre}`}
                              title={
                                ticketIdsDeEnviado(it).length > 1
                                  ? 'Imprimir comprobante mixto'
                                  : 'Imprimir ticket'
                              }
                              onClick={() => void imprimirTicket(ticketIdsDeEnviado(it))}
                            />
                          ) : null}
                        </div>
                      </li>
                    ))}
                  </ul>
                </>
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
                    {vistaCampo === 'cobro'
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
                        cobrando={enviarPlanilla.isPending}
                        onDividir={() => {
                          const hint =
                            e.montoPagar > 0
                              ? e.montoPagar
                              : Math.max(0, row.deudaPendiente || 0)
                          const base =
                            e.medios && e.medios.length >= 2
                              ? e.medios.map((m) => ({
                                  key: `m-${m.tipoPagoId}-${m.importe}`,
                                  tipoPagoId: m.tipoPagoId,
                                  importe: m.importe,
                                  fechaTransferencia: m.fechaHoraTrans ?? '',
                                }))
                              : defaultPagoMediosSplit(
                                  tipoPagoOptions,
                                  hint > 0 ? hint : undefined,
                                )
                          setMixtoLineas(base)
                          setMixtoCreditoId(row.creditoId)
                        }}
                        onCobrar={() => cobrarFila(row)}
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
                          <CobroBloqueCuotasSubgrid
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
                      <CobroBloqueCuotasSubgrid
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

          <Modal
            title="Dividir cobro en varios medios"
            open={mixtoCreditoId != null}
            centered
            className={fieldMode ? 'cobro-bloque-mixto-modal cobro-bloque-mixto-modal--field' : 'cobro-bloque-mixto-modal'}
            width={520}
            styles={{
              body: fieldMode
                ? { maxHeight: 'min(70dvh, 520px)', overflowY: 'auto', paddingTop: 8 }
                : { maxHeight: 'min(72vh, 560px)', overflowY: 'auto' },
            }}
            onCancel={() => {
              setMixtoCreditoId(null)
              setMixtoLineas([])
            }}
            okText="Aplicar mixto"
            cancelText="Cancelar"
            onOk={() => {
              if (mixtoCreditoId == null) return
              const row = (carteraRows ?? []).find((r) => r.creditoId === mixtoCreditoId)
              if (!row) return
              const total = sumPagoMedios(mixtoLineas)
              if (!(total > 0)) {
                message.warning('Indique importes mayores a cero.')
                return Promise.reject()
              }
              const deuda = roundMoney(row.deudaPendiente)
              if (
                roundMoney(total) > deuda &&
                !moneyEquals(total, deuda)
              ) {
                message.error(
                  `El mixto (${formatMoney(total)}) no puede superar la deuda (${formatMoney(deuda)}).`,
                )
                return Promise.reject()
              }
              const draft: CobroBloqueRowDraft = {
                ...getEdit(row),
                montoPagar: roundMoney(total),
                tipoPagoId: mixtoLineas[0]?.tipoPagoId ?? 1,
                fechaHoraTrans:
                  mixtoLineas.find((l) => esTipoPagoDigital(l.tipoPagoId))?.fechaTransferencia ??
                  '',
                medios: mixtoLineas.map((l) => ({
                  tipoPagoId: l.tipoPagoId,
                  importe: roundMoney(l.importe ?? 0),
                  fechaHoraTrans: l.fechaTransferencia,
                })),
              }
              const err = validateRowMixtoMoney(draft)
              if (err) {
                message.warning(err)
                return Promise.reject()
              }
              patchEdit(mixtoCreditoId, draft, row)
              setMixtoCreditoId(null)
              setMixtoLineas([])
              message.success(`Mixto aplicado: ${formatMoney(total)}`)
            }}
            destroyOnClose
          >
            <PagoMediosSplit
              lineas={mixtoLineas}
              options={tipoPagoOptions}
              onChange={setMixtoLineas}
            />
            {mixtoCreditoId != null ? (
              <Button
                type="link"
                danger
                style={{ paddingLeft: 0, marginTop: 8 }}
                onClick={() => {
                  const row = (carteraRows ?? []).find((r) => r.creditoId === mixtoCreditoId)
                  if (!row) return
                  patchEdit(mixtoCreditoId, { medios: undefined }, row)
                  setMixtoCreditoId(null)
                  setMixtoLineas([])
                  message.info('Mixto quitado; queda un solo medio.')
                }}
              >
                Quitar mixto
              </Button>
            ) : null}
          </Modal>
        </>
      )}
    </CredixPage>
  )
}
