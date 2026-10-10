import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Checkbox,
  Grid,
  Input,
  InputNumber,
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
  PhoneOutlined,
  SearchOutlined,
  ThunderboltOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  cobrarPlanillaBloque,
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
import '../../styles/cobro-bloque.css'

const { Text } = Typography

/** Paridad legacy `tiposPagoDigital = [2, 3, 4, 5]`. */
const TIPOS_PAGO_DIGITAL = new Set([2, 3, 4, 5])

type RowEdit = CobroBloqueRowDraft

function nowDatetimeLocal(): string {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

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

  const sesionQuery = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0 && !yaEjecutadoHoy,
    retry: false,
  })

  const ctxOnline = sesionQuery.data

  const resolveOfflineSession = useCallback((): CajaSession | null => {
    if (ctxOnline || usuarioId < 1) return null
    const fecha = fechaOperacionLocal()
    const prefix = `credix.cobroBloqueCartera.v1:${usuarioId}:`
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
  }, [ctxOnline, usuarioId])

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

  const carteraQuery = useQuery({
    queryKey: ['caja-creditos-gestor-des', 'cobro-bloque', cajaDiarioId],
    queryFn: async () => {
      try {
        const rows = await fetchCreditosGestorDesembolsados()
        if (ctx) {
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
        setUsingCache(false)
        return rows
      } catch (e) {
        if (!isLikelyNetworkError(e)) throw e
        const cache = loadCobroBloqueCarteraCache({ usuarioId, cajaDiarioId })
        if (cache?.rows?.length) {
          setUsingCache(true)
          return cache.rows
        }
        throw e
      }
    },
    enabled: !!ctx && !ctx.indCierre && !yaEjecutadoHoy && usuarioId > 0,
  })

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
      return { ...prev, [creditoId]: next }
    })
  }

  const aplicarCuotaSugerida = (row: CreditoGestorPendienteRow) => {
    const sug = Math.min(Math.max(0, row.cuotaSugerida || 0), row.deudaPendiente)
    if (sug <= 0) {
      message.info('No hay cuota sugerida para este crédito.')
      return
    }
    patchEdit(row.creditoId, { montoPagar: sug, cuotasSeleccionadas: [] }, row)
  }

  const filas = useMemo(() => {
    const raw = carteraQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return raw
    return raw.filter(
      (r) =>
        r.personaNombre.toLowerCase().includes(q) ||
        r.personaCodigo.toLowerCase().includes(q) ||
        (r.celular ?? '').toLowerCase().includes(q) ||
        (r.direccion ?? '').toLowerCase().includes(q) ||
        String(r.creditoId).includes(q) ||
        String(r.orden ?? '').includes(q),
    )
  }, [carteraQuery.data, filtro])

  const resumen = useMemo(() => {
    let conCobro = 0
    let total = 0
    const porMetodo = new Map<number, { label: string; monto: number; n: number }>()
    for (const row of carteraQuery.data ?? []) {
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
    const totalFilas = carteraQuery.data?.length ?? 0
    return {
      conCobro,
      impagos: Math.max(0, totalFilas - conCobro),
      total,
      porMetodo: [...porMetodo.values()],
      totalFilas,
    }
  }, [carteraQuery.data, getEdit, tipoPagoOptions])

  const validarPlanilla = (): string | null => {
    for (const row of carteraQuery.data ?? []) {
      const e = getEdit(row)
      if (e.montoPagar <= 0) continue
      if (TIPOS_PAGO_DIGITAL.has(e.tipoPagoId) && !e.fechaHoraTrans?.trim()) {
        return `Ingrese la fecha/hora de transferencia para ${row.personaNombre} (crédito ${row.creditoId}).`
      }
    }
    return null
  }

  const buildPlanillaPayload = () =>
    (carteraQuery.data ?? [])
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

    let cancelled = false
    syncingRef.current = true
    setSyncingQueue(true)
    void (async () => {
      try {
        const r = await cobrarPlanillaBloque({
          oficinaId: pending.oficinaId,
          cajaDiarioId: pending.cajaDiarioId,
          planilla: pending.planilla,
        })
        if (cancelled) return
        finalizarExito(r.mensaje || 'Planilla sincronizada al recuperar red.')
      } catch (e) {
        if (cancelled) return
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
        if (!cancelled) setSyncingQueue(false)
      }
    })()

    return () => {
      cancelled = true
    }
    // Flush solo al recuperar red / cambiar caja; finalizarExito es estable en este ciclo.
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
      title: 'Nro',
      key: 'nro',
      width: 52,
      align: 'right',
      render: (_, __, index) => index + 1,
    },
    {
      title: 'Ord.',
      dataIndex: 'orden',
      width: 56,
      align: 'right',
      render: (v: number | null) => v ?? '—',
    },
    {
      title: 'Cliente',
      dataIndex: 'personaNombre',
      ellipsis: true,
      render: (_, row) => (
        <div className="cobro-bloque-cliente-cell">
          <strong>{row.personaNombre}</strong>
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
      width: 110,
      render: (v: string | null) =>
        v ? (
          <a href={`tel:${v.replace(/\s+/g, '')}`}>{v}</a>
        ) : (
          <Text type="secondary">—</Text>
        ),
    },
    {
      title: 'Venc.',
      dataIndex: 'fechaVencimiento',
      width: 96,
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Cuota sug.',
      dataIndex: 'cuotaSugerida',
      width: 100,
      align: 'right',
      render: (v: number, row) => (
        <Button type="link" size="small" onClick={() => aplicarCuotaSugerida(row)}>
          {formatMoney(v ?? 0)}
        </Button>
      ),
    },
    {
      title: 'Deuda',
      dataIndex: 'deudaPendiente',
      width: 96,
      align: 'right',
      render: (v: number) => <strong>{formatMoney(v)}</strong>,
    },
    {
      title: 'Monto a cobrar',
      key: 'monto',
      width: 130,
      render: (_, row) => {
        const e = getEdit(row)
        return (
          <InputNumber
            min={0}
            max={row.deudaPendiente}
            step={0.01}
            value={e.montoPagar}
            style={{ width: '100%' }}
            onChange={(v) =>
              patchEdit(
                row.creditoId,
                { montoPagar: Number(v) || 0, cuotasSeleccionadas: [] },
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
      width: 140,
      render: (_, row) => {
        const e = getEdit(row)
        return (
          <Select
            value={e.tipoPagoId}
            options={tipoPagoOptions}
            style={{ width: '100%' }}
            onChange={(v) => patchEdit(row.creditoId, { tipoPagoId: v }, row)}
          />
        )
      },
    },
    {
      title: 'Fecha/hora digital',
      key: 'fecha',
      width: 190,
      render: (_, row) => {
        const e = getEdit(row)
        if (!TIPOS_PAGO_DIGITAL.has(e.tipoPagoId)) {
          return <Text type="secondary">—</Text>
        }
        return (
          <Input
            type="datetime-local"
            value={e.fechaHoraTrans}
            status={!e.fechaHoraTrans ? 'error' : undefined}
            onChange={(ev) =>
              patchEdit(row.creditoId, { fechaHoraTrans: ev.target.value }, row)
            }
          />
        )
      },
    },
  ]

  const sinCaja = !yaEjecutadoHoy && ((!ctx && !sesionQuery.isLoading) || Boolean(ctx?.indCierre))

  const toolbar = (
    <div className="credix-cobro-bloque-toolbar">
      <Input
        allowClear
        prefix={<SearchOutlined />}
        placeholder="Buscar cliente, celular, dirección, crédito u ord…"
        value={filtro}
        onChange={(e) => setFiltro(e.target.value)}
        style={{ maxWidth: fieldMode ? '100%' : 420 }}
        aria-label="Filtrar planilla"
      />
      <Space wrap>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/caja/diario')}>
          Volver
        </Button>
        <Button
          type="primary"
          icon={<CheckOutlined />}
          loading={procesar.isPending}
          disabled={resumen.totalFilas < 1}
          onClick={confirmarProcesar}
        >
          Procesar
        </Button>
      </Space>
    </div>
  )

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
      className={`cobro-bloque-page${fieldMode ? ' cobro-bloque-page--field' : ''}`}
      title="Cobro en bloque"
      subtitle="Planilla digital de cobro diario: mismo orden del reporte, cobro rápido en campo y borrador automático."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: <Link to="/caja/diario">Diario</Link> },
        { title: 'Cobro en bloque' },
      ]}
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
          <Alert
            type="info"
            showIcon
            className="cobro-bloque-page__hint"
            message="Ruta digital (reemplazo del reporte impreso / PWA externo)"
            description="Orden idéntico al PDF de cobro diario (vencidos primero). Los montos tipados se guardan en este dispositivo si cambia de módulo; al día siguiente el borrador caduca solo y nunca se registra solo en el servidor. Al procesar se confirman cobros + impagos."
          />

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

          <CredixPanel title={`Planilla campo (${filas.length}) · orden cobro diario`}>
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
                  <Text type="secondary">Sin clientes en la planilla.</Text>
                ) : (
                  filas.map((row, index) => {
                    const e = getEdit(row)
                    const enMora = row.diasAtrazo > 0
                    return (
                      <article
                        key={row.creditoId}
                        className={`cobro-bloque-card${enMora ? ' cobro-bloque-card--mora' : ''}${e.montoPagar > 0 ? ' cobro-bloque-card--cobro' : ''}`}
                      >
                        <header className="cobro-bloque-card__head">
                          <div>
                            <span className="cobro-bloque-card__nro">
                              #{index + 1}
                              {row.orden != null ? ` · Ord. ${row.orden}` : ''}
                            </span>
                            <h3 className="cobro-bloque-card__name">{row.personaNombre}</h3>
                            <Text type="secondary">
                              Crédito {row.creditoId} · venc. {formatFecha(row.fechaVencimiento)}
                              {enMora ? ` · ${row.diasAtrazo}d atraso` : ''}
                            </Text>
                          </div>
                          <div className="cobro-bloque-card__deuda">
                            <Text type="secondary">Deuda</Text>
                            <strong>S/ {formatMoney(row.deudaPendiente)}</strong>
                          </div>
                        </header>

                        <div className="cobro-bloque-card__meta">
                          {row.celular ? (
                            <a
                              className="cobro-bloque-card__link"
                              href={`tel:${row.celular.replace(/\s+/g, '')}`}
                            >
                              <PhoneOutlined /> {row.celular}
                            </a>
                          ) : null}
                          {row.direccion ? (
                            <span className="cobro-bloque-card__dir">
                              <EnvironmentOutlined /> {row.direccion}
                            </span>
                          ) : null}
                        </div>

                        <div className="cobro-bloque-card__actions">
                          <Button
                            size="small"
                            icon={<ThunderboltOutlined />}
                            onClick={() => aplicarCuotaSugerida(row)}
                          >
                            Cuota S/ {formatMoney(row.cuotaSugerida)}
                          </Button>
                          <InputNumber
                            className="cobro-bloque-card__monto"
                            min={0}
                            max={row.deudaPendiente}
                            step={0.01}
                            value={e.montoPagar}
                            inputMode="decimal"
                            onChange={(v) =>
                              patchEdit(
                                row.creditoId,
                                { montoPagar: Number(v) || 0, cuotasSeleccionadas: [] },
                                row,
                              )
                            }
                          />
                          <Select
                            value={e.tipoPagoId}
                            options={tipoPagoOptions}
                            className="cobro-bloque-card__tipo"
                            onChange={(v) => patchEdit(row.creditoId, { tipoPagoId: v }, row)}
                          />
                        </div>

                        {TIPOS_PAGO_DIGITAL.has(e.tipoPagoId) ? (
                          <Input
                            type="datetime-local"
                            value={e.fechaHoraTrans}
                            status={!e.fechaHoraTrans ? 'error' : undefined}
                            onChange={(ev) =>
                              patchEdit(
                                row.creditoId,
                                { fechaHoraTrans: ev.target.value },
                                row,
                              )
                            }
                          />
                        ) : null}

                        <details className="cobro-bloque-card__cuotas">
                          <summary>Seleccionar cuotas</summary>
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
                        </details>
                      </article>
                    )
                  })
                )}
              </div>
            ) : (
              <CredixDataTable
                rowKey="creditoId"
                loading={carteraQuery.isLoading}
                columns={columns}
                dataSource={filas}
                pagination={{ pageSize: 50, showSizeChanger: true }}
                size="small"
                expandable={{
                  expandedRowRender: (row) => {
                    const e = getEdit(row)
                    return (
                      <div>
                        {row.direccion ? (
                          <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                            <EnvironmentOutlined /> {row.direccion}
                          </Text>
                        ) : null}
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

          {fieldMode ? (
            <div className="cobro-bloque-sticky">
              <div>
                <Text type="secondary">A cobrar</Text>
                <strong>
                  {resumen.conCobro} · S/ {formatMoney(resumen.total)}
                </strong>
              </div>
              <Button
                type="primary"
                size="large"
                icon={<CheckOutlined />}
                loading={procesar.isPending}
                disabled={resumen.totalFilas < 1}
                onClick={confirmarProcesar}
              >
                Procesar
              </Button>
            </div>
          ) : null}
        </>
      )}
    </CredixPage>
  )
}
