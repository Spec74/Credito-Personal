import { useMemo, useState } from 'react'
import { formatMoney } from '../../utils/formatMoney'

const W = 640
const H = 220
const PAD = { top: 16, right: 16, bottom: 36, left: 52 }

export function DualMetricChart({
  puntos,
  ariaLabel,
}: {
  puntos: { fecha: string; etiqueta: string; cobrado: number; desembolsado: number }[]
  ariaLabel: string
}) {
  const [hover, setHover] = useState<number | null>(null)

  const geometry = useMemo(() => {
    if (puntos.length === 0) {
      return null
    }
    const innerW = W - PAD.left - PAD.right
    const innerH = H - PAD.top - PAD.bottom
    const max = Math.max(...puntos.flatMap((p) => [p.cobrado, p.desembolsado]), 0)
    const yMax = max <= 0 ? 1 : niceMax(max)
    const xAt = (i: number) =>
      PAD.left + (puntos.length === 1 ? innerW / 2 : (i / (puntos.length - 1)) * innerW)
    const yAt = (v: number) => PAD.top + innerH - (v / yMax) * innerH
    const line = (key: 'cobrado' | 'desembolsado') =>
      puntos
        .map((p, i) => `${i === 0 ? 'M' : 'L'} ${xAt(i).toFixed(2)} ${yAt(p[key]).toFixed(2)}`)
        .join(' ')
    const ticks = 4
    const yTicks = Array.from({ length: ticks + 1 }, (_, i) => (yMax / ticks) * i)
    const xTickIdx = uniqueTicks(puntos.length)
    return { innerH, yMax, xAt, yAt, lineCobrado: line('cobrado'), lineDesembolso: line('desembolsado'), yTicks, xTickIdx }
  }, [puntos])

  if (!geometry) {
    return <p className="dash-chart-empty">No hay movimiento en el período.</p>
  }

  const hovered = hover != null ? puntos[hover] : null

  return (
    <div className="dash-chart">
      <svg viewBox={`0 0 ${W} ${H}`} role="img" aria-label={ariaLabel} className="dash-chart-svg">
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
        <path d={geometry.lineDesembolso} className="dash-chart-line is-desembolso" />
        <path d={geometry.lineCobrado} className="dash-chart-line" />
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
      <div className="dash-chart-legend">
        <span className="is-cobrado">Cobrado</span>
        <span className="is-desembolso">Desembolsado</span>
      </div>
      {hovered ? (
        <div className="dash-chart-tooltip is-dual" role="status">
          <strong>{hovered.etiqueta}</strong>
          <span>Cobrado S/ {formatMoney(hovered.cobrado)}</span>
          <span>Desembolsado S/ {formatMoney(hovered.desembolsado)}</span>
        </div>
      ) : (
        <div className="dash-chart-tooltip is-hint">Pase el cursor para ver el detalle.</div>
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
