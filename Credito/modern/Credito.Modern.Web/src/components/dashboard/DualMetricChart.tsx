import { useId, useMemo, useState } from 'react'
import { formatMoney } from '../../utils/formatMoney'
import {
  formatAxisMoney,
  niceMax,
  uniqueTicks,
} from '../../utils/dashboardChartGeometry'

const W = 640
const H = 228
const PAD = { top: 18, right: 16, bottom: 36, left: 52 }

export function DualMetricChart({
  puntos,
  ariaLabel,
}: {
  puntos: { fecha: string; etiqueta: string; cobrado: number; desembolsado: number }[]
  ariaLabel: string
}) {
  const [hover, setHover] = useState<number | null>(null)
  const uid = useId().replace(/:/g, '')

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
    const area = (key: 'cobrado' | 'desembolsado') => {
      const linePath = line(key)
      return `${linePath} L ${xAt(puntos.length - 1).toFixed(2)} ${(PAD.top + innerH).toFixed(2)} L ${xAt(0).toFixed(2)} ${(PAD.top + innerH).toFixed(2)} Z`
    }
    const ticks = 4
    const yTicks = Array.from({ length: ticks + 1 }, (_, i) => (yMax / ticks) * i)
    const xTickIdx = uniqueTicks(puntos.length)
    const gapNeto = puntos.reduce((acc, p) => acc + (p.cobrado - p.desembolsado), 0)
    return {
      innerH,
      yMax,
      xAt,
      yAt,
      lineCobrado: line('cobrado'),
      lineDesembolso: line('desembolsado'),
      areaCobrado: area('cobrado'),
      areaDesembolso: area('desembolsado'),
      yTicks,
      xTickIdx,
      gapNeto,
    }
  }, [puntos])

  if (!geometry) {
    return <p className="dash-chart-empty">No hay movimiento en el período.</p>
  }

  const hovered = hover != null ? puntos[hover] : null
  const hoverGap = hovered ? hovered.cobrado - hovered.desembolsado : null

  return (
    <div className="dash-chart">
      <svg viewBox={`0 0 ${W} ${H}`} role="img" aria-label={ariaLabel} className="dash-chart-svg">
        <defs>
          <linearGradient id={`dualCobradoFill-${uid}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--credix-ink-brand)" stopOpacity="0.22" />
            <stop offset="100%" stopColor="var(--credix-ink-brand)" stopOpacity="0.01" />
          </linearGradient>
          <linearGradient id={`dualDesembolsoFill-${uid}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--credix-info)" stopOpacity="0.16" />
            <stop offset="100%" stopColor="var(--credix-info)" stopOpacity="0.01" />
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
        <path d={geometry.areaDesembolso} fill={`url(#dualDesembolsoFill-${uid})`} />
        <path d={geometry.areaCobrado} fill={`url(#dualCobradoFill-${uid})`} />
        <path d={geometry.lineDesembolso} className="dash-chart-line is-desembolso" />
        <path d={geometry.lineCobrado} className="dash-chart-line" />
        {hover != null ? (
          <>
            <line
              x1={geometry.xAt(hover)}
              x2={geometry.xAt(hover)}
              y1={PAD.top}
              y2={PAD.top + geometry.innerH}
              className="dash-chart-crosshair"
            />
            <circle
              cx={geometry.xAt(hover)}
              cy={geometry.yAt(puntos[hover].cobrado)}
              r={4.5}
              className="dash-chart-dot is-active"
            />
            <circle
              cx={geometry.xAt(hover)}
              cy={geometry.yAt(puntos[hover].desembolsado)}
              r={4.5}
              className="dash-chart-dot is-desembolso is-active"
            />
          </>
        ) : null}
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
        <span className="is-gap">
          Neto período:{' '}
          <strong className={geometry.gapNeto >= 0 ? 'is-pos' : 'is-neg'}>
            {geometry.gapNeto >= 0 ? '+' : '−'}S/ {formatMoney(Math.abs(geometry.gapNeto))}
          </strong>
        </span>
      </div>
      {hovered && hoverGap != null ? (
        <div className="dash-chart-tooltip is-dual" role="status">
          <strong>{hovered.etiqueta}</strong>
          <span>Cobrado S/ {formatMoney(hovered.cobrado)}</span>
          <span>Desembolsado S/ {formatMoney(hovered.desembolsado)}</span>
          <span className={hoverGap >= 0 ? 'is-pos' : 'is-neg'}>
            Gap {hoverGap >= 0 ? '+' : '−'}S/ {formatMoney(Math.abs(hoverGap))}
          </span>
        </div>
      ) : (
        <div className="dash-chart-tooltip is-hint">Pase el cursor para ver cobrado, desembolso y gap del día.</div>
      )}
    </div>
  )
}
