import type { ReactNode } from 'react'

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
