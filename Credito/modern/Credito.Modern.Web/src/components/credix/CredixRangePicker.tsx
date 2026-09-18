import { DatePicker } from 'antd'
import type { ComponentProps } from 'react'

type RangePickerProps = ComponentProps<typeof DatePicker.RangePicker>

/**
 * RangePicker con defaults pensados para móvil:
 * - 100% ancho
 * - inputReadOnly (evita teclado/zoom iOS)
 * - popup anclado; paneles apilados vía CSS global
 */
export function CredixRangePicker({
  style,
  className,
  ...props
}: RangePickerProps) {
  return (
    <DatePicker.RangePicker
      inputReadOnly
      placement="bottomLeft"
      format="DD/MM/YYYY"
      className={['credix-range-picker', className].filter(Boolean).join(' ')}
      style={{ width: '100%', maxWidth: '100%', ...style }}
      // body: las tarjetas de reporte usan overflow:hidden y recortaban el calendario
      getPopupContainer={() => document.body}
      {...props}
    />
  )
}
