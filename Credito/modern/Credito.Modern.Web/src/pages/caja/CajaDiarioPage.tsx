import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Space, Tabs, Typography } from 'antd'
import { ReloadOutlined } from '@ant-design/icons'
import {
  fetchCajaDiarioSesion,
  fetchResumenIngresoCaja,
  fetchRptSaldosCaja,
  fetchSaldoCuentaCajaDiario,
  recalcularCajaDiario,
} from '../../api/cajaDiario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { getLoginProfile } from '../../auth/sessionProfile'
import {
  CredixAlertNote,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import { formatFechaHora } from '../../utils/formatFecha'
import { CajaDiarioOperacionesBar } from './cajaDiario/CajaDiarioOperacionesBar'
import { ArqueoTab } from './cajaDiario/ArqueoTab'
import { CierreTab } from './cajaDiario/CierreTab'
import { CobranzasTab } from './cajaDiario/CobranzasTab'
import { CxcTab } from './cajaDiario/CxcTab'
import { DesembolsosTab } from './cajaDiario/DesembolsosTab'
import { EntradaSalidaTab } from './cajaDiario/EntradaSalidaTab'
import {
  cajaToastError,
  cajaToastSuccess,
} from './cajaDiario/cajaFeedback'
import {
  type CajaSession,
  errMsg,
  isCajaDiarioTabKey,
  type CajaDiarioTabKey,
} from './cajaDiario/types'

export type { CajaSession } from './cajaDiario/types'

const { Text } = Typography

export function CajaDiarioPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const creditoIdQuery = searchParams.get('creditoId')
  const tabQuery = searchParams.get('tab')
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const loginProfile = getLoginProfile()
  const oficinaId = session?.oficinaId ?? 0

  const [showStats, setShowStats] = useState(true)

  const initialTab: CajaDiarioTabKey = creditoIdQuery
    ? 'cobranzas'
    : isCajaDiarioTabKey(tabQuery)
      ? tabQuery
      : 'cobranzas'

  const activeTab = isCajaDiarioTabKey(tabQuery)
    ? tabQuery
    : initialTab

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
    void queryClient.invalidateQueries({ queryKey: ['caja-movimientos'] })
    void queryClient.invalidateQueries({ queryKey: ['caja-saldo'] })
    void queryClient.invalidateQueries({ queryKey: ['caja-resumen-ingreso'] })
    void queryClient.invalidateQueries({ queryKey: ['caja-cxc'] })
    void queryClient.invalidateQueries({ queryKey: ['caja-cxc-inline'] })
    void queryClient.invalidateQueries({ queryKey: ['caja-desembolsos'] })
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
      subtitle="Operación diaria: cobranzas, desembolsos, arqueo y cierre."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Caja diario' },
      ]}
      stats={showStats ? cajaStats : []}
      statsVariant="default"
      actions={
        <Space wrap>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void cajaQuery.refetch()}
          >
            Actualizar sesión
          </Button>
          <Link to="/caja/verificar-pagos">
            <Button>Verificar pagos</Button>
          </Link>
          <Link to="/informes/saldo-cartera-caja-diario">
            <Button>Saldo cartera</Button>
          </Link>
        </Space>
      }
      className="caja-diario-page"
    >
      <div className="caja-diario-shell">
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
          <Alert
            type="warning"
            showIcon
            message="Sin caja diario abierta"
            description={
              <>
                El usuario{' '}
                <Text strong>
                  {loginProfile.nombreUsuario ??
                    `ID ${session?.usuarioId ?? '?'}`}
                </Text>{' '}
                no tiene caja asignada y abierta. Use{' '}
                <Link to="/caja/asignar">Asignar caja</Link> o{' '}
                <Link to="/caja/saldos">Saldos y cierres</Link>.
              </>
            }
            action={
              <Space wrap>
                <Link to="/caja/asignar">
                  <Button type="primary" size="small">
                    Asignar caja
                  </Button>
                </Link>
                <Button size="small" onClick={() => void cajaQuery.refetch()}>
                  Actualizar
                </Button>
              </Space>
            }
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
                  title="Mostrar u ocultar saldos del encabezado"
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
                  <dt>Sesión</dt>
                  <dd>#{ctx.cajaDiarioId}</dd>
                </div>
                <div>
                  <dt>Inicio</dt>
                  <dd>{formatFechaHora(ctx.fechaIniOperacion)}</dd>
                </div>
                <div>
                  <dt>Efectivo</dt>
                  <dd>
                    {saldoQuery.data != null
                      ? formatMoney(saldoQuery.data.saldo)
                      : '…'}
                  </dd>
                </div>
                <div>
                  <dt>Neto</dt>
                  <dd>{formatMoney(ctx.saldoFinal)}</dd>
                </div>
              </dl>

              <div className="caja-diario-session-bar__actions">
                <Button
                  className="caja-diario-recalc-btn"
                  size="middle"
                  loading={recalcular.isPending}
                  onClick={() => recalcular.mutate()}
                >
                  Recalcular saldos
                </Button>
                <nav
                  className="caja-diario-quick-links"
                  aria-label="Reportes y cierres de caja"
                >
                  <Link to="/informes/caja-diario">Informe caja diario</Link>
                  <Link to="/informes/saldo-cartera-caja-diario">
                    Saldo cartera
                  </Link>
                  <Link to="/caja/saldos">Cierre masivo</Link>
                </nav>
              </div>
            </header>

            <div className="caja-diario-main-column">
              <CajaDiarioOperacionesBar
                ctx={ctx}
                usuarioId={session?.usuarioId ?? 0}
                variant="inline"
              />

              <div className="caja-diario-tabs-panel">
                <CredixPanel className="caja-diario-main-panel">
                  {resumenQuery.data?.texto && (
                    <CredixAlertNote strong="Resumen cuenta caja diario">
                      <pre className="caja-diario-resumen-pre">
                        {resumenQuery.data.texto}
                      </pre>
                    </CredixAlertNote>
                  )}

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
                            onGoCierre={() => setActiveTab('cierre')}
                          />
                        ),
                      },
                      {
                        key: 'cxc',
                        label: 'CxC',
                        children: (
                          <CxcTab
                            ctx={ctx}
                            active={activeTab === 'cxc'}
                            onChanged={refreshCaja}
                          />
                        ),
                      },
                      {
                        key: 'cierre',
                        label: 'Cierre',
                        children: (
                          <CierreTab
                            ctx={ctx}
                            onCerrada={() => {
                              void cajaQuery.refetch()
                              navigate('/inicio')
                            }}
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
