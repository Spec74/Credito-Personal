import type { ReactNode } from 'react'

/** Fila de filtros / acciones (paridad formularios inline del MVC). */
export function CredixFilterBar({
  children,
  className,
}: {
  children: ReactNode
  className?: string
}) {
  return (
    <div className={['credix-filter-bar', className].filter(Boolean).join(' ')}>
      {children}
    </div>
  )
}
