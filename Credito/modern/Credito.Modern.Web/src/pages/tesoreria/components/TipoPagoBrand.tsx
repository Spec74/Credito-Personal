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

/** Orden estable: Efectivo → Yape(s) → Interbank(s) → BCP → Nación → Plin. */
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

function nombreOpcion(itemId: number, denominacion: string): string {
  return medioNombrePorItemId(itemId) ?? medioNombreMarca(denominacion)
}

/**
 * Catálogo operativo de medios (efectivo + marcas).
 * Central: billeteras Gerente/René. Sucursal: Yape/Interbank genéricos.
 * `label` ya trae el nombre amigable; el render también usa `value` (ItemId).
 */
export function buildTipoPagoOptions(
  items: { itemId: number; denominacion: string; desCorta?: string | null }[],
  opts?: { esCentral?: boolean },
): TipoPagoOption[] {
  const esCentral = opts?.esCentral === true
  return items
    .filter((t) => t.itemId > 0 && esMarcaTipoPago(t, esCentral))
    .map((t) => {
      const nombre = nombreOpcion(t.itemId, t.denominacion)
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

export function TipoPagoOptionLabel({
  denominacion,
  itemId,
}: {
  denominacion: string
  itemId?: number
}) {
  const nombre =
    (itemId != null && itemId > 0 ? medioNombrePorItemId(itemId) : null) ??
    medioNombreMarca(denominacion)
  const { variant, aria } = medioEtiquetaUi(
    itemId != null && itemId > 0
      ? medioNombrePorItemId(itemId) ?? denominacion
      : denominacion,
  )
  return (
    <span className="boveda-tipo-pago-opt" translate="no" title={nombre} aria-label={aria || nombre}>
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
  value?: string | number
  data?: { label?: ReactNode; value?: string | number; searchText?: string } | null
}): ReactNode {
  const itemIdRaw = option.data?.value ?? option.value
  const itemId =
    typeof itemIdRaw === 'number'
      ? itemIdRaw
      : typeof itemIdRaw === 'string' && /^\d+$/.test(itemIdRaw)
        ? Number(itemIdRaw)
        : undefined
  const fromData =
    typeof option.data?.label === 'string'
      ? option.data.label
      : (option.data?.searchText ?? '')
  const denominacion = typeof option.label === 'string' ? option.label : fromData
  if (!denominacion && itemId == null) return option.label
  return (
    <TipoPagoOptionLabel
      denominacion={denominacion || String(itemId ?? '')}
      itemId={itemId}
    />
  )
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
