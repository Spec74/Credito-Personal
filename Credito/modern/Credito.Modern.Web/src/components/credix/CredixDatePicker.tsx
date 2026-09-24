import { DatePicker } from 'antd'
import type { Dayjs } from 'dayjs'
import type { ComponentProps } from 'react'

type AntDatePickerProps = ComponentProps<typeof DatePicker>

export type CredixDatePickerProps = Omit<
  AntDatePickerProps,
  'value' | 'defaultValue' | 'onChange' | 'multiple'
> & {
  value?: Dayjs | null
  defaultValue?: Dayjs | null
  onChange?: (date: Dayjs | null, dateString: string) => void
  multiple?: false
}

/**
 * DatePicker estándar Credix:
 * - 100% ancho
 * - inputReadOnly (evita teclado/zoom iOS)
 * - formato PE DD/MM/YYYY
 * - popup en body (no se recorta en paneles overflow:hidden)
 */
export function CredixDatePicker({ style, className, onChange, ...props }: CredixDatePickerProps) {
  return (
    <DatePicker
      inputReadOnly
      placement="bottomLeft"
      format="DD/MM/YYYY"
      className={['credix-date-picker', className].filter(Boolean).join(' ')}
      style={{ width: '100%', maxWidth: '100%', ...style }}
      getPopupContainer={() => document.body}
      {...(props as AntDatePickerProps)}
      onChange={(date, dateString) => {
        if (!onChange) return
        if (Array.isArray(date) || Array.isArray(dateString)) return
        onChange((date as Dayjs | null) ?? null, String(dateString ?? ''))
      }}
    />
  )
}
