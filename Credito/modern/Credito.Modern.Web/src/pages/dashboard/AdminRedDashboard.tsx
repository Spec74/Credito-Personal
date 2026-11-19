import { useMemo, type ReactNode } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import {
  ReloadOutlined,
  RiseOutlined,
  SwapOutlined,
  WalletOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { Alert, Button, Skeleton, Tag } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchDashboardAdminRed,
  type DashboardAdminRedSedeRow,
} from '../../api/dashboard'
import { ApiError } from '../../api/errors'
import { CredixDataTable, CredixPage } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/dashboard-analista.css'

const queryOpts = {
  staleTime: 5 * 60_000,
  gcTime: 15 * 60_000,
  placeholderData: keepPreviousData,
} as const

export function AdminRedDashboard({
  sedeSelect,
  onDrillSede,
}: {
  sedeSelect: ReactNode
  onDrillSede: (sedeId: number) => void
}) {
  const redQuery = useQuery({
    queryKey: ['dashboard-admin-red'],
    queryFn: fetchDashboardAdminRed,
    ...queryOpts,
    retry: 1,
  })

  const columns: ColumnsType<DashboardAdminRedSedeRow> = useMemo(
    () => [
      {
        title: 'Sede',
        dataIndex: 'nombreOficina',
        key: 'nombre',
        fixed: 'left',
        width: 180,
        render: (nombre: string, row) => (
          <button
            type="button"
            className="dash-red-drill"
            onClick={() => onDrillSede(row.oficinaId)}
          >
            {nombre}
          </button>
        ),
      },
      {
        title: 'Cobrado hoy',
        dataIndex: 'cobradoHoy',
        key: 'cobradoHoy',
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
        sorter: (a, b) => a.cobradoHoy - b.cobradoHoy,
        defaultSortOrder: 'descend',
      },
      {
        title: 'vs ayer',
        dataIndex: 'variacionCobradoHoyPct',
        key: 'var',
        align: 'right',
        width: 100,
        render: (pct: number | null) => <Variacion pct={pct} />,
      },
      {
        title: 'Desembolso',
        dataIndex: 'desembolsoHoy',
        key: 'desembolso',
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Flujo neto',
        dataIndex: 'flujoNetoHoy',
        key: 'flujo',
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Mora %',
        dataIndex: 'porcentajeMora',
        key: 'mora',
        align: 'right',
        width: 90,
        render: (v: number) => `${v.toFixed(1)}%`,
      },
      {
        title: 'Por vencer 7d',
        dataIndex: 'creditosPorVencerSemana',
        key: 'vencer',
        align: 'right',
        width: 110,
        render: (v: number) => formatEntero(v),
      },
      {
        title: 'Analistas',
        dataIndex: 'totalAnalistas',
        key: 'analistas',
        align: 'right',
        width: 90,
        render: (v: number) => formatEntero(v),
      },
      {
        title: 'Semáforo',
        dataIndex: 'semaforo',
        key: 'semaforo',
        width: 140,
        render: (s: string, row) => (
          <Tag
            className={`dash-semaforo is-${s}`}
            title={row.motivoSemaforo}
            color={s === 'rojo' ? 'error' : s === 'ambar' ? 'warning' : 'success'}
          >
            {s === 'rojo' ? 'Rojo' : s === 'ambar' ? 'Ámbar' : 'Verde'}
          </Tag>
        ),
      },
    ],
    [onDrillSede],
  )

  if (redQuery.isLoading) {
    return (
      <CredixPage title="Inicio" subtitle="Consolidando la red…">
        <div className="dash-analista dash-admin dash-admin-red">
          <header className="dash-head">
            <div>
              <p className="dash-kicker">Tablero gerencial · red</p>
              <h2 className="dash-hello">Cargando consolidado…</h2>
            </div>
          </header>
          <Skeleton active paragraph={{ rows: 10 }} />
        </div>
      </CredixPage>
    )
  }

  if (redQuery.isError || !redQuery.data) {
    return (
      <CredixPage
        title="Inicio"
        subtitle="No se pudo cargar el consolidado de red."
        actions={
          <>
            {sedeSelect}
            <Button icon={<ReloadOutlined />} onClick={() => void redQuery.refetch()}>
              Reintentar
            </Button>
          </>
        }
      >
        <Alert type="error" showIcon message={errMsg(redQuery.error)} />
      </CredixPage>
    )
  }

  const red = redQuery.data
  const fechaLarga = formatFechaLarga(red.fechaConsulta)

  return (
    <CredixPage
      title="Inicio"
      subtitle={`Consolidado de ${formatEntero(red.totalSedes)} sedes · cobranza, colocación y mora`}
      actions={
        <>
          {sedeSelect}
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void redQuery.refetch()}
            loading={redQuery.isFetching}
          >
            Actualizar
          </Button>
        </>
      }
    >
      <div className="dash-analista dash-admin dash-admin-red">
        {redQuery.isFetching ? (
          <p className="dash-refresh-hint" role="status">
            Actualizando consolidado de red…
          </p>
        ) : null}
        <header className="dash-head">
          <div>
            <p className="dash-kicker">Tablero gerencial · red</p>
            <h2 className="dash-hello">Toda la red</h2>
            <p className="dash-scope" role="status">
              Elija una sede en el selector o haga clic en una fila para ver el tablero operativo.
            </p>
          </div>
          <div className="dash-date">{fechaLarga}</div>
        </header>

        <h3 className="dash-section-title">Estado de la red</h3>
        <section className="dash-kpis dash-kpis-4" aria-label="Estado de la red">
          <KpiCard
            accent="var(--credix-success)"
            icon={<WalletOutlined />}
            label="Cobrado hoy"
            value={`S/ ${formatMoney(red.cobradoHoy)}`}
            meta={
              <>
                <Variacion pct={red.variacionCobradoHoyPct} />
                <span>Ayer S/ {formatMoney(red.cobradoAyer)}</span>
              </>
            }
          />
          <KpiCard
            accent="var(--credix-info)"
            icon={<RiseOutlined />}
            label="Desembolso hoy"
            value={`S/ ${formatMoney(red.desembolsoHoy)}`}
            meta={<span>Ayer S/ {formatMoney(red.desembolsoAyer)}</span>}
          />
          <KpiCard
            accent="var(--credix-ink-brand)"
            icon={<SwapOutlined />}
            label="Flujo neto hoy"
            value={`S/ ${formatMoney(red.flujoNetoHoy)}`}
            meta={<span>Ayer S/ {formatMoney(red.flujoNetoAyer)}</span>}
          />
          <KpiCard
            accent="var(--credix-danger)"
            icon={<WarningOutlined />}
            label="Mora red"
            value={`${red.porcentajeMoraRed.toFixed(1)}%`}
            meta={
              <span>
                {formatEntero(red.clientesMora)} clientes · {formatEntero(red.creditosPorVencerSemana)}{' '}
                por vencer
              </span>
            }
          />
        </section>

        <section className="dash-semaforo-strip" aria-label="Semáforo de sedes">
          <div className="dash-semaforo-chip is-verde">
            <strong>{formatEntero(red.sedesEnVerde)}</strong>
            <span>en verde</span>
          </div>
          <div className="dash-semaforo-chip is-ambar">
            <strong>{formatEntero(red.sedesEnAmbar)}</strong>
            <span>en ámbar</span>
          </div>
          <div className="dash-semaforo-chip is-rojo">
            <strong>{formatEntero(red.sedesEnRojo)}</strong>
            <span>en rojo</span>
          </div>
          <div className="dash-semaforo-chip">
            <strong>{formatEntero(red.totalAnalistas)}</strong>
            <span>analistas · {formatEntero(red.totalClientes)} clientes</span>
          </div>
        </section>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Liga de sedes</h2>
              <p>Clic en la sede para abrir su tablero operativo</p>
            </div>
          </div>
          <div className="dash-panel-body">
            <CredixDataTable
              rowKey={(row) => String(row.oficinaId)}
              columns={columns}
              dataSource={[...red.sedes]}
              pagination={false}
              size="middle"
              scroll={{ x: 1100 }}
            />
          </div>
        </section>
      </div>
    </CredixPage>
  )
}

function KpiCard({
  accent,
  icon,
  label,
  value,
  meta,
}: {
  accent: string
  icon: ReactNode
  label: string
  value: string
  meta: ReactNode
}) {
  return (
    <article className="dash-kpi" style={{ ['--dash-accent' as string]: accent }}>
      <div className="dash-kpi-label">
        {icon} {label}
      </div>
      <div className="dash-kpi-value">{value}</div>
      <div className="dash-kpi-meta">{meta}</div>
    </article>
  )
}

function Variacion({ pct }: { pct: number | null }) {
  if (pct == null) {
    return <span className="dash-var is-new">Nuevo</span>
  }
  if (pct > 0) {
    return <span className="dash-var is-up">▲ {pct.toFixed(1)}%</span>
  }
  if (pct < 0) {
    return <span className="dash-var is-down">▼ {Math.abs(pct).toFixed(1)}%</span>
  }
  return <span className="dash-var">● 0.0%</span>
}

function formatEntero(value: number): string {
  return value.toLocaleString('es-PE')
}

function formatFechaLarga(iso: string): string {
  const d = iso.slice(0, 10)
  const [y, m, day] = d.split('-').map(Number)
  if (!y || !m || !day) {
    return iso
  }
  return new Date(y, m - 1, day).toLocaleDateString('es-PE', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'No se pudieron obtener los indicadores gerenciales.'
}
