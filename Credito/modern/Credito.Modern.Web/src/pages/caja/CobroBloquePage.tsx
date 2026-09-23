import { useCallback, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Input,
  InputNumber,
  Select,
  Space,
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
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, CredixPanel } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

type RowEdit = {
  montoPagar: number
  tipoPagoId: number
  fechaHoraTrans: string
}

function nowDatetimeLocal(): string {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function CobroBloquePage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [filtro, setFiltro] = useState('')
  const [edits, setEdits] = useState<Record<number, RowEdit>>({})

  const sesionQuery = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const carteraQuery = useQuery({
    queryKey: ['caja-creditos-gestor-des', 'cobro-bloque'],
    queryFn: fetchCreditosGestorDesembolsados,
    enabled: !!sesionQuery.data && !sesionQuery.data.indCierre,
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
      }
    },
    [edits],
  )

  const patchEdit = (creditoId: number, patch: Partial<RowEdit>, row: CreditoGestorPendienteRow) => {
    setEdits((prev) => {
      const base = prev[creditoId] ?? {
        montoPagar: 0,
        tipoPagoId: 1,
        fechaHoraTrans: nowDatetimeLocal(),
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
    }
  }, [carteraQuery.data, getEdit, tipoPagoOptions])

  const procesar = useMutation({
    mutationFn: async () => {
      const ctx = sesionQuery.data
      if (!ctx || ctx.indCierre) {
        throw new Error('No hay caja diario abierta.')
      }
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
      message.success(r.mensaje)
      setEdits({})
      void carteraQuery.refetch()
      navigate('/caja/diario')
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error'),
  })

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
            onChange={(v) => patchEdit(row.creditoId, { montoPagar: Number(v) || 0 }, row)}
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
        if (e.tipoPagoId <= 1) {
          return <Text type="secondary">—</Text>
        }
        return (
          <Input
            type="datetime-local"
            value={e.fechaHoraTrans}
            onChange={(ev) =>
              patchEdit(row.creditoId, { fechaHoraTrans: ev.target.value }, row)
            }
          />
        )
      },
    },
  ]

  const ctx = sesionQuery.data
  const sinCaja = sesionQuery.isError || !ctx || ctx.indCierre

  return (
    <CredixPage
      title="Cobro en bloque"
      subtitle="Planilla de cobranza diaria: digite montos y use Enter/Tab. Todo o nada al procesar."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: <Link to="/caja/diario">Diario</Link> },
        { title: 'Cobro en bloque' },
      ]}
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
                disabled={resumen.conCobro < 1}
                onClick={() => {
                  if (resumen.conCobro < 1) {
                    message.warning('Ingrese al menos un monto mayor a cero.')
                    return
                  }
                  void procesar.mutateAsync()
                }}
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
              />
            )}
          </CredixPanel>
        </>
      )}
    </CredixPage>
  )
}
