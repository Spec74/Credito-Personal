import { useMemo } from 'react'
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
import { BovedaOperacionesPanel } from './BovedaOperacionesPanel'
import { BovedaEstadoDineroPanel } from './components/BovedaEstadoDineroPanel'
import { BovedaHistorialGrillas } from './components/BovedaHistorialGrillas'
import { BovedaResumenCuenta } from './components/BovedaResumenCuenta'
import { BovedaSaldosGrid } from './components/BovedaSaldosGrid'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

export function BovedaPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const oficinaLabel = getLoginProfile().oficinaLabel ?? `Oficina ${oficinaId}`

  const boveda = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const temporal = useQuery({
    queryKey: ['existe-boveda-temporal', oficinaId],
    queryFn: () => fetchExisteBovedaTemporal(oficinaId),
    enabled: oficinaId > 0,
  })

  const estadoDinero = useQuery({
    queryKey: ['boveda-estado-dinero', oficinaId],
    queryFn: () => fetchBovedaEstadoDinero(oficinaId),
    enabled: oficinaId > 0,
  })

  const bovedaId = boveda.data?.bovedaId

  const resumen = useQuery({
    queryKey: ['resumen-cuenta-boveda', bovedaId],
    queryFn: () => fetchResumenCuentaBoveda(bovedaId!),
    enabled: !!bovedaId,
  })

  const sinBoveda =
    boveda.isError &&
    boveda.error instanceof ApiError &&
    boveda.error.status === 404

  const stats = useMemo((): CredixStatItem[] => {
    if (boveda.isLoading) {
      return [
        { value: oficinaId, label: 'Oficina' },
        { value: '…', label: 'Bóveda' },
        { value: 'Cargando', label: 'Estado' },
      ]
    }
    if (sinBoveda) {
      return [
        { value: oficinaId, label: 'Oficina' },
        { value: '—', label: 'Bóveda' },
        { value: 'Sin bóveda abierta', label: 'Estado', tone: 'red' },
      ]
    }
    if (!boveda.data) return []
    const abierta = !boveda.data.indCierre
    return [
      { value: boveda.data.bovedaId, label: 'Bóveda' },
      { value: formatMoney(boveda.data.saldoFinal), label: 'Saldo' },
      {
        value: abierta ? 'Abierta' : 'Cerrada',
        label: 'Estado',
        tone: abierta ? 'green' : 'default',
      },
      { value: formatMoney(estadoDinero.data?.totalFondo ?? 0), label: 'Total fondo' },
    ]
  }, [boveda.data, boveda.isLoading, sinBoveda, oficinaId, estadoDinero.data?.totalFondo])

  const refrescar = () => {
    void boveda.refetch()
    void temporal.refetch()
    void estadoDinero.refetch()
    if (bovedaId) void resumen.refetch()
  }

  return (
    <CredixPage
      className="boveda-page"
      title="Bóveda de oficina"
      subtitle="Estado de dinero, operaciones y historial — paridad con Boveda/Index del MVC."
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

      <CredixPanel title="Estado de dinero">
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
          <CredixPanel title={`Bóveda — ${oficinaLabel}`}>
            <Space wrap size="middle">
              <Text strong>Bóveda #{boveda.data.bovedaId}</Text>
              <Tag color={boveda.data.indTemporal ? 'orange' : 'blue'}>
                {boveda.data.indTemporal ? 'Temporal' : 'Principal'}
              </Tag>
              <Tag color={boveda.data.indCierre ? 'default' : 'green'}>
                {boveda.data.indCierre ? 'Cerrada' : 'Abierta'}
              </Tag>
              <Text type="secondary">
                Operación desde {formatFecha(boveda.data.fechaIniOperacion)}
              </Text>
            </Space>
            <BovedaSaldosGrid
              saldoInicial={boveda.data.saldoInicial}
              entradas={boveda.data.entradas}
              salidas={boveda.data.salidas}
              saldoFinal={boveda.data.saldoFinal}
            />
            <BovedaResumenCuenta
              texto={resumen.data?.texto}
              loading={resumen.isLoading}
              isError={resumen.isError}
              error={resumen.error}
            />
          </CredixPanel>

          {!boveda.data.indCierre ? (
            <CredixPanel title="Operaciones">
              <div className="boveda-operaciones-panel">
                <BovedaOperacionesPanel
                  oficinaId={oficinaId}
                  boveda={boveda.data}
                  existeTemporal={temporal.data?.existe ?? false}
                />
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

          <CredixPanel title="Historial y movimientos">
            <BovedaHistorialGrillas
              oficinaId={oficinaId}
              bovedaAbiertaId={boveda.data.bovedaId}
            />
          </CredixPanel>
        </>
      ) : null}
    </CredixPage>
  )
}
