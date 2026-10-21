import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Space, Tabs } from 'antd'
import { ReloadOutlined } from '@ant-design/icons'
import {
  completarImpagosValidacion,
  fetchCajaDiarioSesion,
  fetchResumenIngresoCaja,
  fetchRptSaldosCaja,
  fetchSaldoCuentaCajaDiario,
  recalcularCajaDiario,
} from '../../api/cajaDiario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import {
  CredixAlertNote,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import { formatFechaHora } from '../../utils/formatFecha'
import { invalidateCajaOperacion } from '../../utils/cajaOperacionSync'
import { CajaDiarioOperacionesBar } from './cajaDiario/CajaDiarioOperacionesBar'
import { ArqueoTab } from './cajaDiario/ArqueoTab'
import { CobranzasTab } from './cajaDiario/CobranzasTab'
import { DesembolsosTab } from './cajaDiario/DesembolsosTab'
import { EntradaSalidaTab } from './cajaDiario/EntradaSalidaTab'
import {
  cajaToastError,
  cajaToastSuccess,
} from './cajaDiario/cajaFeedback'
import {
  type CajaSession,
  errMsg,
  resolveCajaDiarioTab,
  type CajaDiarioTabKey,
} from './cajaDiario/types'
import { CajaSinSesionAlert } from './components/CajaSinSesionAlert'
import '../../styles/caja-diario.css';
import '../../styles/caja-list-toolbar.css';

export type { CajaSession } from './cajaDiario/types'

export function CajaDiarioPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const creditoIdQuery = searchParams.get('creditoId')
  const tabQuery = searchParams.get('tab')
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0

  const [showStats, setShowStats] = useState(false)
  const [opsOpen, setOpsOpen] = useState(false)

  const activeTab: CajaDiarioTabKey = creditoIdQuery
    ? 'cobranzas'
    : resolveCajaDiarioTab(tabQuery)

  const setActiveTab = (key: string) => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        next.set('tab', key)
        return next
      },
      { replace: true },
    )
  }

  const cajaQuery = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const caja = cajaQuery.data
  const ctx: CajaSession | null = caja
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

  const saldoQuery = useQuery({
    queryKey: ['caja-saldo', ctx?.cajaDiarioId],
    queryFn: () => fetchSaldoCuentaCajaDiario(ctx!.cajaDiarioId),
    enabled: !!ctx,
  })

  const resumenQuery = useQuery({
    queryKey: ['caja-resumen-ingreso', ctx?.cajaDiarioId, ctx?.oficinaId],
    queryFn: () =>
      fetchResumenIngresoCaja(ctx!.cajaDiarioId, ctx!.oficinaId),
    enabled: !!ctx,
  })

  const movimientosQuery = useQuery({
    queryKey: ['caja-movimientos', ctx?.cajaDiarioId],
    queryFn: () => fetchRptSaldosCaja(ctx!.cajaDiarioId, false, true),
    enabled: !!ctx,
  })

  const impagosValidacionQuery = useQuery({
    queryKey: ['caja-impagos-validacion', ctx?.oficinaId, ctx?.cajaDiarioId],
    queryFn: () => completarImpagosValidacion(ctx!.oficinaId, ctx!.cajaDiarioId),
    enabled: !!ctx && !ctx.indCierre,
    staleTime: 60_000,
  })

  const cantidadImpagosPendientes =
    impagosValidacionQuery.data?.cantidadImpagosPendientes ?? 0

  const movimientos = useMemo(
    () => movimientosQuery.data ?? [],
    [movimientosQuery.data],
  )
  const movimientosEntrada = useMemo(
    () => movimientos.filter((m) => m.indEntrada),
    [movimientos],
  )
  const movimientosSalida = useMemo(
    () => movimientos.filter((m) => !m.indEntrada),
    [movimientos],
  )

  const recalcular = useMutation({
    mutationFn: () =>
      recalcularCajaDiario({
        oficinaId: ctx!.oficinaId,
        cajaDiarioId: ctx!.cajaDiarioId,
      }),
    onSuccess: (r) => {
      cajaToastSuccess(`Caja recalculada (código ${r.resultCode})`, 'caja-recalc')
      invalidateCaja()
      void cajaQuery.refetch()
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const invalidateCaja = () => {
    invalidateCajaOperacion(queryClient, {
      oficinaId,
      cajaDiarioId: ctx?.cajaDiarioId ?? 0,
    })
  }

  const refreshCaja = () => {
    invalidateCaja()
    void cajaQuery.refetch()
  }


  const cajaStats: CredixStatItem[] = ctx
    ? [
        {
          value: formatMoney(ctx.saldoInicial),
          label: 'Saldo inicial (S/.)',
          tone: 'green',
        },
        {
          value: formatMoney(ctx.entradas),
          label: 'Entradas (S/.)',
          tone: 'green',
        },
        {
          value: formatMoney(ctx.salidas),
          label: 'Salidas (S/.)',
          tone: 'red',
        },
        {
          value: formatMoney(ctx.saldoFinal),
          label: 'Neto caja (S/.)',
          tone: 'default',
        },
        {
          value: ctx.indCierre ? 'CERRADO' : 'ABIERTO',
          label: ctx.cajaDenominacion,
          detail: formatFechaHora(ctx.fechaIniOperacion),
          tone: ctx.indCierre ? 'red' : 'green',
        },
      ]
    : []

  return (
    <CredixPage
      title="Caja diario"
      subtitle={undefined}
      breadcrumb={
        activeTab === 'cobranzas'
          ? undefined
          : [
              { title: <Link to="/inicio">Inicio</Link> },
              { title: <Link to="/caja">Caja</Link> },
              { title: 'Caja diario' },
            ]
      }
      stats={showStats ? cajaStats : []}
      statsVariant="default"
      actions={
        <Space wrap>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void cajaQuery.refetch()}
          >
            Actualizar
          </Button>
          <Link to="/caja/verificar-pagos">
            <Button>Verificar pagos</Button>
          </Link>
        </Space>
      }
      className={[
        'caja-diario-page',
        activeTab === 'cobranzas' ? 'caja-diario-page--cobro' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <div className="caja-diario-shell">
        {cantidadImpagosPendientes > 0 ? (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message={`Hay ${cantidadImpagosPendientes} crédito(s) con impagos pendientes`}
            description={
              <>
                Revise y verifique los pagos antes de cerrar caja.{' '}
                <Link to="/caja/verificar-pagos">Ir a verificar pagos</Link>
              </>
            }
            action={
              <Link to="/caja/verificar-pagos">
                <Button size="small" type="primary">
                  Verificar pagos
                </Button>
              </Link>
            }
          />
        ) : null}
        {cajaQuery.isLoading && (
          <Alert message="Cargando caja asignada…" type="info" />
        )}
        {cajaQuery.isError && (
          <Alert
            type="error"
            showIcon
            message="No se pudo cargar la sesión de caja"
            description={
              cajaQuery.error instanceof ApiError
                ? cajaQuery.error.message
                : 'Revise que la API moderna esté en marcha.'
            }
            action={
              <Button size="small" onClick={() => void cajaQuery.refetch()}>
                Reintentar
              </Button>
            }
          />
        )}
        {cajaQuery.isSuccess && !ctx && (
          <CajaSinSesionAlert
            variant="diario"
            refreshing={cajaQuery.isFetching}
            onRefresh={() => void cajaQuery.refetch()}
            onAssigned={() => void cajaQuery.refetch()}
          />
        )}

        {ctx && (
          <div className="caja-diario-workspace">
            <header
              className="caja-diario-session-bar"
              aria-label="Sesión de caja"
            >
              <div className="caja-diario-session-bar__identity">
                <h4
                  className="caja-diario-session-name"
                  role="button"
                  tabIndex={0}
                  title="Ver detalle de saldos (inicial, entradas, salidas)"
                  onClick={() => setShowStats((v) => !v)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      setShowStats((v) => !v)
                    }
                  }}
                >
                  {ctx.cajaDenominacion}
                </h4>
                <span
                  className={[
                    'caja-diario-session-status',
                    ctx.indCierre ? 'is-cerrado' : 'is-abierto',
                  ].join(' ')}
                >
                  {ctx.indCierre ? 'Cerrado' : 'Abierto'}
                </span>
              </div>

              <dl className="caja-diario-session-bar__meta">
                <div>
                  <dt>Efectivo</dt>
                  <dd>
                    {saldoQuery.data != null
                      ? formatMoney(saldoQuery.data.saldo)
                      : '…'}
                  </dd>
                </div>
              </dl>
            </header>

            <div className="caja-diario-main-column">
              {activeTab === 'cobranzas' ? (
                <details
                  className="caja-diario-ops-collapse"
                  open={opsOpen}
                  onToggle={(e) =>
                    setOpsOpen((e.target as HTMLDetailsElement).open)
                  }
                >
                  <summary>Más acciones</summary>
                  <CajaDiarioOperacionesBar
                    ctx={ctx}
                    usuarioId={session?.usuarioId ?? 0}
                    variant="inline"
                    onRecalcular={() => recalcular.mutate()}
                    recalculando={recalcular.isPending}
                  />
                </details>
              ) : (
                <CajaDiarioOperacionesBar
                  ctx={ctx}
                  usuarioId={session?.usuarioId ?? 0}
                  variant="inline"
                  onRecalcular={() => recalcular.mutate()}
                  recalculando={recalcular.isPending}
                />
              )}

              <div className="caja-diario-tabs-panel">
                <CredixPanel className="caja-diario-main-panel">
                  {resumenQuery.data?.texto ? (
                    <details className="caja-diario-resumen-collapse">
                      <summary>Resumen cuenta caja</summary>
                      <CredixAlertNote strong="Resumen cuenta caja diario">
                        <pre className="caja-diario-resumen-pre">
                          {resumenQuery.data.texto}
                        </pre>
                      </CredixAlertNote>
                    </details>
                  ) : null}

                  <Tabs
                    className="credix-tabs caja-diario-tabs"
                    activeKey={activeTab}
                    onChange={setActiveTab}
                    size="middle"
                    tabBarGutter={8}
                    destroyInactiveTabPane
                    items={[
                      {
                        key: 'cobranzas',
                        label: 'Cobranzas',
                        children: (
                          <CobranzasTab
                            ctx={ctx}
                            creditoIdInicial={creditoIdQuery}
                            onChanged={() => {
                              invalidateCaja()
                              void cajaQuery.refetch()
                            }}
                          />
                        ),
                      },
                      {
                        key: 'desembolsos',
                        label: 'Desembolsos',
                        children: (
                          <DesembolsosTab
                            ctx={ctx}
                            active={activeTab === 'desembolsos'}
                            onChanged={refreshCaja}
                          />
                        ),
                      },
                      {
                        key: 'egreso-ingreso',
                        label: 'Egreso / Ingreso',
                        children: (
                          <EntradaSalidaTab
                            ctx={ctx}
                            onChanged={refreshCaja}
                          />
                        ),
                      },
                      {
                        key: 'arqueo',
                        label: 'Arqueo',
                        children: (
                          <ArqueoTab
                            ctx={ctx}
                            entradas={movimientosEntrada}
                            salidas={movimientosSalida}
                            loading={movimientosQuery.isLoading}
                            onRefresh={() => void movimientosQuery.refetch()}
                            onChanged={() => {
                              invalidateCaja()
                              void cajaQuery.refetch()
                            }}
                            onCerrada={() => {
                              void cajaQuery.refetch()
                              navigate('/inicio')
                            }}
                            openCierre={tabQuery === 'cierre'}
                          />
                        ),
                      },
                    ]}
                  />
                </CredixPanel>
              </div>
            </div>
          </div>
        )}
      </div>
    </CredixPage>
  )
}
