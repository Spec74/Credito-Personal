import type { ReactNode } from 'react'
import { resolveResumenCuentaVariant } from './bovedaResumenCuentaParse'
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
  return items.map((t) => ({
    value: t.itemId,
    label: t.denominacion,
    searchText: t.denominacion,
  }))
}

export function TipoPagoOptionLabel({ denominacion }: { denominacion: string }) {
  const { variant, etiqueta } = resolveResumenCuentaVariant(denominacion)
  return (
    <span className="boveda-tipo-pago-opt">
      <ResumenCuentaBrandLogo
        variant={variant}
        etiqueta={etiqueta}
        className="boveda-tipo-pago-opt__icon"
        size={22}
      />
      <span className="boveda-tipo-pago-opt__text">{denominacion}</span>
    </span>
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
  const { variant, etiqueta } = resolveResumenCuentaVariant(raw)
  return (
    <span className="boveda-tipo-pago-cell">
      <ResumenCuentaBrandLogo
        variant={variant}
        etiqueta={etiqueta}
        className="boveda-tipo-pago-cell__icon"
        size={20}
      />
      <span className="boveda-tipo-pago-cell__text">{raw}</span>
    </span>
  )
}
