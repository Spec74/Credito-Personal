import { useEffect, useRef, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Space, Typography } from 'antd'
import { CameraOutlined, CheckCircleOutlined, UploadOutlined } from '@ant-design/icons'
import { ApiError } from '../../api/errors'
import {
  validarVoucherCaptura,
  type VoucherValidacionResult,
} from '../../api/voucherValidacion'
import { useAuth } from '../../auth/useAuth'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/medios-pago.css'

const MAX_BYTES = 8 * 1024 * 1024
const MAX_EDGE_PX = 1600
const JPEG_QUALITY = 0.82

export type VoucherValidadoPayload = {
  fechaOperacionLocal: string
  monto: number | null
  codigoOperacion: string | null
  canalDetectado: string | null
  coincideMonto: boolean
  advertencias: string[]
}

type Props = {
  montoEsperado?: number | null
  /** Marca/medio del cobro (yape, plin, bcp…) para contrastar con la constancia. */
  canalEsperado?: string | null
  disabled?: boolean
  onValidated?: (data: VoucherValidadoPayload) => void
}

async function prepareImageForOcr(file: File): Promise<File> {
  if (!file.type.startsWith('image/') && file.type !== '') {
    throw new Error('Seleccione una imagen (JPG/PNG/WEBP).')
  }
  if (file.size > MAX_BYTES * 2) {
    throw new Error('La foto es demasiado pesada (máx. ~8 MB).')
  }

  // HEIC u otros: enviar tal cual si el navegador no puede dibujar.
  if (typeof createImageBitmap !== 'function') {
    if (file.size > MAX_BYTES) throw new Error('La foto supera 8 MB.')
    return file
  }

  try {
    const bitmap = await createImageBitmap(file)
    const scale = Math.min(1, MAX_EDGE_PX / Math.max(bitmap.width, bitmap.height))
    const w = Math.max(1, Math.round(bitmap.width * scale))
    const h = Math.max(1, Math.round(bitmap.height * scale))
    const canvas = document.createElement('canvas')
    canvas.width = w
    canvas.height = h
    const ctx = canvas.getContext('2d')
    if (!ctx) {
      bitmap.close()
      if (file.size > MAX_BYTES) throw new Error('La foto supera 8 MB.')
      return file
    }
    ctx.drawImage(bitmap, 0, 0, w, h)
    bitmap.close()

    const blob = await new Promise<Blob | null>((resolve) =>
      canvas.toBlob((b) => resolve(b), 'image/jpeg', JPEG_QUALITY),
    )
    if (!blob) {
      if (file.size > MAX_BYTES) throw new Error('La foto supera 8 MB.')
      return file
    }
    if (blob.size > MAX_BYTES) {
      throw new Error('No se pudo comprimir la foto bajo 8 MB. Acérquese o baje la resolución.')
    }
    return new File([blob], (file.name || 'voucher').replace(/\.\w+$/, '') + '.jpg', {
      type: 'image/jpeg',
    })
  } catch (e) {
    if (e instanceof Error && e.message.includes('MB')) throw e
    if (file.size > MAX_BYTES) throw new Error('La foto supera 8 MB.')
    return file
  }
}

/**
 * Bajo el QR: abrir cámara / elegir foto de la constancia del cliente → OCR servidor.
 */
export function VoucherValidarCaptura({
  montoEsperado,
  canalEsperado,
  disabled,
  onValidated,
}: Props) {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const cameraRef = useRef<HTMLInputElement>(null)
  const fileRef = useRef<HTMLInputElement>(null)
  const [last, setLast] = useState<VoucherValidacionResult | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)

  useEffect(() => {
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl)
    }
  }, [previewUrl])

  const mut = useMutation({
    mutationFn: async (file: File) => {
      const prepared = await prepareImageForOcr(file)
      return validarVoucherCaptura({
        oficinaId,
        montoEsperado,
        canalEsperado,
        captura: prepared,
        fileName: prepared.name || 'voucher.jpg',
      })
    },
    onSuccess: (res) => {
      setLast(res)
      setLocalError(null)
      if (res.ok && res.fechaOperacionLocal) {
        onValidated?.({
          fechaOperacionLocal: res.fechaOperacionLocal,
          monto: res.monto,
          codigoOperacion: res.codigoOperacion,
          canalDetectado: res.canalDetectado,
          coincideMonto: res.coincideMonto,
          advertencias: res.advertencias ?? [],
        })
      }
    },
  })

  const handleFile = (file: File | undefined) => {
    if (!file) return
    setLocalError(null)
    setLast(null)
    if (previewUrl) URL.revokeObjectURL(previewUrl)
    setPreviewUrl(URL.createObjectURL(file))
    mut.mutate(file, {
      onError: (e) => {
        setLocalError(
          e instanceof Error && !(e instanceof ApiError)
            ? e.message
            : e instanceof ApiError
              ? e.message
              : 'No se pudo validar la captura',
        )
      },
    })
  }

  const errMsg =
    localError ??
    (mut.error instanceof ApiError
      ? mut.error.message
      : mut.isError
        ? 'No se pudo validar la captura'
        : null)

  const applied =
    last?.ok === true && !!last.fechaOperacionLocal && !mut.isPending

  return (
    <div className="voucher-validar">
      <Space wrap size="small">
        <Button
          type="primary"
          icon={<CameraOutlined />}
          disabled={disabled || oficinaId < 1 || mut.isPending}
          loading={mut.isPending}
          onClick={() => cameraRef.current?.click()}
        >
          Validar (cámara)
        </Button>
        <Button
          icon={<UploadOutlined />}
          disabled={disabled || oficinaId < 1 || mut.isPending}
          onClick={() => fileRef.current?.click()}
        >
          Galería
        </Button>
      </Space>
      <p className="voucher-validar__hint">
        Foto nítida a la pantalla del cliente (Yape, Plin, BCP, Interbank o Nación). Se comprime
        en el dispositivo antes de enviar.
        {montoEsperado != null && montoEsperado > 0
          ? ` Cobro esperado: ${formatMoney(montoEsperado)}.`
          : ' Indique el monto del cobro para contrastar.'}
      </p>

      <input
        ref={cameraRef}
        type="file"
        accept="image/*"
        capture="environment"
        hidden
        onChange={(e) => {
          handleFile(e.target.files?.[0])
          e.target.value = ''
        }}
      />
      <input
        ref={fileRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        hidden
        onChange={(e) => {
          handleFile(e.target.files?.[0])
          e.target.value = ''
        }}
      />

      {previewUrl ? (
        <img className="voucher-validar__preview" src={previewUrl} alt="Captura del voucher" />
      ) : null}

      {errMsg ? <Alert type="error" showIcon title={errMsg} style={{ marginTop: 8 }} /> : null}

      {last ? (
        <Alert
          style={{ marginTop: 8 }}
          type={
            last.ok && last.coincideMonto
              ? 'success'
              : last.ok
                ? 'warning'
                : 'error'
          }
          showIcon
          icon={last.ok ? <CheckCircleOutlined /> : undefined}
          title={
            last.ok
              ? `${last.canalDetectado ?? 'Voucher'} · ${last.fechaOperacionLocal ?? '—'}${
                  last.monto != null ? ` · ${formatMoney(last.monto)}` : ''
                }`
              : 'No se pudo leer la constancia'
          }
          description={
            <div className="voucher-validar__result">
              {last.codigoOperacion ? (
                <Typography.Text copyable={{ text: last.codigoOperacion }}>
                  Op. {last.codigoOperacion}
                </Typography.Text>
              ) : null}
              {(last.advertencias ?? []).map((w) => (
                <div key={w}>{w}</div>
              ))}
              {applied ? (
                <div>
                  {last.coincideMonto
                    ? 'Hora del voucher aplicada al cobro.'
                    : 'Hora aplicada; revise el monto antes de confirmar.'}
                </div>
              ) : null}
              {!last.ocrDisponible ? (
                <div>Tipifique la fecha/hora manualmente en el campo del voucher.</div>
              ) : null}
            </div>
          }
        />
      ) : null}
    </div>
  )
}
