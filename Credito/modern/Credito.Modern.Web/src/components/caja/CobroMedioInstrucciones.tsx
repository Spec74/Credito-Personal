import { useMemo, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, Card, Descriptions, Typography } from 'antd'
import { fetchOficinaMediosPago, type OficinaMedioPago } from '../../api/mediosPago'
import { useAuth } from '../../auth/useAuth'
import { QrEmvDisplay } from './QrEmvDisplay'

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

/** Instrucciones de cobro digital según configuración de la oficina. */
export function CobroMedioInstrucciones({
  tipoPagoId,
  montoLabel,
}: {
  tipoPagoId: number
  montoLabel?: string
}) {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0

  const mediosQuery = useQuery({
    queryKey: ['oficina-medios-pago', oficinaId],
    queryFn: () => fetchOficinaMediosPago(oficinaId, { soloRaices: true }),
    enabled: oficinaId > 0 && tipoPagoId > 1,
  })

  const medio = useMemo(() => {
    const all = flattenMedios(mediosQuery.data ?? [])
    return all.find((m) => m.activo && m.tipoPagoId === tipoPagoId) ?? null
  }, [mediosQuery.data, tipoPagoId])

  if (tipoPagoId <= 1) {
    return null
  }

  if (mediosQuery.isLoading) {
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
        type="info"
        showIcon
        style={{ marginTop: 8 }}
        title={`${medio.nombre}: sin datos de cobro`}
        description="Configure celular, cuenta/CCI o el payload EMV oficial del QR en Maestros → Medios de pago."
      />
    )
  }

  return (
    <Card
      size="small"
      style={{ marginTop: 8 }}
      title={`Cobrar con ${medio.nombre}${montoLabel ? ` · ${montoLabel}` : ''}`}
    >
      {showQr ? (
        <QrEmvDisplay
          payload={medio.qrEmvPayload}
          label={medio.qrNota ?? 'Cliente: escanear con Yape, Plin o banca móvil'}
        />
      ) : null}

      <Descriptions
        size="small"
        column={1}
        style={{ marginTop: showQr ? 12 : 0 }}
        items={[
          showPhone
            ? {
                key: 'tel',
                label: 'Celular',
                children: (
                  <Typography.Text copyable>{medio.telefono}</Typography.Text>
                ),
              }
            : null,
          showAccount && medio.numeroCuenta
            ? {
                key: 'cta',
                label: 'N° cuenta',
                children: (
                  <Typography.Text copyable>{medio.numeroCuenta}</Typography.Text>
                ),
              }
            : null,
          showAccount && medio.cci
            ? {
                key: 'cci',
                label: 'CCI',
                children: (
                  <Typography.Text copyable>{medio.cci}</Typography.Text>
                ),
              }
            : null,
        ].filter(Boolean) as {
          key: string
          label: string
          children: ReactNode
        }[]}
      />
    </Card>
  )
}
