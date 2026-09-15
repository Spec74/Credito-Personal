import type { ReactNode } from 'react'

/** Tarjeta de informe con cabecera de marca (paridad .box del MVC Reporte/Credito). */
export function CredixReportBox({
  title,
  children,
  actions,
  className,
  icon,
}: {
  title: string
  children: ReactNode
  actions: ReactNode
  className?: string
  icon?: ReactNode
}) {
  return (
    <article
      className={['credix-report-card', className].filter(Boolean).join(' ')}
    >
      <header className="credix-report-card__header">
        {icon ? <span className="credix-report-card__icon" aria-hidden>{icon}</span> : null}
        <h3 className="credix-report-card__title">{title}</h3>
      </header>
      <div className="credix-report-card__body">{children}</div>
      <footer className="credix-report-card__footer">{actions}</footer>
    </article>
  )
}
