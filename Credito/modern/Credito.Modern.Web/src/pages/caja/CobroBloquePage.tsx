import { useCallback, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Checkbox,
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
  SearchOutlined,
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
import type { EstadoPlanPagoCuota } from '../../types/api'
import '../../styles/cobro-bloque.css'

const { Text } = Typography

/** Paridad legacy `tiposPagoDigital = [2, 3, 4, 5]`. */
const TIPOS_PAGO_DIGITAL = new Set([2, 3, 4, 5])

type RowEdit = {
  montoPagar: number
  tipoPagoId: number
  fechaHoraTrans: string
  cuotasSeleccionadas: number[]
}

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
        Marque cuotas para sumar el monto (o digite libre arriba). Máx. S/{' '}
        {formatMoney(deudaMax)}.
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
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [filtro, setFiltro] = useState('')
  const [edits, setEdits] = useState<Record<number, RowEdit>>({})
  const yaEjecutadoHoy = isCobroBloqueEjecutadoHoy()

  const sesionQuery = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0 && !yaEjecutadoHoy,
    retry: false,
  })

  const carteraQuery = useQuery({
    queryKey: ['caja-creditos-gestor-des', 'cobro-bloque'],
    queryFn: fetchCreditosGestorDesembolsados,
    enabled: !!sesionQuery.data && !sesionQuery.data.indCierre && !yaEjecutadoHoy,
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

  const getEdit = useCallback(
    (row: CreditoGestorPendienteRow): RowEdit => {
      const existing = edits[row.creditoId]
      if (existing) return existing
      return {
        montoPagar: 0,
        tipoPagoId: 1,
        fechaHoraTrans: nowDatetimeLocal(),
        cuotasSeleccionadas: [],
      }
    },
    [edits],
  )

  const patchEdit = (
    creditoId: number,
    patch: Partial<RowEdit>,
    row: CreditoGestorPendienteRow,
  ) => {
    setEdits((prev) => {
      const base = prev[creditoId] ?? {
        montoPagar: 0,
        tipoPagoId: 1,
        fechaHoraTrans: nowDatetimeLocal(),
        cuotasSeleccionadas: [],
      }
      const next = { ...base, ...patch }
      if (next.montoPagar > row.deudaPendiente) {
        next.montoPagar = row.deudaPendiente
      }
      if (next.montoPagar < 0) next.montoPagar = 0
      return { ...prev, [creditoId]: next }
    })
  }

  const filas = useMemo(() => {
    const raw = carteraQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return raw
    return raw.filter(
      (r) =>
        r.personaNombre.toLowerCase().includes(q) ||
        r.personaCodigo.toLowerCase().includes(q) ||
        String(r.creditoId).includes(q),
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

  const procesar = useMutation({
    mutationFn: async () => {
      const ctx = sesionQuery.data
      if (!ctx || ctx.indCierre) {
        throw new Error('No hay caja diario abierta.')
      }
      const err = validarPlanilla()
      if (err) throw new Error(err)

      const planilla = (carteraQuery.data ?? [])
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

      return cobrarPlanillaBloque({
        oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        planilla,
      })
    },
    onSuccess: (r) => {
      markCobroBloqueEjecutadoHoy()
      message.success(r.mensaje)
      setEdits({})
      navigate('/caja/diario')
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error'),
  })

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

  const columns: ColumnsType<CreditoGestorPendienteRow> = [
    {
      title: 'Código',
      dataIndex: 'personaCodigo',
      width: 88,
    },
    {
      title: 'Cliente',
      dataIndex: 'personaNombre',
      ellipsis: true,
    },
    {
      title: 'Crédito',
      dataIndex: 'creditoId',
      width: 80,
      align: 'center',
    },
    {
      title: 'Venc.',
      dataIndex: 'fechaVencimiento',
      width: 100,
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Mora',
      dataIndex: 'importeMora',
      width: 90,
      align: 'right',
      render: (v: number) => formatMoney(v ?? 0),
    },
    {
      title: 'Deuda',
      dataIndex: 'deudaPendiente',
      width: 100,
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

  const ctx = sesionQuery.data
  const sinCaja = !yaEjecutadoHoy && (sesionQuery.isError || !ctx || ctx.indCierre)

  return (
    <CredixPage
      className="cobro-bloque-page"
      title="Cobro en bloque"
      subtitle="Planilla de cobranza diaria: digite montos o seleccione cuotas. Todo o nada al procesar (incluye impagos)."
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
            style={{ marginBottom: 12 }}
            message="Cartera del gestor en sesión"
            description="Solo aparecen créditos desembolsados registrados por su usuario (mismo criterio del legado). Expanda una fila para marcar cuotas."
          />

          <div className="credix-cobro-bloque-toolbar">
            <Input
              allowClear
              prefix={<SearchOutlined />}
              placeholder="Filtrar por cliente, código o crédito…"
              value={filtro}
              onChange={(e) => setFiltro(e.target.value)}
              style={{ maxWidth: 420 }}
              aria-label="Filtrar planilla"
            />
            <Space wrap>
              <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/caja/diario')}>
                Volver a caja
              </Button>
              <Button
                type="primary"
                icon={<CheckOutlined />}
                loading={procesar.isPending}
                disabled={resumen.totalFilas < 1}
                onClick={confirmarProcesar}
              >
                Procesar planilla
              </Button>
            </Space>
          </div>

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
              <Text type="secondary">Impagos / visitas (S/ 0.00)</Text>
              <div>
                <strong>{resumen.impagos}</strong>
              </div>
            </div>
            <div>
              <Text type="secondary">Total cobrado</Text>
              <div className="credix-cobro-bloque-total">
                S/ {formatMoney(resumen.total)}
              </div>
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

          <CredixPanel title={`Planilla (${filas.length})`}>
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
        </>
      )}
    </CredixPage>
  )
}
