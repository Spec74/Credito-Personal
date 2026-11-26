import { useEffect, useMemo, useState } from 'react'
import QRCode from 'qrcode'
import { Alert, Spin } from 'antd'
import { withEmvTransactionAmount } from '../../utils/emvMpm'
import '../../styles/medios-pago.css'

/**
 * Dibuja un QR EMVCo MPM (BCRP / Estándar QR Perú).
 * Si `amount` > 0, genera payload dinámico (tag 54 + CRC) sobre el oficial.
 * El nombre del titular (tag 59) se conserva del QR pegado en Medios de pago.
 */
export function QrEmvDisplay({
  payload,
  amount,
  size = 220,
  label,
  compact = false,
}: {
  payload: string | null | undefined
  /** Monto PEN a incrustar en el QR (cobro diario). */
  amount?: number | null
  size?: number
  label?: string
  compact?: boolean
}) {
  const [dataUrl, setDataUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const resolved = useMemo(() => {
    const raw = (payload ?? '').trim()
    if (!raw) return null
    const monto = amount != null && Number.isFinite(amount) ? Number(amount) : 0
    if (monto > 0) {
      return withEmvTransactionAmount(raw, monto)
    }
    return {
      ok: true as const,
      payload: raw.replace(/^generate:/i, ''),
      amountText: null as string | null,
      merchantName: null as string | null,
    }
  }, [payload, amount])

  useEffect(() => {
    let cancelled = false
    if (!resolved) {
      setDataUrl(null)
      setError(null)
      return
    }
    if (!resolved.ok) {
      setDataUrl(null)
      setError(resolved.error)
      return
    }

    const emv = resolved.payload
    if (emv.length > 4096) {
      setDataUrl(null)
      setError('Payload EMV demasiado largo. Verifique que pegó solo el código oficial.')
      return
    }

    const pixelSize = Math.min(Math.max(size, 160), 320)
    void QRCode.toDataURL(emv, {
      errorCorrectionLevel: 'M',
      margin: 2,
      width: pixelSize,
      color: { dark: '#000000', light: '#ffffff' },
    })
      .then((url) => {
        if (!cancelled) {
          setDataUrl(url)
          setError(null)
        }
      })
      .catch(() => {
        if (!cancelled) {
          setDataUrl(null)
          setError('No se pudo generar la imagen QR. Verifique el payload EMV.')
        }
      })

    return () => {
      cancelled = true
    }
  }, [resolved, size])

  if (!payload?.trim()) {
    return (
      <Alert
        type="warning"
        showIcon
        title="Sin QR configurado"
        description="Pegue el payload EMV del QR oficial (Mi QR en Yape/Plin o banca) en Medios de pago."
      />
    )
  }

  if (error) {
    return (
      <Alert
        type="error"
        showIcon
        title={error}
        description={
          amount != null && amount > 0
            ? 'Revise el payload en Medios de pago o el monto del cobro.'
            : undefined
        }
      />
    )
  }

  if (!dataUrl) {
    return <Spin tip="Generando QR…" />
  }

  const amountHint =
    resolved && resolved.ok && resolved.amountText
      ? `Monto en QR: S/ ${resolved.amountText}`
      : null
  const titularHint =
    resolved && resolved.ok && resolved.merchantName
      ? `Titular: ${resolved.merchantName}`
      : null

  return (
    <div className={compact ? 'qr-emv qr-emv--compact' : 'qr-emv'}>
      {label ? <p className="qr-emv__label">{label}</p> : null}
      {amountHint ? <p className="qr-emv__amount">{amountHint}</p> : null}
      {titularHint ? <p className="qr-emv__titular">{titularHint}</p> : null}
      <div className="qr-emv__frame">
        <img
          className="qr-emv__img"
          src={dataUrl}
          alt={
            amountHint
              ? `Código QR de cobro ${amountHint}`
              : 'Código QR de cobro'
          }
          width={size}
          height={size}
          decoding="async"
        />
      </div>
      <p className="qr-emv__foot">
        {amountHint
          ? 'QR dinámico: al escanear debe salir el monto. Titular = nombre del QR oficial.'
          : 'Escaneable con Yape, Plin y banca móvil interoperable (BCRP).'}
      </p>
    </div>
  )
}
