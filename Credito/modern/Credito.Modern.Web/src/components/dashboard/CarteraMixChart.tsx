import { useMemo } from 'react'
import { formatMoney } from '../../utils/formatMoney'

type Segment = { key: string; label: string; value: number; color: string }

const W = 320
const H = 200
const CX = 100
const CY = 100
const R = 72
const R_INNER = 42

/** Composición de cartera: sin mora / con mora / vencido (donut). */
export function CarteraMixChart({
  sinMora,
  conMora,
  vencido,
  ariaLabel = 'Composición de cartera',
}: {
  sinMora: number
  conMora: number
  vencido: number
  ariaLabel?: string
}) {
  const segments = useMemo<Segment[]>(
    () =>
      [
        { key: 'ok', label: 'Sin mora', value: Math.max(0, sinMora), color: '#059669' },
        { key: 'mora', label: 'Con mora', value: Math.max(0, conMora), color: '#d97706' },
        { key: 'venc', label: 'Vencido', value: Math.max(0, vencido), color: '#dc2626' },
      ].filter((s) => s.value > 0),
    [sinMora, conMora, vencido],
  )

  const total = segments.reduce((a, s) => a + s.value, 0)

  const arcs = useMemo(() => {
    if (total <= 0 || segments.length === 0) {
      return []
    }
    let angle = -Math.PI / 2
    return segments.map((s) => {
      const sweep = (s.value / total) * Math.PI * 2
      const start = angle
      const end = angle + sweep
      angle = end
      return { ...s, d: donutPath(CX, CY, R, R_INNER, start, end), pct: (s.value / total) * 100 }
    })
  }, [segments, total])

  if (total <= 0) {
    return <p className="dash-chart-empty">Sin saldo de cartera para graficar.</p>
  }

  return (
    <div className="dash-chart dash-chart-mix">
      <svg viewBox={`0 0 ${W} ${H}`} role="img" aria-label={ariaLabel} className="dash-chart-svg">
        {arcs.map((a) => (
          <path key={a.key} d={a.d} fill={a.color} stroke="#fff" strokeWidth={1.5} />
        ))}
        <text x={CX} y={CY - 4} textAnchor="middle" className="dash-chart-axis" fontWeight={700}>
          Cartera
        </text>
        <text x={CX} y={CY + 12} textAnchor="middle" className="dash-chart-axis">
          S/ {formatAxis(total)}
        </text>
        {arcs.map((a, i) => (
          <g key={`leg-${a.key}`}>
            <rect x={190} y={36 + i * 28} width={10} height={10} rx={2} fill={a.color} />
            <text x={206} y={45 + i * 28} className="dash-chart-axis">
              {a.label} {a.pct.toFixed(0)}%
            </text>
            <text x={206} y={58 + i * 28} className="dash-chart-axis" opacity={0.75}>
              S/ {formatMoney(a.value)}
            </text>
          </g>
        ))}
      </svg>
    </div>
  )
}

function formatAxis(value: number): string {
  if (value >= 1000) {
    return `${(value / 1000).toFixed(value >= 10000 ? 0 : 1)}k`
  }
  return value.toFixed(0)
}

function donutPath(
  cx: number,
  cy: number,
  rOuter: number,
  rInner: number,
  start: number,
  end: number,
): string {
  const large = end - start > Math.PI ? 1 : 0
  const x0 = cx + Math.cos(start) * rOuter
  const y0 = cy + Math.sin(start) * rOuter
  const x1 = cx + Math.cos(end) * rOuter
  const y1 = cy + Math.sin(end) * rOuter
  const x2 = cx + Math.cos(end) * rInner
  const y2 = cy + Math.sin(end) * rInner
  const x3 = cx + Math.cos(start) * rInner
  const y3 = cy + Math.sin(start) * rInner
  return [
    `M ${x0} ${y0}`,
    `A ${rOuter} ${rOuter} 0 ${large} 1 ${x1} ${y1}`,
    `L ${x2} ${y2}`,
    `A ${rInner} ${rInner} 0 ${large} 0 ${x3} ${y3}`,
    'Z',
  ].join(' ')
}
