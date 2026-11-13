import { useId, useMemo, useState } from 'react'
import { formatMoney } from '../../utils/formatMoney'
import type { DashboardProductividadPunto } from '../../api/dashboard'
import {
  formatAxisMoney,
  niceMax,
  uniqueTicks,
} from '../../utils/dashboardChartGeometry'

const W = 640
const H = 228
const PAD = { top: 18, right: 16, bottom: 36, left: 52 }

export function CobranzaAreaChart({
  puntos,
}: {
  puntos: DashboardProductividadPunto[]
}) {
  const [hover, setHover] = useState<number | null>(null)
  const uid = useId().replace(/:/g, '')

  const geometry = useMemo(() => {
    if (puntos.length === 0) {
      return null
    }
    const innerW = W - PAD.left - PAD.right
    const innerH = H - PAD.top - PAD.bottom
    const montos = puntos.map((p) => p.montoCobrado)
    const max = Math.max(...montos, 0)
    const avg = montos.reduce((a, b) => a + b, 0) / montos.length
    const yMax = max <= 0 ? 1 : niceMax(Math.max(max, avg))
    const xAt = (i: number) =>
      PAD.left + (puntos.length === 1 ? innerW / 2 : (i / (puntos.length - 1)) * innerW)
    const yAt = (v: number) => PAD.top + innerH - (v / yMax) * innerH
    const line = puntos
      .map((p, i) => `${i === 0 ? 'M' : 'L'} ${xAt(i).toFixed(2)} ${yAt(p.montoCobrado).toFixed(2)}`)
      .join(' ')
    const area = `${line} L ${xAt(puntos.length - 1).toFixed(2)} ${(PAD.top + innerH).toFixed(2)} L ${xAt(0).toFixed(2)} ${(PAD.top + innerH).toFixed(2)} Z`
    const ticks = 4
    const yTicks = Array.from({ length: ticks + 1 }, (_, i) => (yMax / ticks) * i)
    const xTickIdx = uniqueTicks(puntos.length)
    return { innerH, yMax, xAt, yAt, line, area, yTicks, xTickIdx, avg }
  }, [puntos])

  if (!geometry) {
    return <p className="dash-chart-empty">No hay cobranza en los últimos 30 días.</p>
  }

  const hovered = hover != null ? puntos[hover] : null
  const vsAvg =
    hovered != null
      ? hovered.montoCobrado - geometry.avg
      : null

  return (
    <div className="dash-chart">
      <svg
        viewBox={`0 0 ${W} ${H}`}
        role="img"
        aria-label="Cobranza diaria de los últimos 30 días"
        className="dash-chart-svg"
      >
        <defs>
          <linearGradient id={`dashCobranzaFill-${uid}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--credix-chart-cobrado)" stopOpacity="0.28" />
            <stop offset="100%" stopColor="var(--credix-chart-cobrado)" stopOpacity="0.02" />
          </linearGradient>
        </defs>
        {geometry.yTicks.map((tick) => (
          <g key={tick}>
            <line
              x1={PAD.left}
              x2={W - PAD.right}
              y1={geometry.yAt(tick)}
              y2={geometry.yAt(tick)}
              className="dash-chart-grid"
            />
            <text x={PAD.left - 8} y={geometry.yAt(tick) + 4} className="dash-chart-axis" textAnchor="end">
              {formatAxisMoney(tick)}
            </text>
          </g>
        ))}
        <path d={geometry.area} fill={`url(#dashCobranzaFill-${uid})`} />
        <line
          x1={PAD.left}
          x2={W - PAD.right}
          y1={geometry.yAt(geometry.avg)}
          y2={geometry.yAt(geometry.avg)}
          className="dash-chart-avg"
        />
        <text
          x={W - PAD.right}
          y={geometry.yAt(geometry.avg) - 6}
          className="dash-chart-avg-label"
          textAnchor="end"
        >
          Prom. S/ {formatMoney(geometry.avg)}
        </text>
        <path d={geometry.line} className="dash-chart-line" />
        {geometry.xTickIdx.map((i) => (
          <text
            key={puntos[i].fecha}
            x={geometry.xAt(i)}
            y={H - 12}
            className="dash-chart-axis"
            textAnchor="middle"
          >
            {puntos[i].etiqueta}
          </text>
        ))}
        {puntos.map((p, i) => (
          <circle
            key={p.fecha}
            cx={geometry.xAt(i)}
            cy={geometry.yAt(p.montoCobrado)}
            r={hover === i ? 5 : 3}
            className={hover === i ? 'dash-chart-dot is-active' : 'dash-chart-dot'}
          />
        ))}
        {puntos.map((p, i) => (
          <rect
            key={`${p.fecha}-hit`}
            x={geometry.xAt(i) - (puntos.length > 1 ? (W - PAD.left - PAD.right) / (puntos.length - 1) / 2 : 12)}
            y={PAD.top}
            width={puntos.length > 1 ? (W - PAD.left - PAD.right) / (puntos.length - 1) : 24}
            height={geometry.innerH}
            fill="transparent"
            onMouseEnter={() => setHover(i)}
            onMouseLeave={() => setHover(null)}
          />
        ))}
      </svg>
      {hovered && vsAvg != null ? (
        <div className="dash-chart-tooltip" role="status">
          <strong>
            {hovered.etiqueta} · {hovered.fecha.slice(0, 4)}
          </strong>
          <span>S/ {formatMoney(hovered.montoCobrado)}</span>
          <span className={vsAvg >= 0 ? 'is-pos' : 'is-neg'}>
            {vsAvg >= 0 ? '+' : '−'}S/ {formatMoney(Math.abs(vsAvg))} vs promedio
          </span>
        </div>
      ) : (
        <div className="dash-chart-tooltip is-hint">
          Línea punteada = promedio del período. Pase el cursor para el detalle diario.
        </div>
      )}
    </div>
  )
}
