import type { ReactElement, ReactNode } from 'react'
import {
  esMarcaTipoPago,
  medioEtiquetaUi,
  medioNombreMarca,
  medioNombrePorItemId,
} from './bovedaResumenCuentaParse'
import { ResumenCuentaBrandLogo } from './resumenCuentaBrandIcons'

type TipoPagoOption = {
  value: number
  label: string
  searchText: string
}

/** Orden estable en combo: Efectivo → Yape(s) → Interbank(s) → BCP → Nación → Plin. */
const TIPO_PAGO_ORDEN: Record<number, number> = {
  1: 10,
  10: 20,
  11: 21,
  2: 22,
  12: 30,
  13: 31,
  3: 32,
  4: 40,
  5: 50,
  9: 60,
}

/**
 * Catálogo operativo de medios (efectivo + marcas).
 * Multisucursal: Central ve billeteras Gerente/René; sucursal ve Yape/Interbank genéricos.
 * Plin/BCP/Nación/Efectivo en todas.
 * El `label` ya va con Gerente/René para que el Select no dependa solo del render.
 */
export function buildTipoPagoOptions(
  items: { itemId: number; denominacion: string; desCorta?: string | null }[],
  opts?: { esCentral?: boolean },
): TipoPagoOption[] {
  const esCentral = opts?.esCentral === true
  return items
    .filter((t) => t.itemId > 0 && esMarcaTipoPago(t, esCentral))
    .map((t) => {
      const nombre =
        medioNombrePorItemId(t.itemId) ?? medioNombreMarca(t.denominacion)
      return {
        value: t.itemId,
        label: nombre,
        searchText: `${nombre} ${t.denominacion}`.trim(),
      }
    })
    .sort(
      (a, b) =>
        (TIPO_PAGO_ORDEN[a.value] ?? 100 + a.value) -
        (TIPO_PAGO_ORDEN[b.value] ?? 100 + b.value),
    )
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
