import type { ReactElement, ReactNode } from 'react'
import type { OficinaMedioPago } from '../../../api/mediosPago'
import {
  esTipoPagoCobroPermitido,
  medioNombrePorItemId,
  ordenTipoPago,
  TIPO_PAGO_EFECTIVO,
} from '../../../utils/tipoPago'
import {
  esMarcaTipoPago,
  medioEtiquetaUi,
  medioNombreMarca,
} from './bovedaResumenCuentaParse'
import { ResumenCuentaBrandLogo } from './resumenCuentaBrandIcons'
import { marcaDesdeTipoPagoId } from './resumencuentabrands'

export type TipoPagoOption = {
  value: number
  label: string
  searchText: string
}

function nombreOpcion(itemId: number, denominacion: string): string {
  return medioNombrePorItemId(itemId) ?? medioNombreMarca(denominacion)
}

/**
 * Opciones desde OficinaMedioPago (por sucursal). Fuente canónica para cobro/bóveda UI.
 */
export function buildMediosOficinaOptions(
  medios: Pick<OficinaMedioPago, 'tipoPagoId' | 'nombre' | 'activo'>[],
  opts?: { esCentral?: boolean; ensureEfectivo?: boolean },
): TipoPagoOption[] {
  const esCentral = opts?.esCentral === true
  const ensureEfectivo = opts?.ensureEfectivo !== false
  const out = medios
    .filter(
      (m) =>
        m.activo &&
        m.tipoPagoId > 0 &&
        esTipoPagoCobroPermitido(m.tipoPagoId, esCentral),
    )
    .map((m) => {
      const label =
        medioNombrePorItemId(m.tipoPagoId) ??
        (m.nombre.trim() || `Tipo ${m.tipoPagoId}`)
      return {
        value: m.tipoPagoId,
        label,
        searchText: `${label} ${m.nombre}`.trim(),
      }
    })
    .sort((a, b) => ordenTipoPago(a.value) - ordenTipoPago(b.value))

  if (
    ensureEfectivo &&
    out.length > 0 &&
    !out.some((o) => o.value === TIPO_PAGO_EFECTIVO)
  ) {
    out.unshift({
      value: TIPO_PAGO_EFECTIVO,
      label: 'Efectivo',
      searchText: 'Efectivo',
    })
  }
  return out
}

/**
 * Fallback legado: ValorTabla TablaId=13 (global). Preferir buildMediosOficinaOptions.
 * Central: billeteras Gerente/René. Sucursal: Yape/Interbank genéricos.
 */
export function buildTipoPagoOptions(
  items: { itemId: number; denominacion: string; desCorta?: string | null }[],
  opts?: { esCentral?: boolean; oficinaId?: number },
): TipoPagoOption[] {
  const esCentral = opts?.esCentral === true
  const oficinaId = opts?.oficinaId
  return items
    .filter((t) => t.itemId > 0 && esMarcaTipoPago(t, esCentral, oficinaId))
    .map((t) => {
      const nombre = nombreOpcion(t.itemId, t.denominacion)
      return {
        value: t.itemId,
        label: nombre,
        searchText: `${nombre} ${t.denominacion}`.trim(),
      }
    })
    .sort((a, b) => ordenTipoPago(a.value) - ordenTipoPago(b.value))
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
  const fromTexto = medioEtiquetaUi(
    itemId != null && itemId > 0
      ? medioNombrePorItemId(itemId) ?? denominacion
      : denominacion,
  )
  // ItemId manda el logo (evita cruzar marcas si el texto falla).
  const variant =
    (itemId != null && itemId > 0 ? marcaDesdeTipoPagoId(itemId) : null) ?? fromTexto.variant
  return (
    <span className="boveda-tipo-pago-opt" translate="no" title={nombre} aria-label={fromTexto.aria || nombre}>
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
