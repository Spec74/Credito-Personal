import { useMemo } from 'react'
import { formatMoney } from '../../utils/formatMoney'

type Segment = { key: string; label: string; value: number; color: string }

const W = 220
const H = 200
const CX = 110
const CY = 100
const R = 78
const R_INNER = 48

/**
 * Paleta de composición (marca Credix, sin naranja):
 * - Sin mora → verde éxito corporativo
 * - Con mora → vino / riesgo medio (no naranja)
 * - Vencido → rojo peligro
 */
const MIX_COLORS = {
  sinMora: '#15803d',
  conMora: '#9f1239',
  vencido: '#b91c1c',
} as const

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
        {
          key: 'ok',
          label: 'Sin mora',
          value: Math.max(0, sinMora),
          color: MIX_COLORS.sinMora,
        },
        {
          key: 'mora',
          label: 'Con mora',
          value: Math.max(0, conMora),
          color: MIX_COLORS.conMora,
        },
        {
          key: 'venc',
          label: 'Vencido',
          value: Math.max(0, vencido),
          color: MIX_COLORS.vencido,
        },
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
      return {
        ...s,
        d: donutPath(CX, CY, R, R_INNER, start, end),
        pct: (s.value / total) * 100,
      }
    })
  }, [segments, total])

  if (total <= 0) {
    return <p className="dash-chart-empty">Sin saldo de cartera para graficar.</p>
  }

  return (
    <div className="dash-chart dash-chart-mix">
      <div className="dash-chart-mix__visual">
        <svg
          viewBox={`0 0 ${W} ${H}`}
          role="img"
          aria-label={ariaLabel}
          className="dash-chart-svg"
        >
          {arcs.map((a) => (
            <path
              key={a.key}
              d={a.d}
              fill={a.color}
              stroke="#fff"
              strokeWidth={2}
            />
          ))}
          <text
            x={CX}
            y={CY - 6}
            textAnchor="middle"
            className="dash-chart-mix__center-label"
          >
            Cartera
          </text>
          <text
            x={CX}
            y={CY + 14}
            textAnchor="middle"
            className="dash-chart-mix__center-value"
          >
            S/ {formatAxis(total)}
          </text>
        </svg>
      </div>
      <ul className="dash-chart-mix__legend">
        {arcs.map((a) => (
          <li key={a.key}>
            <span
              className="dash-chart-mix__swatch"
              style={{ background: a.color }}
              aria-hidden
            />
            <div className="dash-chart-mix__legend-copy">
              <strong>
                {a.label}
                <span className="dash-chart-mix__pct">{a.pct.toFixed(0)}%</span>
              </strong>
              <span>S/ {formatMoney(a.value)}</span>
            </div>
          </li>
        ))}
      </ul>
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
