import { useState } from 'react'
import dayjs, { type Dayjs } from 'dayjs'
import { Button, Calendar, DatePicker, Drawer, Grid, TimePicker, Tooltip } from 'antd'
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

function mergeDateTime(base: Dayjs, datePart: Dayjs, timePart: Dayjs | null): Dayjs {
  const t = timePart ?? base
  return datePart.hour(t.hour()).minute(t.minute()).second(0).millisecond(0)
}

/**
 * Fecha/hora del voucher digital (Yape, Plin, transferencia…).
 * Desktop: DatePicker con hora. Móvil: hoja inferior táctil (calendario + hora apilados).
 */
export function FechaHoraDigitalField({ value, onChange, status, compact = false }: Props) {
  const screens = Grid.useBreakpoint()
  const isMobile = !screens.md
  const current = parseLocal(value)
  const [sheetOpen, setSheetOpen] = useState(false)
  const [draft, setDraft] = useState<Dayjs>(() => current ?? dayjs())

  const openSheet = () => {
    setDraft(current ?? dayjs())
    setSheetOpen(true)
  }

  const confirmSheet = () => {
    onChange(toLocal(draft))
    setSheetOpen(false)
  }

  const ahoraBtn = compact ? (
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
  )

  if (isMobile) {
    const display = current?.format('DD/MM/YYYY HH:mm') ?? ''
    return (
      <div
        className={[
          'cobro-bloque-fecha-digital',
          'cobro-bloque-fecha-digital--mobile',
          compact ? 'cobro-bloque-fecha-digital--compact' : '',
        ]
          .filter(Boolean)
          .join(' ')}
      >
        <button
          type="button"
          className={[
            'cobro-bloque-fecha-digital__trigger',
            status === 'error' ? 'cobro-bloque-fecha-digital__trigger--error' : '',
            compact ? 'cobro-bloque-fecha-digital__trigger--compact' : '',
          ]
            .filter(Boolean)
            .join(' ')}
          onClick={openSheet}
          aria-label="Elegir fecha y hora del voucher"
        >
          <span className="cobro-bloque-fecha-digital__trigger-value">
            {display || 'Hora del pago'}
          </span>
          <FieldTimeOutlined />
        </button>
        {ahoraBtn}

        <Drawer
          open={sheetOpen}
          onClose={() => setSheetOpen(false)}
          placement="bottom"
          height="auto"
          title="Fecha y hora del voucher"
          className="cobro-bloque-fecha-digital__sheet"
          destroyOnClose
          styles={{
            body: { paddingTop: 8, paddingBottom: 12 },
            footer: { padding: '10px 16px calc(10px + env(safe-area-inset-bottom, 0px))' },
          }}
          footer={
            <div className="cobro-bloque-fecha-digital__sheet-footer">
              <Button
                size="large"
                icon={<FieldTimeOutlined />}
                onClick={() => setDraft(dayjs())}
              >
                Ahora
              </Button>
              <Button type="primary" size="large" onClick={confirmSheet}>
                Confirmar
              </Button>
            </div>
          }
        >
          <div className="cobro-bloque-fecha-digital__sheet-body">
            <p className="cobro-bloque-fecha-digital__sheet-preview">
              {draft.format('DD/MM/YYYY HH:mm')}
            </p>
            <Calendar
              fullscreen={false}
              value={draft}
              onSelect={(d) => setDraft((prev) => mergeDateTime(prev, d, prev))}
              onChange={(d) => setDraft((prev) => mergeDateTime(prev, d, prev))}
            />
            <div className="cobro-bloque-fecha-digital__sheet-time">
              <span className="cobro-bloque-fecha-digital__sheet-label">Hora</span>
              <TimePicker
                format="HH:mm"
                value={draft}
                allowClear={false}
                needConfirm={false}
                inputReadOnly
                showNow={false}
                size="large"
                className="cobro-bloque-fecha-digital__time"
                onChange={(t) => {
                  if (!t) return
                  setDraft((prev) => mergeDateTime(prev, prev, t))
                }}
              />
            </div>
          </div>
        </Drawer>
      </div>
    )
  }

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
        popupClassName="cobro-bloque-fecha-digital__dropdown"
        getPopupContainer={() => document.body}
        onChange={(d) => onChange(toLocal(d))}
      />
      {ahoraBtn}
    </div>
  )
}
