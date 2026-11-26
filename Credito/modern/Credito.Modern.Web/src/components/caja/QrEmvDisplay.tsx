import { useEffect, useState } from 'react'
import QRCode from 'qrcode'
import { Alert, Spin } from 'antd'
import '../../styles/medios-pago.css'

/**
 * Dibuja un QR EMVCo MPM (BCRP / Estándar QR Perú).
 * El payload debe ser el oficial del proveedor; no se inventa.
 */
export function QrEmvDisplay({
  payload,
  size = 220,
  label,
  compact = false,
}: {
  payload: string | null | undefined
  size?: number
  label?: string
  /** En paneles estrechos (cobro móvil) reduce tipografía auxiliar. */
  compact?: boolean
}) {
  const [dataUrl, setDataUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    const raw = (payload ?? '').trim()
    if (!raw) {
      setDataUrl(null)
      setError(null)
      return
    }

    // Límite defensivo: payloads EMV reales no suelen superar ~1–2 KB.
    if (raw.length > 4096) {
      setDataUrl(null)
      setError('Payload EMV demasiado largo. Verifique que pegó solo el código oficial.')
      return
    }

    const emv = raw.replace(/^generate:/i, '')
    const pixelSize = Math.min(Math.max(size, 160), 320)
    // Módulos oscuros + quiet zone blanca: contraste de lectura (ISO/IEC 18004 / EMVCo).
    // Independiente del tema UI para que escáneres y banca móvil lean el código.
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
  }, [payload, size])

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
    return <Alert type="error" showIcon title={error} />
  }

  if (!dataUrl) {
    return <Spin tip="Generando QR…" />
  }

  return (
    <div className={compact ? 'qr-emv qr-emv--compact' : 'qr-emv'}>
      {label ? <p className="qr-emv__label">{label}</p> : null}
      <div className="qr-emv__frame">
        <img
          className="qr-emv__img"
          src={dataUrl}
          alt="Código QR de cobro"
          width={size}
          height={size}
          decoding="async"
        />
      </div>
      <p className="qr-emv__foot">
        Escaneable con Yape, Plin y banca móvil interoperable (BCRP).
      </p>
    </div>
  )
}
