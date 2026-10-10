import { useState, type ComponentProps } from 'react'
import { InputNumber } from 'antd'

type Props = Omit<ComponentProps<typeof InputNumber>, 'value' | 'onChange'> & {
  value: number
  onChange: (value: number) => void
}

/**
 * Monto a cobrar: si está en 0, al tocar/enfocar se vacía el campo
 * para tipar sin borrar el cero a mano.
 */
export function MontoCobrarInput({ value, onChange, onFocus, onBlur, ...rest }: Props) {
  const [clearZero, setClearZero] = useState(false)
  const showEmpty = clearZero && (value ?? 0) === 0

  return (
    <InputNumber
      {...rest}
      value={showEmpty ? null : value}
      onFocus={(e) => {
        if ((value ?? 0) === 0) setClearZero(true)
        onFocus?.(e)
      }}
      onBlur={(e) => {
        setClearZero(false)
        onBlur?.(e)
      }}
      onChange={(v) => {
        if (v == null) {
          setClearZero(true)
          onChange(0)
          return
        }
        setClearZero(false)
        onChange(Number(v) || 0)
      }}
    />
  )
}
