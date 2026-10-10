import dayjs, { type Dayjs } from 'dayjs'
import { Button, DatePicker, Tooltip } from 'antd'
import { FieldTimeOutlined } from '@ant-design/icons'
import '../../../styles/cobro-bloque-fecha-digital.css'

type Props = {
  value: string
  onChange: (next: string) => void
  status?: 'error'
  /** Compacto para celda de tabla. */
  compact?: boolean
}

function parseLocal(value: string | null | undefined): Dayjs | null {
  if (!value?.trim()) return null
  const d = dayjs(value)
  return d.isValid() ? d : null
}

function toLocal(value: Dayjs | null): string {
  return value?.isValid() ? value.format('YYYY-MM-DDTHH:mm') : ''
}

export function nowDatetimeLocal(): string {
  return toLocal(dayjs())
}

/**
 * Fecha/hora del voucher digital (Yape, Plin, transferencia…).
 * Formato PE, atajo Ahora y panel Ant Design (sin input nativo legacy).
 */
export function FechaHoraDigitalField({ value, onChange, status, compact = false }: Props) {
  const current = parseLocal(value)

  return (
    <div
      className={[
        'cobro-bloque-fecha-digital',
        compact ? 'cobro-bloque-fecha-digital--compact' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <DatePicker
        showTime={{ format: 'HH:mm', showSecond: false }}
        format="DD/MM/YYYY HH:mm"
        value={current}
        status={status}
        inputReadOnly
        allowClear={false}
        needConfirm={false}
        showNow
        size={compact ? 'small' : 'large'}
        placeholder="Hora del pago"
        className="cobro-bloque-fecha-digital__picker"
        getPopupContainer={() => document.body}
        onChange={(d) => onChange(toLocal(d))}
      />
      {compact ? (
        <Tooltip title="Usar hora actual">
          <Button
            type="default"
            size="small"
            icon={<FieldTimeOutlined />}
            aria-label="Ahora"
            onClick={() => onChange(nowDatetimeLocal())}
          />
        </Tooltip>
      ) : (
        <Button
          type="default"
          size="middle"
          icon={<FieldTimeOutlined />}
          onClick={() => onChange(nowDatetimeLocal())}
        >
          Ahora
        </Button>
      )}
    </div>
  )
}
