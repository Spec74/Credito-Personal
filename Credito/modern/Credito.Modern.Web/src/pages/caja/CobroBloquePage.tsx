import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Checkbox,
  Grid,
  Input,
  Modal,
  Select,
  Space,
  Spin,
  Typography,
  message,
} from 'antd'
import {
  ArrowLeftOutlined,
  CheckOutlined,
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
  type CreditoGestorPendienteRow,
} from '../../api/cajaDiario'
import { fetchEstadoPlanPago } from '../../api/creditoPlanes'
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, CredixPanel } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import { formatFecha } from '../../utils/formatFecha'
import {
  isCobroBloqueEjecutadoHoy,
  markCobroBloqueEjecutadoHoy,
} from '../../utils/cobroBloqueDayLock'
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
  clearCobroBloqueCarteraCache,
  clearCobroBloquePendingProcess,
  enqueueCobroBloqueProcess,
  isBrowserOnline,
  isLikelyNetworkError,
  loadCobroBloqueCarteraCache,
  loadCobroBloquePendingProcess,
  saveCobroBloqueCarteraCache,
} from '../../utils/cobroBloqueOffline'
import type { EstadoPlanPagoCuota } from '../../types/api'
import type { CajaSession } from './cajaDiario/types'
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
  const yaEjecutadoHoy = isCobroBloqueEjecutadoHoy()

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
    enabled: oficinaId > 0 && !yaEjecutadoHoy,
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
    const fecha = fechaOperacionLocal()
    const prefix = `credix.cobroBloqueCartera.v2:${usuarioId}:`
    try {
      for (let i = 0; i < localStorage.length; i++) {
        const k = localStorage.key(i)
        if (!k?.startsWith(prefix) || !k.endsWith(`:${fecha}`)) continue
        const raw = localStorage.getItem(k)
        if (!raw) continue
        const parsed = JSON.parse(raw) as {
          session?: CajaSession | null
        }
        if (parsed.session && !parsed.session.indCierre) return parsed.session
      }
    } catch {
      /* ignore */
    }
    return null
  }, [allowOfflineSession, usuarioId])

  const ctxOffline = resolveOfflineSession()
  const ctx = ctxOnline ?? ctxOffline
  const cajaDiarioId = ctx?.cajaDiarioId ?? 0
  const offlineSesion = Boolean(!ctxOnline && ctxOffline)

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
          return { rows: cache.rows, fromCache: true as const }
        }
        throw e
      }
    },
    enabled:
      !!ctx && !ctx.indCierre && !yaEjecutadoHoy && usuarioId > 0 && oficinaId > 0,
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
    if (draftHydrated.current || yaEjecutadoHoy || usuarioId < 1 || cajaDiarioId < 1) return
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
    if (draft && draftHasMontos(draft)) {
      const next: Record<number, RowEdit> = {}
      for (const [k, v] of Object.entries(draft.edits)) {
        const id = Number(k)
        if (Number.isFinite(id) && v) next[id] = v
      }
      setEdits(next)
      if (draft.filtro) setFiltro(draft.filtro)
      setDraftBanner(
        `Borrador recuperado (${formatDraftAge(draft.updatedAt)}). Puede continuar donde lo dejó.`,
      )
    }
  }, [usuarioId, oficinaId, cajaDiarioId, yaEjecutadoHoy])

  // Autoguardado al navegar a otro módulo / minimizar.
  useEffect(() => {
    if (yaEjecutadoHoy || usuarioId < 1 || cajaDiarioId < 1) return
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
  }, [edits, filtro, usuarioId, oficinaId, cajaDiarioId, yaEjecutadoHoy])

  const getEdit = useCallback(
    (row: CreditoGestorPendienteRow): RowEdit => edits[row.creditoId] ?? defaultEdit(),
    [edits],
  )

  const patchEdit = (
    creditoId: number,
    patch: Partial<RowEdit>,
    row: CreditoGestorPendienteRow,
  ) => {
    setDraftBanner(null)
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
      if (fieldMode && vistaCampo === 'cobro' && getEdit(r).montoPagar <= 0) return false
      return true
    })
  }, [carteraRows, filtro, fieldMode, vistaCampo, getEdit])

  const resumen = useMemo(() => {
    let conCobro = 0
    let total = 0
    const porMetodo = new Map<number, { label: string; monto: number; n: number }>()
    for (const row of carteraRows ?? []) {
      const e = getEdit(row)
      if (e.montoPagar <= 0) continue
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
      impagos: Math.max(0, totalFilas - conCobro),
      total,
      porMetodo: [...porMetodo.values()],
      totalFilas,
    }
  }, [carteraRows, getEdit, tipoPagoOptions])

  const validarPlanilla = (): string | null => {
    for (const row of carteraRows ?? []) {
      const e = getEdit(row)
      if (e.montoPagar <= 0) continue
      if (TIPOS_PAGO_DIGITAL.has(e.tipoPagoId) && !e.fechaHoraTrans?.trim()) {
        return `Ingrese la fecha/hora de transferencia para ${row.personaNombre} (crédito ${row.creditoId}).`
      }
    }
    return null
  }

  const buildPlanillaPayload = () =>
    (carteraRows ?? [])
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

  const finalizarExito = (mensaje: string) => {
    markCobroBloqueEjecutadoHoy()
    clearCobroBloqueDraft({ usuarioId, cajaDiarioId })
    clearCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
    clearCobroBloqueCarteraCache({ usuarioId, cajaDiarioId })
    setPendingSync(false)
    setEdits({})
    message.success(mensaje)
    navigate('/caja/diario')
  }

  const encolarPorOffline = (planilla: ReturnType<typeof buildPlanillaPayload>) => {
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
      'Sin conexión: la planilla quedó en cola en este dispositivo. Se enviará al recuperar red.',
    )
  }

  const procesar = useMutation({
    mutationFn: async () => {
      if (!ctx || ctx.indCierre) throw new Error('No hay caja diario abierta.')
      const err = validarPlanilla()
      if (err) throw new Error(err)

      const planilla = buildPlanillaPayload()

      if (!isBrowserOnline()) {
        encolarPorOffline(planilla)
        return { queued: true as const }
      }

      try {
        const r = await cobrarPlanillaBloque({
          oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          planilla,
        })
        return { queued: false as const, result: r }
      } catch (e) {
        if (isLikelyNetworkError(e)) {
          encolarPorOffline(planilla)
          return { queued: true as const }
        }
        throw e
      }
    },
    onSuccess: (r) => {
      if (r.queued) return
      finalizarExito(r.result.mensaje)
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error'),
  })

  useEffect(() => {
    if (!online || yaEjecutadoHoy || usuarioId < 1 || cajaDiarioId < 1) return
    if (syncingRef.current) return
    const pending = loadCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
    if (!pending) return

    syncingRef.current = true
    setSyncingQueue(true)
    void (async () => {
      try {
        const r = await cobrarPlanillaBloque({
          oficinaId: pending.oficinaId,
          cajaDiarioId: pending.cajaDiarioId,
          planilla: pending.planilla,
        })
        // No cancelar el éxito: si el request ya fue a BD, hay que cerrar borrador/cola.
        finalizarExito(r.mensaje || 'Planilla sincronizada al recuperar red.')
      } catch (e) {
        if (isLikelyNetworkError(e)) {
          message.warning('Aún sin red estable. La cola se reintentará al conectar.')
        } else {
          clearCobroBloquePendingProcess({ usuarioId, cajaDiarioId })
          setPendingSync(false)
          message.error(
            e instanceof ApiError
              ? e.message
              : e instanceof Error
                ? e.message
                : 'No se pudo sincronizar la planilla en cola.',
          )
        }
      } finally {
        syncingRef.current = false
        setSyncingQueue(false)
      }
    })()
    // Flush solo al recuperar red / cambiar caja.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [online, yaEjecutadoHoy, usuarioId, cajaDiarioId])

  const confirmarProcesar = () => {
    if (yaEjecutadoHoy) {
      message.warning('El cobro en bloque ya fue ejecutado hoy.')
      return
    }
    if (resumen.totalFilas < 1) {
      message.warning('No hay clientes en la planilla para procesar.')
      return
    }
    const err = validarPlanilla()
    if (err) {
      message.warning(err)
      return
    }

    const content =
      resumen.conCobro > 0
        ? `¿Registrar el cobro de ${resumen.conCobro} cliente(s) por S/ ${formatMoney(resumen.total)} y marcar el resto como impagos?`
        : '¿Cerrar la planilla registrando a TODOS los clientes como IMPAGOS (S/ 0.00)?'

    Modal.confirm({
      title: 'Procesar planilla',
      content,
      okText: 'Sí, procesar',
      cancelText: 'Cancelar',
      onOk: () => procesar.mutateAsync(),
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
  ]

  const sinCaja = !yaEjecutadoHoy && ((!ctx && !sesionQuery.isLoading) || Boolean(ctx?.indCierre))

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
        {fieldMode ? (
          <div className="cobro-bloque-command__vistas" role="tablist" aria-label="Vista de planilla">
            {(
              [
                { id: 'todos' as const, label: 'Todos' },
                { id: 'mora' as const, label: 'Mora' },
                { id: 'cobro' as const, label: 'Con cobro' },
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
          {filas.length}/{resumen.totalFilas}
        </span>
        <span className="cobro-bloque-command__chip cobro-bloque-command__chip--accent">
          {resumen.conCobro} · S/ {formatMoney(resumen.total)}
        </span>
      </div>
    </div>
  )

  const actionDock = !yaEjecutadoHoy && !sinCaja ? (
    <div
      className="cobro-bloque-dock"
      role="region"
      aria-label="Acciones de planilla"
      style={
        dockBox
          ? { left: dockBox.left, width: dockBox.width }
          : undefined
      }
    >
      <div className="cobro-bloque-dock__inner">
        <div className="cobro-bloque-dock__stats">
          <div>
            <Text type="secondary">A cobrar</Text>
            <strong>
              {resumen.conCobro} · S/ {formatMoney(resumen.total)}
            </strong>
          </div>
          <div className="cobro-bloque-dock__impagos">
            <Text type="secondary">Impagos</Text>
            <strong>{resumen.impagos}</strong>
          </div>
        </div>
        <Space wrap className="cobro-bloque-dock__actions">
          {scrolledDown ? (
            <Button icon={<ArrowUpOutlined />} onClick={scrollPlanillaTop}>
              Arriba
            </Button>
          ) : null}
          <Button
            type="primary"
            size="large"
            icon={<CheckOutlined />}
            loading={procesar.isPending}
            disabled={resumen.totalFilas < 1}
            onClick={confirmarProcesar}
          >
            Procesar planilla
          </Button>
        </Space>
      </div>
    </div>
  ) : null

  const resumenBar = (
    <div className="credix-cobro-bloque-resumen" role="region" aria-label="Resumen operativo">
      <div>
        <Text type="secondary">Con cobro</Text>
        <div>
          <strong>
            {resumen.conCobro} · S/ {formatMoney(resumen.total)}
          </strong>
        </div>
      </div>
      <div>
        <Text type="secondary">Impagos (S/ 0)</Text>
        <div>
          <strong>{resumen.impagos}</strong>
        </div>
      </div>
      <div>
        <Text type="secondary">Total cobrado</Text>
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
      title="Cobro en bloque"
      subtitle={
        fieldMode
          ? undefined
          : 'Planilla digital de cobro diario: cobro rápido en campo, voucher con un toque y borrador automático.'
      }
      breadcrumb={
        fieldMode
          ? undefined
          : [
              { title: <Link to="/inicio">Inicio</Link> },
              { title: <Link to="/caja">Caja</Link> },
              { title: <Link to="/caja/diario">Diario</Link> },
              { title: 'Cobro en bloque' },
            ]
      }
    >
      {yaEjecutadoHoy ? (
        <Alert
          type="warning"
          showIcon
          message="Cobro en bloque ya ejecutado hoy"
          description="No está permitido ingresar nuevamente el mismo día para evitar alterar los impagos."
          action={
            <Button type="primary" onClick={() => navigate('/caja/diario')}>
              Ir a caja diario
            </Button>
          }
        />
      ) : sinCaja ? (
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
              description="Orden idéntico al PDF de cobro diario (vencidos primero). Los montos tipados se guardan en este dispositivo si cambia de módulo; al día siguiente el borrador caduca solo y nunca se registra solo en el servidor. Al procesar se confirman cobros + impagos."
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
              description="Puede seguir tipando montos sobre la última planilla. Al procesar se encola en este dispositivo y se envía al recuperar red."
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
              message={syncingQueue ? 'Sincronizando planilla…' : 'Planilla en cola (pendiente de envío)'}
              description="Hay un lote listo para enviar. No cierre sesión hasta que se confirme el envío."
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

          {toolbar}
          {resumenBar}

          <CredixPanel
            title={fieldMode ? `Ruta (${filas.length})` : `Planilla de campo (${filas.length})`}
            className={fieldMode ? 'cobro-bloque-panel--field' : undefined}
          >
            {carteraQuery.isError ? (
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
                        onUbicacionGuardada={(personaId, lat, lng) => {
                          queryClient.setQueryData(
                            carteraQueryKey,
                            (prev: { rows: CreditoGestorPendienteRow[]; fromCache: boolean } | undefined) => {
                              if (!prev) return prev
                              return {
                                ...prev,
                                rows: prev.rows.map((r) =>
                                  r.personaId === personaId
                                    ? { ...r, latitud: lat, longitud: lng }
                                    : r,
                                ),
                              }
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
                      <div>
                        {(() => {
                          const punto = {
                            direccion: row.direccion,
                            latitud: row.latitud,
                            longitud: row.longitud,
                          }
                          const href = clienteMapaHref(punto)
                          const label = clienteMapaLabel(punto)
                          if (!href || !label) return null
                          return (
                            <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                              <a
                                href={href}
                                target="_blank"
                                rel="noreferrer"
                                title={clienteMapaTitle(punto)}
                              >
                                <EnvironmentOutlined /> {label}
                                {clienteTieneGps(punto) ? ' · GPS' : ''}
                              </a>
                            </Text>
                          )
                        })()}
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
                      </div>
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
