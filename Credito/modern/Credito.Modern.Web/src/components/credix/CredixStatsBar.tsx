import type { ReactNode } from 'react'

export interface CredixStatItem {
  value: ReactNode
  label: ReactNode
  /** Etiqueta secundaria bajo el valor (fecha, estado, etc.) */
  detail?: ReactNode
  tone?: 'default' | 'green' | 'red'
  onClick?: () => void
  active?: boolean
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
    <ul className={cls} translate="no">
      {items.map((item, i) => {
        const body = (
          <>
            <strong>{item.value}</strong>
            {item.detail != null && item.detail !== '' ? <small>{item.detail}</small> : null}
            <span className={item.tone === 'red' ? 'red' : item.tone === 'green' ? 'green' : ''}>
              {item.label}
            </span>
          </>
        )
        const liClass = [
          item.onClick ? 'credix-stats-action' : '',
          item.active ? 'credix-stats-action--on' : '',
        ]
          .filter(Boolean)
          .join(' ')
        return (
          <li key={i} className={liClass || undefined}>
            {item.onClick ? (
              <button type="button" onClick={item.onClick}>
                {body}
              </button>
            ) : (
              body
            )}
          </li>
        )
      })}
    </ul>
  )
}
