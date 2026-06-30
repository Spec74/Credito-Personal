import { useMemo, useState, type Key } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Input,
  InputNumber,
  Radio,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { CajaDrawer } from '../../../components/caja/CajaDrawer'
import { CajaCuotasTable } from '../../../components/caja/CajaCuotasTable'
import {
  loadCuotasCobranzaGrid,
  type CuotaCobranzaRow,
} from '../../../components/caja/cuotasGridMerge'
import { isCuotaSelectable } from '../../../components/caja/cuotaRowStyle'
import { fetchCobroDiario } from '../../../api/creditoPlanes'
import { fetchValoresTabla } from '../../../api/maestros'
import {
  completarImpagos,
  fetchTieneCxcPendiente,
  pagarCuotaImporteLibre,
  pagarCuotas,
} from '../../../api/cajaDiario'
import type { RptCobroDiarioRow } from '../../../types/api'
import { formatFecha } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'
import { maybeDownloadCajaTicket } from './cajaPagoHelpers'
import type { CajaSession } from './types'
import { errMsg } from './types'

const { Text } = Typography

type ModoPagoBloque = 'libre' | 'cuotas'

type EstadoFila = {
  modo: ModoPagoBloque
  monto: number | null
  tipoPagoId: number
  fechaTransferencia: string
  selectedKeys: Key[]
}

type ResultadoProceso = {
  pagosOk: number
  impagos: number
  omitidos: number
}

const estadoDefault: EstadoFila = {
  modo: 'libre',
  monto: null,
  tipoPagoId: 1,
  fechaTransferencia: '',
  selectedKeys: [],
}

function normalizar(value: unknown): string {
  return String(value ?? '')
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .trim()
}

function fechaMs(value: string | null | undefined): number {
  if (!value) {
    return Number.MAX_SAFE_INTEGER
  }
  const ms = new Date(value).getTime()
  return Number.isNaN(ms) ? Number.MAX_SAFE_INTEGER : ms
}

function ordenarCobroDiario(rows: RptCobroDiarioRow[]): RptCobroDiarioRow[] {
  return [...rows].sort((a, b) => {
    const venc = fechaMs(a.fechaVencimiento) - fechaMs(b.fechaVencimiento)
    if (venc !== 0) {
      return venc
    }
    return (a.orden ?? a.nro ?? a.creditoId) - (b.orden ?? b.nro ?? b.creditoId)
  })
}

function montoCuotas(rows: CuotaCobranzaRow[], keys: Key[]): number {
  return rows
    .filter((c) => c.planPagoId != null && keys.includes(c.planPagoId))
    .reduce((sum, c) => sum + (c.pagoCuota ?? c.cuota ?? 0), 0)
}

function CobranzaBloqueCuotasPanel({
  creditoId,
  selectedKeys,
  onSelectionChange,
}: {
  creditoId: number
  selectedKeys: Key[]
  onSelectionChange: (keys: Key[], total: number) => void
}) {
  const cuotasQuery = useQuery({
    queryKey: ['cobranza-bloque-cuotas', creditoId],
    queryFn: () => loadCuotasCobranzaGrid(creditoId),
    staleTime: 60_000,
  })

  return (
    <div className="cobranza-bloque-cuotas-panel">
      <CajaCuotasTable
        data={cuotasQuery.data ?? []}
        loading={cuotasQuery.isLoading}
        selectedKeys={selectedKeys}
        onSelectionChange={(keys) =>
          onSelectionChange(keys, montoCuotas(cuotasQuery.data ?? [], keys))
        }
        showLegend
        pageSize={12}
        compact
      />
    </div>
  )
}

function CobranzaBloqueDrawer({
  open,
  ctx,
  usuarioId,
  onClose,
  onChanged,
}: {
  open: boolean
  ctx: CajaSession
  usuarioId: number
  onClose: () => void
  onChanged: () => void
}) {
  const [filtro, setFiltro] = useState('')
  const [descargarTickets, setDescargarTickets] = useState(false)
  const [estadoPorCredito, setEstadoPorCredito] = useState<
    Record<number, EstadoFila>
  >({})

  const cobroQuery = useQuery({
    queryKey: ['caja-cobranza-bloque-cobro-diario', ctx.oficinaId, usuarioId],
    queryFn: () => fetchCobroDiario({ oficinaId: ctx.oficinaId, usuarioId }),
    enabled: open && ctx.oficinaId > 0 && usuarioId > 0,
  })

  const tiposPagoQuery = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    enabled: open,
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

  const rows = useMemo(
    () => ordenarCobroDiario(cobroQuery.data ?? []),
    [cobroQuery.data],
  )

  const rowsFiltradas = useMemo(() => {
    const q = normalizar(filtro)
    if (!q) {
      return rows
    }
    const tokens = q.split(/\s+/).filter(Boolean)
    return rows.filter((r) => {
      const blob = normalizar([
        r.creditoId,
        r.cliente,
        r.celular,
        r.direccion,
        r.negocio,
        r.formaPago,
      ].join(' '))
      return tokens.every((t) => blob.includes(t))
    })
  }, [rows, filtro])

  const getEstado = (creditoId: number): EstadoFila =>
    estadoPorCredito[creditoId] ?? estadoDefault

  const patchEstado = (creditoId: number, patch: Partial<EstadoFila>) => {
    setEstadoPorCredito((prev) => ({
      ...prev,
      [creditoId]: {
        ...(prev[creditoId] ?? estadoDefault),
        ...patch,
      },
    }))
  }

  const resumen = useMemo(() => {
    let conMonto = 0
    let sinMonto = 0
    let total = 0
    for (const row of rows) {
      const monto = getEstado(row.creditoId).monto ?? 0
      if (monto > 0) {
        conMonto += 1
        total += monto
      } else {
        sinMonto += 1
      }
    }
    return { conMonto, sinMonto, total }
    // estadoPorCredito impacta getEstado; recalcular resumen cuando cambie.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows, estadoPorCredito])

  const procesar = useMutation({
    mutationFn: async (): Promise<ResultadoProceso> => {
      const digitalesSinFecha = rows.filter((row) => {
        const estado = getEstado(row.creditoId)
        return (
          (estado.monto ?? 0) > 0 &&
          estado.tipoPagoId > 1 &&
          !estado.fechaTransferencia.trim()
        )
      })
      if (digitalesSinFecha.length > 0) {
        throw new Error(
          `Complete fecha/hora de transacción en ${digitalesSinFecha.length} fila(s) con medio digital.`,
        )
      }

      let pagosOk = 0
      let impagos = 0
      let omitidos = 0

      for (const row of rows) {
        const estado = getEstado(row.creditoId)
        const monto = estado.monto ?? 0
        if (monto <= 0) {
          impagos += 1
          continue
        }

        if (await fetchTieneCxcPendiente(row.creditoId)) {
          omitidos += 1
          continue
        }

        const fechaPagoTransferencia =
          estado.tipoPagoId > 1 ? estado.fechaTransferencia : undefined

        if (estado.modo === 'cuotas' && estado.selectedKeys.length > 0) {
          const result = await pagarCuotas({
            oficinaId: ctx.oficinaId,
            cajaDiarioId: ctx.cajaDiarioId,
            creditoId: row.creditoId,
            listaPlanPagoId: estado.selectedKeys.join(','),
            importeRecibido: monto,
            tipoPagoId: estado.tipoPagoId,
            fechaPagoTransferencia,
            aplicarMoraPostergada: true,
          })
          if (descargarTickets) {
            await maybeDownloadCajaTicket(ctx.oficinaId, result.resultId)
          }
          pagosOk += 1
          continue
        }

        const result = await pagarCuotaImporteLibre({
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          creditoId: row.creditoId,
          importeRecibido: monto,
          tipoPagoId: estado.tipoPagoId,
          fechaPagoTransferencia,
        })
        if (descargarTickets) {
          await maybeDownloadCajaTicket(ctx.oficinaId, result.resultId)
        }
        pagosOk += 1
      }

      if (impagos > 0) {
        await completarImpagos({
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
        })
      }

      return { pagosOk, impagos, omitidos }
    },
    onSuccess: (r) => {
      message.success(
        `Cobranza en bloque procesada: ${r.pagosOk} pago(s), ${r.impagos} sin monto enviados a impagos${r.omitidos ? `, ${r.omitidos} omitido(s) por CxC` : ''}.`,
      )
      setEstadoPorCredito({})
      void cobroQuery.refetch()
      onChanged()
    },
    onError: (e) => message.error(e instanceof Error ? e.message : errMsg(e)),
  })

  const columns: ColumnsType<RptCobroDiarioRow> = [
    {
      title: 'Orden',
      width: 78,
      fixed: 'left',
      render: (_, row, index) => row.orden ?? row.nro ?? index + 1,
    },
    {
      title: 'Vence',
      dataIndex: 'fechaVencimiento',
      width: 104,
      render: (v: string) => formatFecha(v),
      sorter: (a, b) => fechaMs(a.fechaVencimiento) - fechaMs(b.fechaVencimiento),
      defaultSortOrder: 'ascend',
    },
    {
      title: 'Cliente / crédito',
      width: 260,
      render: (_, row) => (
        <Space direction="vertical" size={0}>
          <Text strong>{row.cliente ?? 'Cliente sin nombre'}</Text>
          <Text type="secondary">
            Crédito {row.creditoId} · {row.formaPago}
          </Text>
        </Space>
      ),
    },
    {
      title: 'Saldo',
      dataIndex: 'saldo',
      width: 98,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cuota',
      dataIndex: 'cuotaTotal',
      width: 98,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Mora',
      dataIndex: 'mora',
      width: 92,
      align: 'right',
      render: (v: number | null, row) => (
        <Text type={(row.diasAtrazo ?? 0) > 0 ? 'danger' : undefined}>
          {formatMoney(v)}
        </Text>
      ),
    },
    {
      title: 'Modo',
      width: 154,
      render: (_, row) => {
        const estado = getEstado(row.creditoId)
        return (
          <Radio.Group
            size="small"
            optionType="button"
            value={estado.modo}
            onChange={(e) =>
              patchEstado(row.creditoId, {
                modo: e.target.value as ModoPagoBloque,
                selectedKeys: e.target.value === 'libre' ? [] : estado.selectedKeys,
              })
            }
            options={[
              { label: 'Libre', value: 'libre' },
              { label: 'Cuotas', value: 'cuotas' },
            ]}
          />
        )
      },
    },
    {
      title: 'Monto cobrado',
      width: 132,
      render: (_, row) => {
        const estado = getEstado(row.creditoId)
        return (
          <InputNumber
            min={0}
            step={0.01}
            value={estado.monto}
            onChange={(monto) => patchEstado(row.creditoId, { monto })}
            placeholder="Impago"
            style={{ width: 118 }}
          />
        )
      },
    },
    {
      title: 'Tipo pago',
      width: 148,
      render: (_, row) => {
        const estado = getEstado(row.creditoId)
        return (
          <Select
            value={estado.tipoPagoId}
            onChange={(tipoPagoId) => patchEstado(row.creditoId, { tipoPagoId })}
            options={tipoPagoOptions}
            loading={tiposPagoQuery.isLoading}
            style={{ width: 132 }}
          />
        )
      },
    },
    {
      title: 'Fecha transacción',
      width: 190,
      render: (_, row) => {
        const estado = getEstado(row.creditoId)
        return estado.tipoPagoId > 1 ? (
          <Input
            type="datetime-local"
            value={estado.fechaTransferencia}
            onChange={(e) =>
              patchEstado(row.creditoId, { fechaTransferencia: e.target.value })
            }
            style={{ width: 176 }}
          />
        ) : (
          <Tag>EFECTIVO</Tag>
        )
      },
    },
  ]

  return (
    <CajaDrawer
      title="Cobranza en bloque"
      open={open}
      onClose={onClose}
      width="min(1280px, 96vw)"
      rootClassName="cobranza-bloque-drawer"
      footer={
        <div className="cobranza-bloque-footer">
          <Space wrap>
            <Text>
              Pagos: <strong>{resumen.conMonto}</strong> · Sin monto/impago:{' '}
              <strong>{resumen.sinMonto}</strong> · Total:{' '}
              <strong>{formatMoney(resumen.total)}</strong>
            </Text>
            <Switch
              checked={descargarTickets}
              onChange={setDescargarTickets}
              checkedChildren="Tickets"
              unCheckedChildren="Sin tickets"
            />
          </Space>
          <Space>
            <Button onClick={onClose}>Cerrar</Button>
            <Button
              type="primary"
              loading={procesar.isPending}
              disabled={rows.length === 0}
              onClick={() => {
                void procesar.mutateAsync()
              }}
            >
              Procesar cobranza
            </Button>
          </Space>
        </div>
      }
    >
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Alert
          type="info"
          showIcon
          message="Lista del cobro diario ordenada por fecha de vencimiento, de antiguo a nuevo."
          description="Ingrese el monto recibido por cliente. Si deja una fila sin monto, al procesar se ejecuta Completar Impagos para registrar pago libre 0 solo en créditos elegibles por la base de datos (por ejemplo, no marca créditos cuyo primer pago aún no vence)."
        />

        <div className="cobranza-bloque-toolbar">
          <Input.Search
            allowClear
            value={filtro}
            onChange={(e) => setFiltro(e.target.value)}
            placeholder="Buscar cliente, crédito, celular, dirección o negocio"
            style={{ maxWidth: 520 }}
          />
          <Button onClick={() => void cobroQuery.refetch()} loading={cobroQuery.isFetching}>
            Actualizar cobro diario
          </Button>
          <Text type="secondary">
            {rowsFiltradas.length} de {rows.length} cliente(s)
          </Text>
        </div>

        <Table<RptCobroDiarioRow>
          rowKey="creditoId"
          className="credix-table cobranza-bloque-table"
          columns={columns}
          dataSource={rowsFiltradas}
          loading={cobroQuery.isLoading}
          size="small"
          bordered
          pagination={{
            defaultPageSize: 20,
            showSizeChanger: true,
            pageSizeOptions: ['10', '20', '50', '100'],
          }}
          scroll={{ x: 1380, y: 'calc(100vh - 390px)' }}
          expandable={{
            expandedRowRender: (row) => {
              const estado = getEstado(row.creditoId)
              return (
                <CobranzaBloqueCuotasPanel
                  creditoId={row.creditoId}
                  selectedKeys={estado.selectedKeys}
                  onSelectionChange={(selectedKeys, total) =>
                    patchEstado(row.creditoId, {
                      modo: 'cuotas',
                      selectedKeys,
                      monto: selectedKeys.length > 0 ? total : estado.monto,
                    })
                  }
                />
              )
            },
          }}
          rowClassName={(row) => {
            const estado = getEstado(row.creditoId)
            if ((estado.monto ?? 0) > 0) {
              return 'cobranza-bloque-row--pago'
            }
            return isCuotaSelectable({
              planPagoId: 1,
              glosa: null,
              fechaVencimiento: row.fechaVencimiento,
              amortizacion: null,
              interes: null,
              gastosAdm: null,
              cuota: row.cuotaTotal,
              diasAtrazo: row.diasAtrazo,
              importeMora: row.mora,
              descuento: null,
              cargo: null,
              pagoLibre: null,
              pagoCuota: row.cuotaTotal,
            })
              ? 'cobranza-bloque-row--impago'
              : ''
          }}
        />
      </Space>
    </CajaDrawer>
  )
}

export default CobranzaBloqueDrawer
