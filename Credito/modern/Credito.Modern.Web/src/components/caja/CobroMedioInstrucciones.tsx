import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, Grid, Typography } from 'antd'
import { fetchOficinaMediosPago, type OficinaMedioPago } from '../../api/mediosPago'
import { useAuth } from '../../auth/useAuth'
import { QrEmvDisplay } from './QrEmvDisplay'
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
  montoLabel,
}: {
  tipoPagoId: number
  montoLabel?: string
}) {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true

  const mediosQuery = useQuery({
    queryKey: ['oficina-medios-pago', oficinaId],
    queryFn: () => fetchOficinaMediosPago(oficinaId, { soloRaices: true }),
    enabled: oficinaId > 0 && tipoPagoId > 1,
    staleTime: 60_000,
  })

  const medio = useMemo(() => {
    const all = flattenMedios(mediosQuery.data ?? [])
    return all.find((m) => m.activo && m.tipoPagoId === tipoPagoId) ?? null
  }, [mediosQuery.data, tipoPagoId])

  if (tipoPagoId <= 1 || mediosQuery.isLoading) {
    return null
  }

  if (!medio || !medio.esDigital) {
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
          {montoLabel ? ` · ${montoLabel}` : ''}
        </h3>
        <p className="cobro-medio__hint">Muestre al cliente · verifique en su app</p>
      </header>

      <div className="cobro-medio__body">
        {showQr ? (
          <QrEmvDisplay
            payload={medio.qrEmvPayload}
            size={isMobile ? 200 : 220}
            compact={isMobile}
            label={medio.qrNota ?? 'Cliente: escanear con Yape, Plin o banca móvil'}
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
