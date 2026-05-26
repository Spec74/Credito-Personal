import type { ReactNode } from 'react'

type Tone = 'default' | 'brand' | 'credito' | 'cxc' | 'pago' | 'arqueo'

export function CajaSection({
  kicker,
  title,
  subtitle,
  icon,
  tone = 'default',
  children,
  className,
}: {
  kicker?: string
  title: ReactNode
  subtitle?: ReactNode
  icon?: ReactNode
  tone?: Tone
  children: ReactNode
  className?: string
}) {
  return (
    <section
      className={[
        'caja-section',
        `caja-section--${tone}`,
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <header className="caja-section__head">
        {icon ? <span className="caja-section__icon">{icon}</span> : null}
        <div className="caja-section__titles">
          {kicker ? <span className="caja-section__kicker">{kicker}</span> : null}
          <h3 className="caja-section__title">{title}</h3>
          {subtitle ? (
            <div className="caja-section__subtitle">{subtitle}</div>
          ) : null}
        </div>
      </header>
      <div className="caja-section__body">{children}</div>
    </section>
  )
}
