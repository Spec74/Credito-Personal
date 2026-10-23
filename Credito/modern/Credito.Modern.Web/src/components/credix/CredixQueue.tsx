import type { ReactNode } from 'react'

/** Lista densa + panel de detalle. Sin selección, la lista usa todo el ancho. */
export function CredixQueue({
  list,
  detail,
  emptyDetail = 'Seleccione un registro',
}: {
  list: ReactNode
  detail?: ReactNode
  emptyDetail?: string
}) {
  const conDetalle = detail != null
  return (
    <div
      className={conDetalle ? 'credix-queue credix-queue--con-detalle' : 'credix-queue credix-queue--sin-detalle'}
      aria-label={conDetalle ? undefined : emptyDetail}
    >
      <div className="credix-queue__list">{list}</div>
      {conDetalle ? <div className="credix-queue__detail">{detail}</div> : null}
    </div>
  )
}
