import {
  lazy,
  Suspense,
  useCallback,
  useEffect,
  useMemo,
  useState,
  type Key,
} from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Input,
  InputNumber,
  Select,
  Space,
  message,
} from 'antd'
import {
  CreditCardOutlined,
  DollarOutlined,
  FilePdfOutlined,
  FileSearchOutlined,
  HistoryOutlined,
  SearchOutlined,
  TeamOutlined,
  UnorderedListOutlined,
  WalletOutlined,
} from '@ant-design/icons'
import {
  fetchCreditoMoraResumen,
  fetchCreditosPorPersona,
  fetchCuentasPorCobrarPendientes,
  pagarCuotaConMora,
  pagarCuotaImporteLibre,
  pagarCuotas,
} from '../../../api/cajaDiario'
import { downloadMovimientosCreditoPdf } from '../../../api/creditoPlanes'
import { CajaModal } from '../../../components/caja/CajaModal'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import { CreditoMoraModal } from '../../../components/caja/CreditoMoraModal'
import { fetchValoresTabla } from '../../../api/maestros'
import { CajaCuotasTable } from '../../../components/caja/CajaCuotasTable'
import {
  loadCuotasCobranzaGrid,
  type CuotaCobranzaRow,
} from '../../../components/caja/cuotasGridMerge'
import { CajaSection } from '../../../components/caja/CajaSection'
import { ClienteBuscarAutoComplete } from '../../../components/caja/ClienteBuscarAutoComplete'
import {
  contarCuotasCobrables,
  contarCuotasConMora,
  isCuotaSelectable,
} from '../../../components/caja/cuotaRowStyle'
import { sumarMoraVigente } from '../../../components/caja/creditoMoraVista'
import type { CreditoPorPersonaRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { CancelacionCreditoPanel } from './CancelacionCreditoPanel'
import { CxcInlinePanel } from './CxcInlinePanel'
import { CreditosPendientesModal } from './CreditosPendientesModal'
import type { CajaSession } from './types'
import { errMsg } from './types'
import {
  assertSinCxcPendiente,
  maybeDownloadCajaTicket,
} from './cajaPagoHelpers'

const CobranzaBloqueDrawer = lazy(() => import('./cobranzabloquedrawer'))

export function CobranzasTab({
  ctx,
  creditoIdInicial,
  usuarioId,
  onChanged,
}: {
  ctx: CajaSession
  creditoIdInicial: string | null
  usuarioId: number
  onChanged: () => void
}) {
  const [clienteLabel, setClienteLabel] = useState('')
  const [personaId, setPersonaId] = useState(0)
  const [creditosPersona, setCreditosPersona] = useState<CreditoPorPersonaRow[]>(
    [],
  )
  const [creditoId, setCreditoId] = useState<number | null>(() => {
    if (!creditoIdInicial) {
      return null
    }
    const id = Number(creditoIdInicial)
    return Number.isNaN(id) || id < 1 ? null : id
  })
  const [cuotas, setCuotas] = useState<CuotaCobranzaRow[]>([])
  const [selectedKeys, setSelectedKeys] = useState<Key[]>([])
  const [importeRecibido, setImporteRecibido] = useState<number | null>(null)
  const [tipoPagoId, setTipoPagoId] = useState(1)
  const [tipoPagoLibreId, setTipoPagoLibreId] = useState(1)
  const [pagoLibre, setPagoLibre] = useState<number | null>(null)
  const [fechaTransferencia, setFechaTransferencia] = useState('')
  const [fechaPagoLibre, setFechaPagoLibre] = useState('')
  const [showCxc, setShowCxc] = useState(false)
  const [cuotasModalOpen, setCuotasModalOpen] = useState(false)
  const [cobranzaBloqueOpen, setCobranzaBloqueOpen] = useState(false)
  const [fechaLibreModalOpen, setFechaLibreModalOpen] = useState(false)
  const [fechaCuotaModalOpen, setFechaCuotaModalOpen] = useState(false)
  const [moraModalOpen, setMoraModalOpen] = useState(false)
  const moraResumenQuery = useQuery({
    queryKey: ['credito-mora-resumen', creditoId],
    queryFn: () => fetchCreditoMoraResumen(creditoId!),
    enabled: creditoId != null && creditoId > 0,
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

  const buscar = useMutation({
    mutationFn: ({
      creditoId: id,
      silent,
    }: {
      creditoId: number
      silent?: boolean
    }) => loadCuotasCobranzaGrid(id).then((data) => ({ data, silent })),
    onSuccess: ({ data, silent }) => {
      setCuotas(data)
      setSelectedKeys([])
      setImporteRecibido(null)
      if (silent) {
        return
      }
      const cobrables = data.filter(isCuotaSelectable).length
      if (data.length > 0) {
        message.success(
          `Plan cargado: ${data.length} fila(s)${cobrables > 0 ? ` · ${cobrables} cobrable(s)` : ''}`,
        )
      } else {
        message.info('Sin cuotas para este crédito')
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cargarCuotas = (id: number, options?: { silent?: boolean }) => {
    setCreditoId(id)
    buscar.mutate({ creditoId: id, silent: options?.silent })
  }

  useEffect(() => {
    if (!creditoIdInicial) {
      return
    }
    const id = Number(creditoIdInicial)
    if (id > 0) {
      cargarCuotas(id, { silent: true })
    }
    // Solo al cambiar el crédito en la URL; no incluir cargarCuotas (evita bucle).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [creditoIdInicial])

  const cargarCreditos = async (pid: number, label: string) => {
    setPersonaId(pid)
    setClienteLabel(label)
    const list = await fetchCreditosPorPersona(pid, ctx.esCajaCentral)
    setCreditosPersona(list)

    const cxc = await fetchCuentasPorCobrarPendientes(
      ctx.oficinaId,
      ctx.cajaDiarioId,
      pid,
    )
    if (cxc.length > 0) {
      setShowCxc(true)
    }

    if (list.length === 0) {
      message.info('El cliente no tiene créditos activos para cobrar')
      return
    }

    const creditoDefault = list[0].creditoId
    cargarCuotas(creditoDefault)

    if (list.length > 1) {
      message.info(
        `${list.length} créditos activos. Se cargó el crédito ${creditoDefault}; puede cambiarlo abajo.`,
      )
    }
  }

  const creditoSel = creditosPersona.find((c) => c.creditoId === creditoId)

  const cuotasPagables = useMemo(
    () => cuotas.filter(isCuotaSelectable),
    [cuotas],
  )

  const resumenCuotas = useMemo(() => {
    const cobrables = contarCuotasCobrables(cuotas)
    const mora = contarCuotasConMora(cuotas)
    const moraVigente = sumarMoraVigente(cuotas)
    return { cobrables, mora, moraVigente, filas: cuotas.length }
  }, [cuotas])

  const prepararCobroTodasCuotas = useCallback(() => {
    const ids = cuotasPagables
      .map((c) => c.planPagoId)
      .filter((id): id is number => id != null && id > 0)
    setSelectedKeys(ids)
    const total = cuotasPagables.reduce(
      (s, c) => s + (c.pagoCuota ?? c.cuota ?? 0),
      0,
    )
    setImporteRecibido(total)
    message.info(
      `${ids.length} cuota(s) seleccionada(s). Revise el importe recibido antes de cobrar.`,
    )
  }, [cuotasPagables])



  const totalCuotasCredito = useMemo(
    () =>
      cuotasPagables.reduce((s, c) => s + (c.pagoCuota ?? c.cuota ?? 0), 0),
    [cuotasPagables],
  )

  const pagar = useMutation({
    mutationFn: () =>
      pagarCuotas({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId: creditoId!,
        listaPlanPagoId: selectedKeys.join(','),
        importeRecibido: importeRecibido ?? 0,
        tipoPagoId,
        fechaPagoTransferencia:
          tipoPagoId > 1 && fechaTransferencia
            ? fechaTransferencia
            : undefined,
        esUltimaCuota:
          selectedKeys.length > 0 && selectedKeys.length === cuotasPagables.length,
        aplicarMoraPostergada: moraResumenQuery.data?.indMoraProducto ?? true,
      }),
    onSuccess: async (r) => {
      message.success(`Pago registrado (${r.resultId ?? 'OK'})`)
      await maybeDownloadCajaTicket(ctx.oficinaId, r.resultId)
      onChanged()
      void moraResumenQuery.refetch()
      if (creditoId) {
        buscar.mutate({ creditoId, silent: true })
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const ejecutarPagoLibre = useMutation({
    mutationFn: async () => {
      const esUltima = cuotasPagables.length <= 1
      const usarMoraIntegrada =
        (moraResumenQuery.data?.indMoraProducto ?? false) && esUltima
      if (usarMoraIntegrada) {
        return pagarCuotaConMora({
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          creditoId: creditoId!,
          importeRecibido: pagoLibre ?? 0,
          esUltimaCuota: true,
          tipoPagoId: tipoPagoLibreId,
          fechaPagoTransferencia:
            tipoPagoLibreId > 1 && fechaPagoLibre ? fechaPagoLibre : null,
        })
      }
      return pagarCuotaImporteLibre({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId: creditoId!,
        importeRecibido: pagoLibre ?? 0,
        tipoPagoId: tipoPagoLibreId,
        fechaPagoTransferencia:
          tipoPagoLibreId > 1 && fechaPagoLibre ? fechaPagoLibre : null,
      })
    },
    onSuccess: async (r) => {
      const movId =
        'movimientoCajaId' in r ? r.movimientoCajaId : r.resultId
      const msg =
        'mensaje' in r && r.mensaje
          ? r.mensaje
          : 'Pago libre registrado'
      message.success(msg)
      await maybeDownloadCajaTicket(ctx.oficinaId, movId)
      setPagoLibre(null)
      setFechaLibreModalOpen(false)
      onChanged()
      void moraResumenQuery.refetch()
      if (creditoId) {
        buscar.mutate({ creditoId, silent: true })
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const solicitarPagoLibre = async () => {
    if (!creditoId || !pagoLibre || pagoLibre <= 0) {
      return
    }
    if (pagoLibre > totalCuotasCredito && totalCuotasCredito > 0) {
      message.error(
        `El pago libre no debe superar ${formatMoney(totalCuotasCredito)}`,
      )
      return
    }
    try {
      if (!(await assertSinCxcPendiente(creditoId))) {
        return
      }
      if (tipoPagoLibreId > 1) {
        setFechaLibreModalOpen(true)
        return
      }
      cajaConfirm({
        title: 'Confirmar pago libre',
        content: `¿Registrar pago libre de ${formatMoney(pagoLibre)}?`,
        onOk: () => ejecutarPagoLibre.mutateAsync(),
      })
    } catch (e) {
      message.error(errMsg(e))
    }
  }

  const solicitarPagarCuota = async () => {
    if (!creditoId || selectedKeys.length === 0) {
      return
    }
    if (importeRecibido == null || importeRecibido < selectedTotal) {
      message.error('El importe recibido debe ser mayor o igual al total de pago')
      return
    }
    try {
      if (!(await assertSinCxcPendiente(creditoId))) {
        return
      }
      if (tipoPagoId > 1 && !fechaTransferencia.trim()) {
        setFechaCuotaModalOpen(true)
        return
      }
      cajaConfirm({
        title: 'Confirmar cobro',
        content: `¿Cobrar ${selectedKeys.length} cuota(s) por ${formatMoney(selectedTotal)}?`,
        onOk: () => pagar.mutateAsync(),
      })
    } catch (e) {
      message.error(errMsg(e))
    }
  }

  const descargarMovimientosCredito = async (id: number) => {
    try {
      await downloadMovimientosCreditoPdf(id)
      message.success('Movimientos del crédito descargados')
    } catch (e) {
      message.error(errMsg(e))
    }
  }

  const handleSelectionChange = useCallback(
    (keys: Key[]) => {
      const valid = keys.filter((k) =>
        cuotasPagables.some((c) => c.planPagoId === k),
      )
      setSelectedKeys(valid)
      const total = cuotasPagables
        .filter((c) => c.planPagoId != null && valid.includes(c.planPagoId))
        .reduce((s, c) => s + (c.pagoCuota ?? c.cuota ?? 0), 0)
      setImporteRecibido(total)
    },
    [cuotasPagables],
  )

  const selectedTotal = useMemo(
    () =>
      cuotasPagables
        .filter((c) => c.planPagoId != null && selectedKeys.includes(c.planPagoId))
        .reduce((s, c) => s + (c.pagoCuota ?? c.cuota ?? 0), 0),
    [cuotasPagables, selectedKeys],
  )

  const vuelto =
    importeRecibido != null ? importeRecibido - selectedTotal : null

  return (
    <div className="caja-diario-cobranzas">
      <div className="caja-diario-cobranzas-top">
        <div className="caja-diario-cobranzas-top__search">
          <span className="caja-diario-cobranzas-top__label" id="caja-buscar-cliente-label">
            Buscar cliente
          </span>
          <ClienteBuscarAutoComplete
            value={clienteLabel}
            onChange={setClienteLabel}
            onSelectPersona={(pid, label) => {
              void cargarCreditos(pid, label)
            }}
            autoFocus
            fullWidth
            showSearchButton
            searchButtonLabel="Cargar"
            ariaLabelledBy="caja-buscar-cliente-label"
          />
        </div>
        <div
          className="caja-diario-actions-row"
          role="toolbar"
          aria-label="Acciones de cobranza"
        >
          <Button
            icon={<WalletOutlined />}
            type={showCxc ? 'primary' : 'default'}
            onClick={() => {
              setShowCxc(true)
              setPersonaId(0)
            }}
          >
            GAD pendientes
          </Button>
          <Button
            icon={<UnorderedListOutlined />}
            onClick={() => setCuotasModalOpen(true)}
          >
            Cuotas pendientes
          </Button>
          <Button
            icon={<TeamOutlined />}
            type="primary"
            ghost
            onClick={() => setCobranzaBloqueOpen(true)}
          >
            Cobranza en bloque
          </Button>
        </div>
      </div>

      {showCxc ? (
        <CxcInlinePanel
          ctx={ctx}
          personaId={personaId}
          clienteLabel={clienteLabel}
          onChanged={onChanged}
        />
      ) : null}

      {moraResumenQuery.data?.indMoraProducto &&
      (resumenCuotas.moraVigente > 0 ||
        (moraResumenQuery.data.saldoPostergado ?? 0) > 0) ? (
        <Alert
          type="warning"
          showIcon
          className="caja-diario-mora-banner"
          message={
            <>
              Mora del crédito:{' '}
              <strong>{formatMoney(resumenCuotas.moraVigente)}</strong> en cuotas
              {(moraResumenQuery.data.saldoPostergado ?? 0) > 0
                ? ` · ${formatMoney(moraResumenQuery.data.saldoPostergado)} postergada`
                : ''}
            </>
          }
          description="La mora postergada se liquida al cobrar la última cuota. Use «Crédito mora» para el detalle y reportes PDF."
          action={
            <Button size="small" onClick={() => setMoraModalOpen(true)}>
              Ver detalle
            </Button>
          }
        />
      ) : null}

      {moraResumenQuery.data?.indMoraProducto &&
      (moraResumenQuery.data.saldoPostergado ?? 0) > 0 ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Mora postergada acumulada: ${formatMoney(moraResumenQuery.data.saldoPostergado)}`}
          description="Se cobrará al liquidar la última cuota pendiente (CreditoMora)."
          action={
            <Button size="small" onClick={() => setMoraModalOpen(true)}>
              Ver historial
            </Button>
          }
        />
      ) : null}

      <CajaSection
        tone="credito"
        kicker="Crédito activo"
        title={creditoId ?? '—'}
        subtitle={clienteLabel || creditoSel?.descripcion}
        icon={<FileSearchOutlined />}
      >
        {creditosPersona.length > 1 ? (
          <div className="caja-diario-creditos-chips">
            {creditosPersona.map((c) => (
              <div key={c.creditoId} className="caja-diario-credito-chip-wrap">
                <button
                  type="button"
                  className={`caja-diario-credito-chip${creditoId === c.creditoId ? ' is-active' : ''}`}
                  onClick={() => cargarCuotas(c.creditoId)}
                >
                  <span className="caja-diario-credito-chip-id">
                    Crédito {c.creditoId}
                  </span>
                  <span className="caja-diario-credito-chip-monto">
                    S/. {formatMoney(c.montoCredito)}
                  </span>
                </button>
                <Button
                  size="small"
                  type="link"
                  icon={<FilePdfOutlined />}
                  className="caja-diario-credito-chip-mov"
                  onClick={() => void descargarMovimientosCredito(c.creditoId)}
                >
                  Movimientos
                </Button>
              </div>
            ))}
          </div>
        ) : null}

        <Space wrap className="caja-diario-credito-filters">
          <Select
            style={{ minWidth: 220 }}
            placeholder="Crédito del cliente"
            value={creditoId ?? undefined}
            onChange={(id) => cargarCuotas(id)}
            options={creditosPersona.map((c) => ({
              value: c.creditoId,
              label: `${c.creditoId} — ${formatMoney(c.montoCredito)}`,
            }))}
            allowClear
            showSearch
            optionFilterProp="label"
            onClear={() => setCreditoId(null)}
          />
          <InputNumber
            min={1}
            placeholder="Nro. crédito"
            value={creditoId ?? undefined}
            onChange={(v) => setCreditoId(v ?? null)}
            onPressEnter={() => creditoId && cargarCuotas(creditoId)}
            style={{ width: 130 }}
          />
          <Button
            type="primary"
            icon={<SearchOutlined />}
            loading={buscar.isPending}
            disabled={!creditoId}
            onClick={() => creditoId && cargarCuotas(creditoId)}
          >
            Cuotas
          </Button>
          {creditoId ? (
            <Button
              icon={<HistoryOutlined />}
              onClick={() => setMoraModalOpen(true)}
            >
              Crédito mora
            </Button>
          ) : null}
        </Space>

        <div className="caja-diario-pago-libre-band">
          <DollarOutlined aria-hidden />
          <span className="caja-diario-pago-libre-label">Pago libre</span>
          <Select
            style={{ width: 130 }}
            value={tipoPagoLibreId}
            onChange={setTipoPagoLibreId}
            loading={tiposPagoQuery.isLoading}
            options={tipoPagoOptions}
          />
          <InputNumber
            min={0}
            step={0.01}
            placeholder="Monto"
            value={pagoLibre}
            onChange={setPagoLibre}
            style={{ width: 110 }}
          />
          <Button
            type="primary"
            className="caja-btn-pago-libre"
            disabled={!creditoId || !pagoLibre}
            loading={ejecutarPagoLibre.isPending}
            onClick={() => void solicitarPagoLibre()}
          >
            Pago libre
          </Button>
        </div>
      </CajaSection>

      <CajaSection
        tone="brand"
        kicker="Plan de pagos"
        title="Plan de cuotas"
        subtitle={
          creditoId
            ? `${resumenCuotas.filas} fila(s) · ${resumenCuotas.cobrables} cobrable(s)${resumenCuotas.mora > 0 ? ` · ${resumenCuotas.mora} con mora` : ''} · Crédito ${creditoId}`
            : 'Seleccione un crédito'
        }
      >
        <CajaCuotasTable
          data={cuotas}
          loading={buscar.isPending}
          selectedKeys={selectedKeys}
          onSelectionChange={handleSelectionChange}
        />
      </CajaSection>

      <p className="caja-diario-seleccion-resumen">
        <span>
          Selección: <strong>{formatMoney(selectedTotal)}</strong>
        </span>
        <span>
          Vuelto:{' '}
          <strong className={vuelto != null && vuelto < 0 ? 'caja-vuelto-neg' : ''}>
            {vuelto != null ? formatMoney(vuelto) : '—'}
          </strong>
        </span>
        <span>{selectedKeys.length} cuota(s) seleccionada(s)</span>
      </p>

      <CajaSection
        tone="pago"
        kicker="Cobro"
        title="Registrar pago"
        icon={<CreditCardOutlined />}
      >
        <div className="caja-diario-pago-dock caja-diario-pago-dock--inline">
          <div className="caja-diario-pago-totals">
            <span>
              TOTAL: <strong>{formatMoney(selectedTotal)}</strong>
            </span>
            <span>
              Recibido:{' '}
              <InputNumber
                min={0}
                step={0.01}
                size="middle"
                value={importeRecibido}
                onChange={setImporteRecibido}
                style={{ width: 120 }}
              />
            </span>
            <span>
              VUELTO:{' '}
              <strong className={vuelto != null && vuelto < 0 ? 'caja-vuelto-neg' : ''}>
                {vuelto != null ? formatMoney(vuelto) : '—'}
              </strong>
            </span>
          </div>
          <Space wrap align="center" className="caja-diario-pago-actions">
            <Select
              style={{ width: 150 }}
              value={tipoPagoId}
              onChange={setTipoPagoId}
              loading={tiposPagoQuery.isLoading}
              options={tipoPagoOptions}
              placeholder="Tipo de pago"
            />
            {tipoPagoId > 1 && (
              <Input
                type="datetime-local"
                style={{ width: 210 }}
                value={fechaTransferencia}
                onChange={(e) => setFechaTransferencia(e.target.value)}
                aria-label="Fecha transferencia"
              />
            )}
            <Button
              type="primary"
              size="large"
              className="caja-btn-pagar-cuota"
              icon={<CreditCardOutlined />}
              disabled={!creditoId || selectedKeys.length === 0}
              loading={pagar.isPending}
              onClick={() => void solicitarPagarCuota()}
            >
              Pagar cuota
            </Button>
          </Space>
        </div>
      </CajaSection>

      <CancelacionCreditoPanel
        ctx={ctx}
        creditoId={creditoId}
        onChanged={onChanged}
      />

      <CreditosPendientesModal
        open={cuotasModalOpen}
        onClose={() => setCuotasModalOpen(false)}
        onSelectCredito={(id, pid, label) => {
          setCuotasModalOpen(false)
          void (async () => {
            await cargarCreditos(pid, label)
            cargarCuotas(id)
          })()
        }}
      />

      {cobranzaBloqueOpen ? (
        <Suspense fallback={null}>
          <CobranzaBloqueDrawer
            open={cobranzaBloqueOpen}
            ctx={ctx}
            usuarioId={usuarioId}
            onClose={() => setCobranzaBloqueOpen(false)}
            onChanged={onChanged}
          />
        </Suspense>
      ) : null}

      <CajaModal
        title="Fecha y hora de transferencia"
        open={fechaLibreModalOpen}
        onCancel={() => setFechaLibreModalOpen(false)}
        onOk={() => {
          if (!fechaPagoLibre.trim()) {
            message.warning('Indique fecha y hora')
            return
          }
          cajaConfirm({
            title: 'Confirmar pago libre',
            content: `¿Registrar pago libre de ${formatMoney(pagoLibre ?? 0)}?`,
            onOk: () => ejecutarPagoLibre.mutateAsync(),
          })
        }}
        confirmLoading={ejecutarPagoLibre.isPending}
        okText="Continuar"
      >
        <Input
          type="datetime-local"
          value={fechaPagoLibre}
          onChange={(e) => setFechaPagoLibre(e.target.value)}
          style={{ width: '100%' }}
        />
      </CajaModal>

      <CajaModal
        title="Fecha y hora de transferencia"
        open={fechaCuotaModalOpen}
        onCancel={() => setFechaCuotaModalOpen(false)}
        onOk={() => {
          if (!fechaTransferencia.trim()) {
            message.warning('Indique fecha y hora')
            return
          }
          setFechaCuotaModalOpen(false)
          cajaConfirm({
            title: 'Confirmar cobro',
            content: `¿Cobrar ${selectedKeys.length} cuota(s) por ${formatMoney(selectedTotal)}?`,
            onOk: () => pagar.mutateAsync(),
          })
        }}
        confirmLoading={pagar.isPending}
        okText="Continuar"
      >
        <Input
          type="datetime-local"
          value={fechaTransferencia}
          onChange={(e) => setFechaTransferencia(e.target.value)}
          style={{ width: '100%' }}
        />
      </CajaModal>

      <CreditoMoraModal
        open={moraModalOpen}
        creditoId={creditoId}
        cuotas={cuotas}
        onClose={() => setMoraModalOpen(false)}
        onPrepararCobroTodasCuotas={prepararCobroTodasCuotas}
      />
    </div>
  )
}
