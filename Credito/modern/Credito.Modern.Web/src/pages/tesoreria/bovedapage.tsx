import { Suspense, lazy, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Alert, Button, Space, Spin, Tag, Typography } from 'antd'
import { ReloadOutlined } from '@ant-design/icons'
import {
  fetchBovedaAbierta,
  fetchBovedaEstadoDinero,
  fetchExisteBovedaTemporal,
  fetchResumenCuentaBoveda,
} from '../../api/boveda'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { BovedaEstadoDineroPanel } from './components/BovedaEstadoDineroPanel'
import { BovedaResumenCuenta } from './components/BovedaResumenCuenta'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { useSecondaryDataReady } from '../../hooks/useSecondaryDataReady'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/boveda-module.css'

const { Text } = Typography

/** Paneles pesados (forms + grillas): fuera del path crítico de LCP. */
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

export function BovedaPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const secondaryReady = useSecondaryDataReady(oficinaId > 0)

  const boveda = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const estadoDinero = useQuery({
    queryKey: ['boveda-estado-dinero', oficinaId],
    queryFn: () => fetchBovedaEstadoDinero(oficinaId),
    enabled: oficinaId > 0,
  })

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

  const stats = useMemo((): CredixStatItem[] => {
    if (boveda.isLoading) {
      return [
        { value: '…', label: 'Saldo' },
        { value: '…', label: 'Entradas' },
        { value: '…', label: 'Salidas' },
        { value: '…', label: 'Total fondo' },
      ]
    }
    if (sinBoveda || !boveda.data) {
      return [
        { value: '—', label: 'Saldo', tone: sinBoveda ? 'red' : undefined },
        { value: '—', label: 'Entradas' },
        { value: '—', label: 'Salidas' },
        {
          value: formatMoney(estadoDinero.data?.totalFondo ?? 0),
          label: 'Total fondo',
        },
      ]
    }
    const abierta = !boveda.data.indCierre
    return [
      {
        value: formatMoney(boveda.data.saldoFinal),
        label: 'Saldo',
        detail: abierta ? 'Abierta' : 'Cerrada',
        tone: abierta ? 'green' : 'default',
      },
      { value: formatMoney(boveda.data.entradas), label: 'Entradas' },
      { value: formatMoney(boveda.data.salidas), label: 'Salidas' },
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

  const sesionExtra = boveda.data ? (
    <Space wrap size={8} className="boveda-contexto" translate="no">
      <Tag color={boveda.data.indTemporal ? 'orange' : 'blue'}>
        {boveda.data.indTemporal ? 'Temporal' : 'Principal'}
      </Tag>
      <Tag color={boveda.data.indCierre ? 'default' : 'green'}>
        {boveda.data.indCierre ? 'Cerrada' : 'Abierta'}
      </Tag>
      <Text type="secondary">
        Desde {formatFecha(boveda.data.fechaIniOperacion)} · #{boveda.data.bovedaId}
      </Text>
    </Space>
  ) : null

  return (
    <CredixPage
      className="boveda-page"
      title="Bóveda"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/tesoreria">Tesorería</Link> },
        { title: 'Bóveda' },
      ]}
      stats={stats}
      actions={
        <Space wrap>
          <Button icon={<ReloadOutlined />} onClick={refrescar}>
            Actualizar
          </Button>
          {bovedaId ? (
            <Link to={`/tesoreria/movimiento-boveda?bovedaId=${bovedaId}`}>
              <Button type="primary">Informe</Button>
            </Link>
          ) : null}
        </Space>
      }
    >
      {temporal.data?.existe ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 0 }}
          message="Hay una bóveda temporal abierta. Ciérrela en Cierre cuando termine."
        />
      ) : null}

      <CredixPanel
        title="Fuera de esta bóveda"
        className="boveda-panel-liquidez"
      >
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
          {!boveda.data.indCierre ? (
            <CredixPanel
              title="Operaciones"
              className="boveda-panel-ops"
              extra={sesionExtra}
            >
              <Suspense fallback={<Spin />}>
                <BovedaOperacionesPanel
                  oficinaId={oficinaId}
                  boveda={boveda.data}
                  existeTemporal={temporal.data?.existe ?? false}
                />
              </Suspense>
            </CredixPanel>
          ) : (
            <Alert
              type="info"
              showIcon
              message="Bóveda cerrada"
              description="No hay operaciones sobre una bóveda cerrada."
            />
          )}

          <CredixPanel title="Medios de pago" className="boveda-panel-medios">
            <BovedaResumenCuenta
              texto={resumen.data?.texto}
              loading={resumen.isLoading}
              isError={resumen.isError}
              error={resumen.error}
              hideHeader
            />
          </CredixPanel>

          <CredixPanel title="Historial" className="boveda-panel-historial">
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
