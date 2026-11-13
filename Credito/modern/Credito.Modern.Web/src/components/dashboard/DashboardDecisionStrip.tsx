import { Link } from 'react-router-dom'
import type { DashboardDecisionItem } from '../../utils/dashboardDecisionStrip'

/** Franja de prioridades accionables (ventaja competitiva: qué hacer ahora). */
export function DashboardDecisionStrip({
  items,
  title = 'Qué decidir ahora',
}: {
  items: DashboardDecisionItem[]
  title?: string
}) {
  if (items.length === 0) return null

  return (
    <section className="dash-decision" aria-label={title}>
      <div className="dash-decision__head">
        <h3 className="dash-section-title">{title}</h3>
        <p className="dash-section-hint">Señales derivadas de mora, flujo, cobertura y ritmo de cobranza</p>
      </div>
      <ul className="dash-decision__list">
        {items.map((item) => (
          <li key={item.id} className={`dash-decision__card is-${item.tone}`}>
            <strong>{item.titulo}</strong>
            <p>{item.mensaje}</p>
            {item.to && item.cta ? (
              <Link to={item.to} className="dash-decision__cta">
                {item.cta}
              </Link>
            ) : null}
          </li>
        ))}
      </ul>
    </section>
  )
}
