import { useMemo, useState } from 'react'
import { formatMoney } from '../../utils/formatMoney'
import type { DashboardProductividadPunto } from '../../api/dashboard'

const W = 640
const H = 220
const PAD = { top: 16, right: 16, bottom: 36, left: 52 }

export function CobranzaAreaChart({
  puntos,
}: {
  puntos: DashboardProductividadPunto[]
}) {
  const [hover, setHover] = useState<number | null>(null)

  const geometry = useMemo(() => {
    if (puntos.length === 0) {
      return null
    }
    const innerW = W - PAD.left - PAD.right
    const innerH = H - PAD.top - PAD.bottom
    const max = Math.max(...puntos.map((p) => p.montoCobrado), 0)
    const yMax = max <= 0 ? 1 : niceMax(max)
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
    return { innerH, yMax, xAt, yAt, line, area, yTicks, xTickIdx }
  }, [puntos])

  if (!geometry) {
    return <p className="dash-chart-empty">No hay cobranza en los últimos 30 días.</p>
  }

  const hovered = hover != null ? puntos[hover] : null

  return (
    <div className="dash-chart">
      <svg
        viewBox={`0 0 ${W} ${H}`}
        role="img"
        aria-label="Cobranza diaria de los últimos 30 días"
        className="dash-chart-svg"
      >
        <defs>
          <linearGradient id="dashCobranzaFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#114885" stopOpacity="0.28" />
            <stop offset="100%" stopColor="#114885" stopOpacity="0.02" />
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
        <path d={geometry.area} fill="url(#dashCobranzaFill)" />
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
      {hovered ? (
        <div className="dash-chart-tooltip" role="status">
          <strong>
            {hovered.etiqueta} · {hovered.fecha.slice(0, 4)}
          </strong>
          <span>S/ {formatMoney(hovered.montoCobrado)}</span>
        </div>
      ) : (
        <div className="dash-chart-tooltip is-hint">Pase el cursor sobre la línea para ver el detalle diario.</div>
      )}
    </div>
  )
}

function formatAxisMoney(value: number): string {
  if (value >= 1000) {
    return `S/ ${(value / 1000).toFixed(value >= 10000 ? 0 : 1)}k`
  }
  return `S/ ${value.toFixed(0)}`
}

function niceMax(max: number): number {
  const exp = Math.floor(Math.log10(max))
  const base = 10 ** exp
  const n = max / base
  const nice = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10
  return nice * base
}

function uniqueTicks(length: number): number[] {
  if (length <= 6) {
    return Array.from({ length }, (_, i) => i)
  }
  const last = length - 1
  const step = Math.ceil(last / 5)
  const ticks = [0]
  for (let i = step; i < last; i += step) {
    ticks.push(i)
  }
  if (ticks[ticks.length - 1] !== last) {
    ticks.push(last)
  }
  return ticks
}
