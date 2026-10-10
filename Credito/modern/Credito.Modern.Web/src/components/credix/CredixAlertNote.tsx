import type { ReactNode } from 'react'

export function CredixAlertNote({
  children,
  strong,
}: {
  strong?: ReactNode
  children?: ReactNode
}) {
  return (
    <div className="credix-alert-note" role="status">
      {strong != null ? <strong>{strong}</strong> : null}
      {children}
    </div>
  )
}
