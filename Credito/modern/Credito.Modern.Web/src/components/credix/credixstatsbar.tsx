import type { ReactNode } from 'react'

export interface CredixStatItem {
  value: ReactNode
  label: ReactNode
  /** Etiqueta secundaria bajo el valor (fecha, estado, etc.) */
  detail?: ReactNode
  tone?: 'default' | 'green' | 'red'
}

export function CredixStatsBar({
  items,
  variant = 'default',
}: {
  items: CredixStatItem[]
  variant?: 'default' | 'module'
}) {
  if (items.length === 0) {
    return null
  }
  const cls =
    variant === 'module' ? 'credix-stats credix-stats--module' : 'credix-stats'
  return (
    <ul className={cls}>
      {items.map((item, i) => (
        <li key={i}>
          <strong>{item.value}</strong>
          {item.detail != null && item.detail !== '' ? (
            <small>{item.detail}</small>
          ) : null}
          <span className={item.tone === 'red' ? 'red' : item.tone === 'green' ? 'green' : ''}>
            {item.label}
          </span>
        </li>
      ))}
    </ul>
  )
}
