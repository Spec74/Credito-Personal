import { Link } from 'react-router-dom'
import type { CobranzaDecision, CobranzaInsights, CobranzaSegmento } from './cobranzaPagosInsights'

const SEGMENTS: { id: CobranzaSegmento; label: string; countKey: keyof CobranzaInsights | 'todos' }[] =
  [
    { id: 'todos', label: 'Todos', countKey: 'todos' },
    { id: 'mora', label: 'En mora', countKey: 'clientesMora' },
    { id: 'mora_grave', label: '≥ 30 días', countKey: 'clientesMoraGrave' },
    { id: 'al_dia', label: 'Al día', countKey: 'clientesAlDia' },
    { id: 'sin_saldo', label: 'Sin saldo', countKey: 'clientesSinSaldo' },
  ]

export function CobranzaSegmentChips({
  insights,
  total,
  value,
  onChange,
}: {
  insights: CobranzaInsights
  total: number
  value: CobranzaSegmento
  onChange: (s: CobranzaSegmento) => void
}) {
  return (
    <div className="credix-cobranza-segments" role="group" aria-label="Filtrar cartera">
      {SEGMENTS.map((s) => {
        const count = s.countKey === 'todos' ? total : (insights[s.countKey] as number)
        return (
          <button
            key={s.id}
            type="button"
            className={`credix-cobranza-segment${value === s.id ? ' is-active' : ''}`}
            onClick={() => onChange(s.id)}
          >
            {s.label}
            <strong>{count}</strong>
          </button>
        )
      })}
    </div>
  )
}

export function CobranzaDecisionStrip({ decisions }: { decisions: CobranzaDecision[] }) {
  if (decisions.length === 0) return null
  return (
    <section className="credix-cobranza-decisions" aria-label="Qué decidir ahora">
      <div className="credix-cobranza-decisions__head">
        <h3>Qué decidir ahora</h3>
        <p>Señales derivadas de mora, saldo y recuperación de esta cartera</p>
      </div>
      <ul className="credix-cobranza-decisions__list">
        {decisions.map((d) => (
          <li key={d.id} className={`credix-cobranza-decision is-${d.tone}`}>
            <strong>{d.titulo}</strong>
            <p>{d.mensaje}</p>
            {d.to && d.cta ? (
              <Link to={d.to} className="credix-cobranza-decision__cta">
                {d.cta}
              </Link>
            ) : null}
          </li>
        ))}
      </ul>
    </section>
  )
}

/** Barra de composición: al día / mora / liquidado. */
export function CobranzaMixBar({ insights }: { insights: CobranzaInsights }) {
  const total =
    insights.clientesAlDia + insights.clientesMora + insights.clientesSinSaldo
  if (total <= 0) return null
  const pct = (n: number) => Math.round((n / total) * 1000) / 10
  return (
    <div className="credix-cobranza-mix" aria-label="Composición de clientes">
      <div className="credix-cobranza-mix__bar" role="img" aria-hidden>
        {insights.clientesAlDia > 0 ? (
          <span className="is-ok" style={{ width: `${pct(insights.clientesAlDia)}%` }} />
        ) : null}
        {insights.clientesMora > 0 ? (
          <span className="is-mora" style={{ width: `${pct(insights.clientesMora)}%` }} />
        ) : null}
        {insights.clientesSinSaldo > 0 ? (
          <span className="is-done" style={{ width: `${pct(insights.clientesSinSaldo)}%` }} />
        ) : null}
      </div>
      <ul className="credix-cobranza-mix__legend">
        <li>
          <i className="is-ok" aria-hidden /> Al día {insights.clientesAlDia} (
          {pct(insights.clientesAlDia)}%)
        </li>
        <li>
          <i className="is-mora" aria-hidden /> En mora {insights.clientesMora} (
          {pct(insights.clientesMora)}%)
        </li>
        <li>
          <i className="is-done" aria-hidden /> Sin saldo {insights.clientesSinSaldo} (
          {pct(insights.clientesSinSaldo)}%)
        </li>
      </ul>
    </div>
  )
}
