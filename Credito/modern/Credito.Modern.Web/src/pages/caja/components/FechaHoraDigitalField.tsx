import dayjs, { type Dayjs } from 'dayjs'
import { Button, DatePicker, Space } from 'antd'
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
 * Formato PE, botón Ahora y panel Ant Design (sin input nativo legacy).
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
        size={compact ? 'middle' : 'large'}
        placeholder="Fecha y hora del pago"
        prefix={<FieldTimeOutlined />}
        className="cobro-bloque-fecha-digital__picker"
        getPopupContainer={() => document.body}
        onChange={(d) => onChange(toLocal(d))}
      />
      <Space size={4} className="cobro-bloque-fecha-digital__actions">
        <Button
          type="default"
          size="small"
          icon={<FieldTimeOutlined />}
          onClick={() => onChange(nowDatetimeLocal())}
        >
          Ahora
        </Button>
      </Space>
    </div>
  )
}
