import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, Grid, Typography } from 'antd'
import { fetchOficinaMediosPago, type OficinaMedioPago } from '../../api/mediosPago'
import { useAuth } from '../../auth/useAuth'
import { formatMoney } from '../../utils/formatMoney'
import { QrEmvDisplay } from './QrEmvDisplay'
import {
  VoucherValidarCaptura,
  type VoucherValidadoPayload,
} from './VoucherValidarCaptura'
import '../../styles/medios-pago.css'

function flattenMedios(items: OficinaMedioPago[]): OficinaMedioPago[] {
  const out: OficinaMedioPago[] = []
  for (const m of items) {
    out.push(m)
    if (m.variantes?.length) {
      out.push(...m.variantes)
    }
  }
  return out
}

/** Instrucciones de cobro digital según configuración de la oficina (PC + móvil). */
export function CobroMedioInstrucciones({
  tipoPagoId,
  monto,
  montoLabel,
  onVoucherValidated,
}: {
  tipoPagoId: number
  /** Monto PEN a incrustar en el QR dinámico (tag 54). */
  monto?: number | null
  /** Etiqueta opcional; si no hay, se arma desde `monto`. */
  montoLabel?: string
  /** Tras OCR exitoso: rellenar fecha/hora del voucher en el cobro. */
  onVoucherValidated?: (data: VoucherValidadoPayload) => void
}) {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true

  const mediosQuery = useQuery({
    queryKey: ['oficina-medios-pago', oficinaId, { soloRaices: true }],
    queryFn: () => fetchOficinaMediosPago(oficinaId, { soloRaices: true }),
    enabled: oficinaId > 0 && tipoPagoId > 1,
    staleTime: 30_000,
  })

  const medio = useMemo(() => {
    const all = flattenMedios(mediosQuery.data ?? [])
    const id = Number(tipoPagoId)
    return (
      all.find((m) => m.activo && Number(m.tipoPagoId) === id) ??
      all.find((m) => Number(m.tipoPagoId) === id) ??
      null
    )
  }, [mediosQuery.data, tipoPagoId])

  const montoNum =
    monto != null && Number.isFinite(monto) && monto > 0 ? Number(monto) : null
  const labelMonto =
    montoLabel?.trim() ||
    (montoNum != null ? formatMoney(montoNum) : undefined)

  if (tipoPagoId <= 1 || mediosQuery.isLoading) {
    return null
  }

  if (!medio) {
    return (
      <Alert
        className="cobro-medio__empty"
        type="warning"
        showIcon
        title="Medio digital sin ficha en esta oficina"
        description="Configure Yape/Interbank (celular, CCI o QR) en Mantenimiento → Medios de pago."
      />
    )
  }

  if (!medio.esDigital) {
    return null
  }

  const showQr = medio.usaQr
  const showPhone = medio.usaCelular && !!medio.telefono
  const showAccount = medio.usaCuenta && (!!medio.numeroCuenta || !!medio.cci)

  if (!showQr && !showPhone && !showAccount) {
    return (
      <Alert
        className="cobro-medio__empty"
        type="info"
        showIcon
        title={`${medio.nombre}: sin datos de cobro`}
        description="Configure celular, cuenta/CCI o el QR oficial en Mantenimiento → Medios de pago."
      />
    )
  }

  return (
    <section className="cobro-medio" aria-label={`Cobrar con ${medio.nombre}`}>
      <header className="cobro-medio__head">
        <h3 className="cobro-medio__title">
          Cobrar con {medio.nombre}
          {labelMonto ? ` · ${labelMonto}` : ''}
        </h3>
        <p className="cobro-medio__hint">
          {showQr && montoNum != null
            ? 'Muestre el QR · el cliente debe ver el monto al escanear'
            : 'Muestre al cliente · verifique en su app'}
        </p>
      </header>

      <div className="cobro-medio__body">
        {showQr ? (
          montoNum == null ? (
            <Alert
              type="warning"
              showIcon
              title="Indique el monto del cobro"
              description="El QR dinámico necesita el importe para que al escanear salga precargado."
              style={{ marginBottom: 8 }}
            />
          ) : null
        ) : null}
        {showQr ? (
          <QrEmvDisplay
            payload={medio.qrEmvPayload}
            amount={montoNum}
            size={isMobile ? 200 : 220}
            compact={isMobile}
            label={medio.qrNota ?? 'Cliente: escanear con Yape, Plin o banca móvil'}
          />
        ) : null}

        {medio.esDigital ? (
          <VoucherValidarCaptura
            montoEsperado={montoNum}
            canalEsperado={medio.marcaCodigo || medio.nombre}
            onValidated={onVoucherValidated}
          />
        ) : null}

        {(showPhone || showAccount) && (
          <div className="cobro-medio__fields">
            {showPhone ? (
              <div className="cobro-medio__field">
                <span className="cobro-medio__field-label">Celular</span>
                <Typography.Text
                  className="cobro-medio__field-value"
                  copyable={{ text: medio.telefono ?? undefined }}
                >
                  {medio.telefono}
                </Typography.Text>
              </div>
            ) : null}
            {showAccount && medio.numeroCuenta ? (
              <div className="cobro-medio__field">
                <span className="cobro-medio__field-label">N° cuenta</span>
                <Typography.Text
                  className="cobro-medio__field-value"
                  copyable={{ text: medio.numeroCuenta }}
                >
                  {medio.numeroCuenta}
                </Typography.Text>
              </div>
            ) : null}
            {showAccount && medio.cci ? (
              <div className="cobro-medio__field">
                <span className="cobro-medio__field-label">CCI</span>
                <Typography.Text
                  className="cobro-medio__field-value"
                  copyable={{ text: medio.cci }}
                >
                  {medio.cci}
                </Typography.Text>
              </div>
            ) : null}
          </div>
        )}
      </div>
    </section>
  )
}
