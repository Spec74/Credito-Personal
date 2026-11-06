import type { ReactElement, ReactNode } from 'react'
import { esMarcaTipoPago, medioEtiquetaUi, medioNombreMarca } from './bovedaResumenCuentaParse'
import { ResumenCuentaBrandLogo } from './resumenCuentaBrandIcons'

type TipoPagoOption = {
  value: number
  label: string
  searchText: string
}

/**
 * Catálogo operativo de medios (efectivo + marcas).
 * Omite placeholder ItemId≤0 y variantes por sede del legado
 * («YAPE HUANTA», «INTERBANK HUANTA», …) que duplicaban el chip visual.
 */
export function buildTipoPagoOptions(
  items: { itemId: number; denominacion: string; desCorta?: string | null }[],
): TipoPagoOption[] {
  return items
    .filter((t) => t.itemId > 0 && esMarcaTipoPago(t))
    .map((t) => ({
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
export function tipoPagoPopupRender(menu: ReactElement): ReactElement {
  return (
    <div className="medio-marca-dropdown" translate="no">
      {menu}
    </div>
  )
}

/** Firma ancha: Ant Select no siempre tipa `data` como TipoPagoOption. */
export function tipoPagoOptionRender(option: {
  label?: ReactNode
  data?: { label?: ReactNode; searchText?: string } | null
}): ReactNode {
  const fromData =
    typeof option.data?.label === 'string'
      ? option.data.label
      : (option.data?.searchText ?? '')
  const denominacion = typeof option.label === 'string' ? option.label : fromData
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
