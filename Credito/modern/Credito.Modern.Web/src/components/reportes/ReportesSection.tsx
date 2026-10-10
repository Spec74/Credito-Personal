import type { ReactNode } from 'react'

export function ReportesSection({
  title,
  description,
  children,
  className,
}: {
  title: string
  description?: string
  children: ReactNode
  className?: string
}) {
  return (
    <section className={['credix-reportes-section', className].filter(Boolean).join(' ')}>
      <header className="credix-reportes-section__head">
        <h2 className="credix-reportes-section__title">{title}</h2>
        {description ? <p className="credix-reportes-section__desc">{description}</p> : null}
      </header>
      <div className="credix-reporte-grid">{children}</div>
    </section>
  )
}
