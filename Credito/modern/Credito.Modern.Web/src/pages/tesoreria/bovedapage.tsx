import { Suspense, lazy, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Space,
  Spin,
  Tag,
  Typography,
} from 'antd'
import {
  FilePdfOutlined,
  ReloadOutlined,
} from '@ant-design/icons'
import {
  downloadRptMovimientoBovedaPdf,
  fetchBovedaAbierta,
  fetchBovedaEstadoDinero,
  fetchExisteBovedaTemporal,
  fetchResumenCuentaBoveda,
} from '../../api/boveda'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { getLoginProfile } from '../../auth/sessionProfile'
import { BovedaEstadoDineroPanel } from './components/BovedaEstadoDineroPanel'
import { BovedaResumenCuenta } from './components/BovedaResumenCuenta'
import { BovedaSaldosGrid } from './components/BovedaSaldosGrid'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { useSecondaryDataReady } from '../../hooks/useSecondaryDataReady'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/boveda-module.css'

const { Text } = Typography

/** Paneles pesados (Tabs/Forms + grillas): fuera del path crítico de LCP. */
const BovedaOperacionesPanel = lazy(() =>
  import('./bovedaoperacionespanel').then((m) => ({
    default: m.BovedaOperacionesPanel,
  })),
)
const BovedaHistorialGrillas = lazy(() =>
  import('./components/BovedaHistorialGrillas').then((m) => ({
    default: m.BovedaHistorialGrillas,
  })),
)

const BOVEDA_SUBTITLE =
  'Liquidez de oficina, medios de pago y movimientos de tesorería.'

export function BovedaPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const oficinaLabel = getLoginProfile().oficinaLabel ?? `Oficina ${oficinaId}`
  const secondaryReady = useSecondaryDataReady(oficinaId > 0)

  const boveda = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  /** KPI “Total fondo”: crítico para el shell de stats (evita CLS). */
  const estadoDinero = useQuery({
    queryKey: ['boveda-estado-dinero', oficinaId],
    queryFn: () => fetchBovedaEstadoDinero(oficinaId),
    enabled: oficinaId > 0,
  })

  /** Alertas / paneles: no bloquean el primer paint. */
  const temporal = useQuery({
    queryKey: ['existe-boveda-temporal', oficinaId],
    queryFn: () => fetchExisteBovedaTemporal(oficinaId),
    enabled: oficinaId > 0 && secondaryReady,
  })

  const bovedaId = boveda.data?.bovedaId

  const resumen = useQuery({
    queryKey: ['resumen-cuenta-boveda', bovedaId],
    queryFn: () => fetchResumenCuentaBoveda(bovedaId!),
    enabled: !!bovedaId && secondaryReady,
  })

  const sinBoveda =
    boveda.isError &&
    boveda.error instanceof ApiError &&
    boveda.error.status === 404

  /**
   * Siempre 4 celdas con la misma geometría → evita CLS al pasar de
   * “cargando (3)” a “datos + Total fondo (4)”.
   */
  const stats = useMemo((): CredixStatItem[] => {
    if (boveda.isLoading) {
      return [
        { value: '…', label: 'Bóveda' },
        { value: '…', label: 'Saldo' },
        { value: 'Cargando', label: 'Estado' },
        { value: '…', label: 'Total fondo' },
      ]
    }
    if (sinBoveda) {
      return [
        { value: '—', label: 'Bóveda' },
        { value: '—', label: 'Saldo' },
        { value: 'Sin bóveda abierta', label: 'Estado', tone: 'red' },
        { value: formatMoney(estadoDinero.data?.totalFondo ?? 0), label: 'Total fondo' },
      ]
    }
    if (!boveda.data) {
      return [
        { value: '—', label: 'Bóveda' },
        { value: '—', label: 'Saldo' },
        { value: '—', label: 'Estado' },
        { value: '—', label: 'Total fondo' },
      ]
    }
    const abierta = !boveda.data.indCierre
    return [
      { value: boveda.data.bovedaId, label: 'Bóveda' },
      { value: formatMoney(boveda.data.saldoFinal), label: 'Saldo' },
      {
        value: abierta ? 'Abierta' : 'Cerrada',
        label: 'Estado',
        tone: abierta ? 'green' : 'default',
      },
      {
        value: estadoDinero.isLoading
          ? '…'
          : formatMoney(estadoDinero.data?.totalFondo ?? 0),
        label: 'Total fondo',
      },
    ]
  }, [
    boveda.data,
    boveda.isLoading,
    sinBoveda,
    estadoDinero.data?.totalFondo,
    estadoDinero.isLoading,
  ])

  const refrescar = () => {
    void boveda.refetch()
    void estadoDinero.refetch()
    void temporal.refetch()
    if (bovedaId) void resumen.refetch()
  }

  return (
    <CredixPage
      className="boveda-page"
      title="Bóveda de oficina"
      subtitle={BOVEDA_SUBTITLE}
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Bóveda' },
      ]}
      actions={
        <Space wrap>
          <Button icon={<ReloadOutlined />} onClick={refrescar}>
            Actualizar
          </Button>
          {bovedaId ? (
            <>
              <Button
                icon={<FilePdfOutlined />}
                onClick={() => void downloadRptMovimientoBovedaPdf(bovedaId)}
              >
                Movimientos PDF
              </Button>
              <Link to={`/tesoreria/movimiento-boveda?bovedaId=${bovedaId}`}>
                <Button type="primary">Informe movimientos</Button>
              </Link>
            </>
          ) : null}
        </Space>
      }
    >
      {temporal.data?.existe ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 0 }}
          message="Hay una bóveda temporal abierta en esta oficina."
        />
      ) : null}

      <CredixPanel title="Posición de liquidez" className="boveda-panel-liquidez">
        <BovedaEstadoDineroPanel
          data={estadoDinero.data}
          loading={estadoDinero.isLoading}
        />
      </CredixPanel>

      {boveda.isLoading ? (
        <CredixPanel>
          <Spin />
        </CredixPanel>
      ) : sinBoveda ? (
        <Alert
          type="warning"
          showIcon
          message="No hay bóveda abierta para esta oficina."
          description="La bóveda del día se crea al iniciar sesión. Revise con soporte si el problema persiste."
        />
      ) : boveda.isError ? (
        <Alert
          type="error"
          showIcon
          message="No se pudo cargar la bóveda"
          description={
            boveda.error instanceof ApiError ? boveda.error.message : 'Error desconocido'
          }
        />
      ) : boveda.data ? (
        <>
          <CredixPanel
            title={`Sesión · ${oficinaLabel}`}
            className="boveda-panel-sesion"
            extra={
              <Space wrap size={6}>
                <Tag color={boveda.data.indTemporal ? 'orange' : 'blue'}>
                  {boveda.data.indTemporal ? 'Temporal' : 'Principal'}
                </Tag>
                <Tag color={boveda.data.indCierre ? 'default' : 'green'}>
                  {boveda.data.indCierre ? 'Cerrada' : 'Abierta'}
                </Tag>
              </Space>
            }
          >
            <div className="boveda-sesion-meta">
              <Text strong>Bóveda #{boveda.data.bovedaId}</Text>
              <Text type="secondary">
                Operación desde {formatFecha(boveda.data.fechaIniOperacion)}
              </Text>
            </div>
            <BovedaSaldosGrid
              saldoInicial={boveda.data.saldoInicial}
              entradas={boveda.data.entradas}
              salidas={boveda.data.salidas}
              saldoFinal={boveda.data.saldoFinal}
            />
          </CredixPanel>

          <CredixPanel title="Medios de pago" className="boveda-panel-medios">
            <BovedaResumenCuenta
              texto={resumen.data?.texto}
              loading={resumen.isLoading}
              isError={resumen.isError}
              error={resumen.error}
            />
          </CredixPanel>

          {!boveda.data.indCierre ? (
            <CredixPanel title="Operaciones" className="boveda-panel-ops">
              <div className="boveda-operaciones-panel">
                <Suspense fallback={<Spin />}>
                  <BovedaOperacionesPanel
                    oficinaId={oficinaId}
                    boveda={boveda.data}
                    existeTemporal={temporal.data?.existe ?? false}
                  />
                </Suspense>
              </div>
            </CredixPanel>
          ) : (
            <Alert
              type="info"
              showIcon
              message="Bóveda cerrada"
              description="No hay operaciones sobre una bóveda cerrada."
            />
          )}

          <CredixPanel title="Historial y movimientos" className="boveda-panel-historial">
            <Suspense fallback={<Spin />}>
              <BovedaHistorialGrillas
                oficinaId={oficinaId}
                bovedaAbiertaId={boveda.data.bovedaId}
              />
            </Suspense>
          </CredixPanel>
        </>
      ) : null}
    </CredixPage>
  )
}
