import { Button, InputNumber, Space, Typography } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { TipoPagoChips } from './TipoPagoChips'
import {
  FechaHoraDigitalField,
  nowDatetimeLocal,
} from '../../pages/caja/components/FechaHoraDigitalField'
import { formatMoney } from '../../utils/formatMoney'
import { moneyEquals, roundMoney, sumMoney } from '../../utils/money'
import { esTipoPagoDigital, esTipoPagoSedeObsoleto } from '../../utils/tipoPago'

export type PagoMedioLineaUi = {
  key: string
  tipoPagoId: number
  importe: number | null
  fechaTransferencia: string
}

type Opt = { value: number; label: string }

function newKey(): string {
  return `m-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`
}

/** Líneas por defecto: primer digital/transferencia del catálogo + efectivo. */
export function defaultPagoMediosSplit(options: Opt[], totalHint?: number): PagoMedioLineaUi[] {
  const efectivo = options.find((o) => o.value === 1)?.value ?? 1
  const digital = options.find((o) => esTipoPagoDigital(o.value))?.value ?? 2
  const half =
    totalHint != null && totalHint > 0 ? roundMoney(totalHint / 2) : null
  const rest =
    totalHint != null && totalHint > 0 && half != null
      ? roundMoney(totalHint - half)
      : null
  return [
    {
      key: newKey(),
      tipoPagoId: digital,
      importe: half,
      fechaTransferencia: nowDatetimeLocal(),
    },
    {
      key: newKey(),
      tipoPagoId: efectivo,
      importe: rest,
      fechaTransferencia: '',
    },
  ]
}

export function sumPagoMedios(lineas: PagoMedioLineaUi[]): number {
  return sumMoney(lineas.map((l) => l.importe ?? 0))
}

export function validatePagoMedios(
  lineas: PagoMedioLineaUi[],
  expectedTotal?: number | null,
): string | null {
  if (lineas.length < 2) return 'Indique al menos dos medios.'
  for (const l of lineas) {
    const imp = roundMoney(l.importe ?? 0)
    if (!(imp > 0)) return 'Cada medio debe tener importe mayor a 0 (mín. S/ 0.01).'
    if (!(l.tipoPagoId >= 1) || esTipoPagoSedeObsoleto(l.tipoPagoId)) {
      return 'Seleccione un medio válido (catálogo de la sede).'
    }
    if (esTipoPagoDigital(l.tipoPagoId) && !l.fechaTransferencia.trim()) {
      return 'Yape Gerente/René, Plin, BCP, Interbank, Nación u otro digital: indique fecha/hora del voucher.'
    }
  }
  if (expectedTotal != null && expectedTotal > 0) {
    const suma = sumPagoMedios(lineas)
    if (!moneyEquals(suma, roundMoney(expectedTotal))) {
      return `La suma de medios (${formatMoney(suma)}) debe coincidir al céntimo con ${formatMoney(expectedTotal)}.`
    }
  }
  return null
}

/** Editor de cobro mixto (Yape/Plin/BCP/Interbank/Nación + efectivo, etc.). */
export function PagoMediosSplit({
  lineas,
  options,
  onChange,
  disabled,
}: {
  lineas: PagoMedioLineaUi[]
  options: Opt[]
  onChange: (next: PagoMedioLineaUi[]) => void
  disabled?: boolean
}) {
  const total = sumPagoMedios(lineas)
  const patch = (key: string, partial: Partial<PagoMedioLineaUi>) => {
    onChange(
      lineas.map((l) => {
        if (l.key !== key) return l
        const next = { ...l, ...partial }
        if (
          partial.tipoPagoId != null &&
          esTipoPagoDigital(partial.tipoPagoId) &&
          !next.fechaTransferencia
        ) {
          next.fechaTransferencia = nowDatetimeLocal()
        }
        return next
      }),
    )
  }

  return (
    <div className="caja-pago-medios-split">
      <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
        Divida el cobro (Yape Gerente/René, Plin, BCP, Interbank, Nación, efectivo…). Total:{' '}
        <strong>{formatMoney(total)}</strong>
      </Typography.Text>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        {lineas.map((l, idx) => (
          <div key={l.key} className="caja-pago-medios-split__row">
            <div className="caja-pago-medios-split__head">
              <Typography.Text strong>Medio {idx + 1}</Typography.Text>
              {lineas.length > 2 ? (
                <Button
                  type="text"
                  size="small"
                  danger
                  icon={<DeleteOutlined />}
                  disabled={disabled}
                  onClick={() => onChange(lineas.filter((x) => x.key !== l.key))}
                />
              ) : null}
            </div>
            <TipoPagoChips
              value={l.tipoPagoId}
              options={options}
              disabled={disabled}
              wrap
              onChange={(id) => patch(l.key, { tipoPagoId: id })}
            />
            <label className="caja-diario-pago-recibido" style={{ marginTop: 8 }}>
              <span>Importe</span>
              <InputNumber
                min={0}
                step={0.01}
                controls={false}
                inputMode="decimal"
                prefix="S/"
                disabled={disabled}
                value={l.importe}
                onChange={(v) => patch(l.key, { importe: v })}
                style={{ width: '100%' }}
              />
            </label>
            {esTipoPagoDigital(l.tipoPagoId) ? (
              <FechaHoraDigitalField
                value={l.fechaTransferencia}
                status={!l.fechaTransferencia ? 'error' : undefined}
                onChange={(v) => patch(l.key, { fechaTransferencia: v })}
                compact
              />
            ) : null}
          </div>
        ))}
        {lineas.length < 5 ? (
          <Button
            type="dashed"
            block
            icon={<PlusOutlined />}
            disabled={disabled}
            onClick={() =>
              onChange([
                ...lineas,
                {
                  key: newKey(),
                  tipoPagoId: 1,
                  importe: null,
                  fechaTransferencia: '',
                },
              ])
            }
          >
            Agregar medio
          </Button>
        ) : null}
      </Space>
    </div>
  )
}
