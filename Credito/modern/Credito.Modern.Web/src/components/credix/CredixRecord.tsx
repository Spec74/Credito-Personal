import type { ReactNode } from 'react'

/** Ficha de un registro: cabecera + cuerpo + laterales. */
export function CredixRecord({
  header,
  body,
  side,
}: {
  header?: ReactNode
  body: ReactNode
  side?: ReactNode
}) {
  return (
    <article className="credix-record">
      {header ? <header className="credix-record__header">{header}</header> : null}
      <div className="credix-record__body">{body}</div>
      {side ? <aside className="credix-record__side">{side}</aside> : null}
    </article>
  )
}
