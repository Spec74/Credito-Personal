import { Suspense, lazy, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Alert, Button, Space, Spin, Tag, Typography } from 'antd'
import { ReloadOutlined } from '@ant-design/icons'
import {
  fetchBovedaAbierta,
  fetchBovedaEstadoDinero,
  fetchExisteBovedaTemporal,
  fetchBovedaMediosRed,
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
  const [mostrarCeros, setMostrarCeros] = useState(false)

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
    queryKey: ['boveda-medios-red', oficinaId],
    queryFn: () => fetchBovedaMediosRed(oficinaId),
    enabled: oficinaId > 0 && secondaryReady,
  })

  const sinBoveda =
    boveda.isError &&
    boveda.error instanceof ApiError &&
    boveda.error.status === 404

  const esCentral = resumen.data?.esCentral === true

  const stats = useMemo((): CredixStatItem[] => {
    if (boveda.isLoading) {
      return [
        { value: '…', label: 'Saldo inicial' },
        { value: '…', label: 'Entradas' },
        { value: '…', label: 'Salidas' },
        { value: '…', label: 'Saldo final' },
      ]
    }
    if (sinBoveda || !boveda.data) {
      return [
        { value: '—', label: 'Saldo inicial', tone: sinBoveda ? 'red' : undefined },
        { value: '—', label: 'Entradas' },
        { value: '—', label: 'Salidas' },
        { value: '—', label: 'Saldo final' },
      ]
    }
    const abierta = !boveda.data.indCierre
    const delta = Number(boveda.data.deltaMedios ?? 0)
    const descuadre = Math.abs(delta) > 0.01
    return [
      { value: formatMoney(boveda.data.saldoInicial), label: 'Saldo inicial' },
      { value: formatMoney(boveda.data.entradas), label: 'Entradas' },
      { value: formatMoney(boveda.data.salidas), label: 'Salidas' },
      {
        value: formatMoney(boveda.data.saldoFinal),
        label: 'Saldo final',
        detail: abierta ? (descuadre ? 'Descuadre' : 'Cuadrado') : 'Cerrada',
        tone: descuadre ? 'red' : abierta ? 'green' : 'default',
      },
    ]
  }, [boveda.data, boveda.isLoading, sinBoveda])

  const refrescar = () => {
    void boveda.refetch()
    void estadoDinero.refetch()
    void temporal.refetch()
    if (oficinaId > 0) void resumen.refetch()
  }

  const deltaMedios = Number(boveda.data?.deltaMedios ?? 0)
  const sumaMedios = Number(boveda.data?.sumaMedios ?? 0)
  const descuadre = !!boveda.data && Math.abs(deltaMedios) > 0.01

  const estadoDineroErrorMsg =
    estadoDinero.error instanceof ApiError
      ? estadoDinero.error.message
      : undefined

  const sesionExtra = boveda.data ? (
    <Space wrap size={8} className="boveda-contexto" translate="no">
      <Tag color={boveda.data.indTemporal ? 'orange' : 'blue'}>
        {boveda.data.indTemporal ? 'Temporal' : 'Principal'}
      </Tag>
      <Tag color={boveda.data.indCierre ? 'default' : 'green'}>
        {boveda.data.indCierre ? 'Cerrada' : 'Abierta'}
      </Tag>
      <Text type="secondary" className="boveda-contexto__meta">
        Desde {formatFecha(boveda.data.fechaIniOperacion)} · #{boveda.data.bovedaId}
      </Text>
    </Space>
  ) : null

  const liquidezPanel = (
    <CredixPanel title="Liquidez de efectivo" className="boveda-panel-liquidez">
      <p className="boveda-panel-lead">
        Dónde está el efectivo. El cuadre usa solo la bóveda principal; temporal y
        cajas son capas aparte hasta el retorno (TRE).
      </p>
      <BovedaEstadoDineroPanel
        data={estadoDinero.data}
        capas={resumen.data?.capas}
        loading={
          estadoDinero.isLoading ||
          (secondaryReady && resumen.isLoading && !estadoDinero.data)
        }
        isError={estadoDinero.isError}
        errorMessage={estadoDineroErrorMsg}
      />
    </CredixPanel>
  )

  const mediosLead = esCentral
    ? 'Red consolidada por medio. Yape e Interbank agrupan Gerente/René; el ledger sigue separado. «En caja» es pendiente de TRE.'
    : 'Posición por medio (bóveda + temporal + pendiente en caja). Los cobros digitales siguen visibles hasta el retorno a bóveda.'

  return (
    <CredixPage
      className="boveda-page"
      translate="no"
      title="Bóveda"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/tesoreria">Tesorería</Link> },
        { title: 'Bóveda' },
      ]}
      stats={stats}
      actions={
        <Space wrap className="boveda-page__actions">
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
          className="boveda-alert"
          message="Hay una bóveda temporal abierta. Ciérrela en Cierre cuando termine."
        />
      ) : null}

      {descuadre ? (
        <Alert
          type="error"
          showIcon
          className="boveda-alert"
          message="El cuadre de medios no cierra"
          description={
            <>
              Σ medios del ledger {formatMoney(sumaMedios)} vs saldo final{' '}
              {formatMoney(boveda.data!.saldoFinal)} (delta {formatMoney(deltaMedios)}).
              No cierre hasta corregir los asientos.
            </>
          }
        />
      ) : null}

      {boveda.isLoading ? (
        <CredixPanel>
          <div className="boveda-page__loading">
            <Spin />
          </div>
        </CredixPanel>
      ) : sinBoveda ? (
        <>
          <Alert
            type="warning"
            showIcon
            className="boveda-alert"
            message="No hay bóveda abierta para esta oficina."
            description="La bóveda del día se crea al iniciar sesión. Revise con soporte si el problema persiste."
          />
          {liquidezPanel}
        </>
      ) : boveda.isError ? (
        <div translate="no">
          <Alert
            type="error"
            showIcon
            className="boveda-alert"
            message="No se pudo cargar la bóveda"
            description={
              boveda.error instanceof ApiError ? boveda.error.message : 'Error desconocido'
            }
          />
        </div>
      ) : boveda.data ? (
        <>
          {liquidezPanel}

          <CredixPanel title="Medios de pago" className="boveda-panel-medios">
            <p className="boveda-panel-lead">{mediosLead}</p>
            <BovedaResumenCuenta
              data={resumen.data}
              loading={resumen.isLoading || !secondaryReady}
              isError={resumen.isError}
              error={resumen.error}
              hideHeader
              eagerLogos
              mostrarCeros={mostrarCeros}
              onMostrarCerosChange={setMostrarCeros}
            />
          </CredixPanel>

          {!boveda.data.indCierre ? (
            <CredixPanel
              title="Operaciones"
              className="boveda-panel-ops"
              extra={sesionExtra}
            >
              <Suspense
                fallback={
                  <div className="boveda-page__loading">
                    <Spin />
                  </div>
                }
              >
                <BovedaOperacionesPanel
                  oficinaId={oficinaId}
                  boveda={boveda.data}
                  existeTemporal={temporal.data?.existe === true}
                  temporalListo={temporal.isSuccess}
                />
              </Suspense>
            </CredixPanel>
          ) : (
            <Alert
              type="info"
              showIcon
              className="boveda-alert"
              message="Bóveda cerrada"
              description="No hay operaciones sobre una bóveda cerrada."
            />
          )}

          <CredixPanel title="Historial" className="boveda-panel-historial">
            <Suspense
              fallback={
                <div className="boveda-page__loading">
                  <Spin />
                </div>
              }
            >
              <BovedaHistorialGrillas
                oficinaId={oficinaId}
                bovedaAbiertaId={boveda.data.bovedaId}
              />
            </Suspense>
          </CredixPanel>
        </>
      ) : (
        liquidezPanel
      )}
    </CredixPage>
  )
}
