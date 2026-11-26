import { useEffect, useState } from 'react'
import QRCode from 'qrcode'
import { Alert, Spin, Typography } from 'antd'

/**
 * Dibuja un QR a partir de payload EMVCo MPM (estándar BCRP / Circular 0003-2020).
 * El string debe provenir del QR oficial (Yape/Plin/banco), no inventarse.
 */
export function QrEmvDisplay({
  payload,
  size = 220,
  label,
}: {
  payload: string | null | undefined
  size?: number
  label?: string
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

    // Algunos proveedores prefijan "generate:" — se elimina para dibujar.
    const emv = raw.replace(/^generate:/i, '')
    void QRCode.toDataURL(emv, {
      errorCorrectionLevel: 'M',
      margin: 2,
      width: size,
      color: { dark: '#0a1f33', light: '#ffffff' },
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
    <div style={{ textAlign: 'center' }}>
      {label ? (
        <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
          {label}
        </Typography.Text>
      ) : null}
      <img src={dataUrl} alt="QR de cobro" width={size} height={size} />
      <Typography.Paragraph type="secondary" style={{ marginTop: 8, marginBottom: 0, fontSize: 12 }}>
        Escaneable desde Yape, Plin y apps de banca interoperables (BCRP).
      </Typography.Paragraph>
    </div>
  )
}
