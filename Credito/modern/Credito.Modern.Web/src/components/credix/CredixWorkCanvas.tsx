import type { ReactNode } from 'react'

/** Tres zonas: contexto | trabajo | resultado. En móvil se apilan. */
export function CredixWorkCanvas({
  context,
  work,
  result,
}: {
  context?: ReactNode
  work: ReactNode
  result?: ReactNode
}) {
  const cls = [
    'credix-work-canvas',
    context && result ? 'credix-work-canvas--full' : '',
    !context && result ? 'credix-work-canvas--no-context' : '',
    context && !result ? 'credix-work-canvas--no-result' : '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={cls}>
      {context ? <section className="credix-work-canvas__context">{context}</section> : null}
      <section className="credix-work-canvas__work">{work}</section>
      {result ? <aside className="credix-work-canvas__result">{result}</aside> : null}
    </div>
  )
}
