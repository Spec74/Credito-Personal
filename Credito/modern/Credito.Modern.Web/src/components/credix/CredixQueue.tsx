import type { ReactNode } from 'react'

/** Lista densa + panel de detalle. */
export function CredixQueue({
  list,
  detail,
  emptyDetail = 'Seleccione un registro',
}: {
  list: ReactNode
  detail?: ReactNode
  emptyDetail?: string
}) {
  return (
    <div className="credix-queue">
      <div className="credix-queue__list">{list}</div>
      <div className="credix-queue__detail">{detail ?? <p className="credix-queue__empty">{emptyDetail}</p>}</div>
    </div>
  )
}
