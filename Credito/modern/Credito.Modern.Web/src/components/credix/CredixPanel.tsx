import type { CSSProperties, ReactNode } from 'react'

export function CredixPanel({
  title,
  extra,
  children,
  className,
  style,
}: {
  title?: ReactNode
  extra?: ReactNode
  children: ReactNode
  className?: string
  style?: CSSProperties
}) {
  return (
    <div className={['credix-box', className].filter(Boolean).join(' ')} style={style}>
      {title != null && (
        <div className="credix-box-header credix-box-header--split">
          <h2>{title}</h2>
          {extra ? <div className="credix-box-header-extra">{extra}</div> : null}
        </div>
      )}
      <div className="credix-box-content">{children}</div>
    </div>
  )
}
