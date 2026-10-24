import type { ReactNode } from 'react'
import { esMarcaTipoPago, medioEtiquetaUi, medioNombreMarca } from './bovedaResumenCuentaParse'
import { ResumenCuentaBrandLogo } from './resumenCuentaBrandIcons'

type TipoPagoOption = {
  value: number
  label: string
  searchText: string
}

/** Opción de Select con marca bancaria (label string para filtro; icono vía optionRender). */
export function buildTipoPagoOptions(
  items: { itemId: number; denominacion: string }[],
): TipoPagoOption[] {
  return items.filter(esMarcaTipoPago).map((t) => ({
    value: t.itemId,
    label: t.denominacion,
    searchText: t.denominacion,
  }))
}

export function TipoPagoOptionLabel({ denominacion }: { denominacion: string }) {
  const { variant, aria } = medioEtiquetaUi(denominacion)
  const nombre = medioNombreMarca(denominacion)
  return (
    <span className="boveda-tipo-pago-opt" translate="no" title={nombre} aria-label={aria}>
      <ResumenCuentaBrandLogo variant={variant} etiqueta={nombre} size="sm" tone="mark" />
      <span className="boveda-tipo-pago-opt__text">{nombre}</span>
    </span>
  )
}

/** El dropdown de Ant Design vive en portal: no hereda translate="no" de la página. */
export function tipoPagoPopupRender(menu: ReactNode): ReactNode {
  return (
    <div className="medio-marca-dropdown" translate="no">
      {menu}
    </div>
  )
}

export function tipoPagoOptionRender(option: {
  label?: ReactNode
  data?: TipoPagoOption
}): ReactNode {
  const denominacion =
    typeof option.label === 'string'
      ? option.label
      : (option.data?.label ?? option.data?.searchText ?? '')
  if (!denominacion) return option.label
  return <TipoPagoOptionLabel denominacion={denominacion} />
}

export function TipoPagoCell({ tipoPago }: { tipoPago?: string | null }) {
  const raw = tipoPago?.trim()
  if (!raw) return <span>—</span>
  const { variant, aria } = medioEtiquetaUi(raw)
  const nombre = medioNombreMarca(raw)
  return (
    <span className="boveda-tipo-pago-cell" translate="no" title={aria} aria-label={aria}>
      <ResumenCuentaBrandLogo variant={variant} etiqueta={nombre} size="sm" />
      <span className="boveda-tipo-pago-cell__text">{nombre}</span>
    </span>
  )
}
