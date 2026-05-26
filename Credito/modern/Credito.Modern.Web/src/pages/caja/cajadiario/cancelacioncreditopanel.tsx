import { useMemo, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Button, message } from 'antd'
import { StopOutlined } from '@ant-design/icons'
import {
  fetchCuotasPendientes,
  pagarCuotasCancelacion,
} from '../../../api/cajaDiario'
import { CajaCuotasTable } from '../../../components/caja/CajaCuotasTable'
import { CajaSection } from '../../../components/caja/CajaSection'
import { cajaConfirm } from '../../../components/caja/CajaModal'
import { formatMoney } from '../../../utils/formatMoney'
import {
  assertSinCxcPendiente,
  maybeDownloadCajaTicket,
} from './cajaPagoHelpers'
import type { CajaSession } from './types'
import { errMsg } from './types'

/** Sub-grilla cancelación (paridad `btnCancelarCreditoShow` del MVC). */
export function CancelacionCreditoPanel({
  ctx,
  creditoId,
  onChanged,
}: {
  ctx: CajaSession
  creditoId: number | null
  onChanged: () => void
}) {
  const [expanded, setExpanded] = useState(false)

  const query = useQuery({
    queryKey: ['cuotas-cancelacion', creditoId],
    queryFn: () => fetchCuotasPendientes(creditoId!, true),
    enabled: expanded && creditoId != null && creditoId > 0,
  })

  const cancelacion = useMutation({
    mutationFn: () =>
      pagarCuotasCancelacion({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId: creditoId!,
      }),
    onSuccess: async (r) => {
      message.success('Cancelación de crédito registrada')
      await maybeDownloadCajaTicket(ctx.oficinaId, r.resultId)
      setExpanded(false)
      onChanged()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const rows = query.data ?? []

  const totales = useMemo(
    () => ({
      cuotas: rows.length,
      pagoCuota: rows.reduce((s, r) => s + (r.pagoCuota ?? 0), 0),
    }),
    [rows],
  )

  if (!creditoId) {
    return null
  }

  if (!expanded) {
    return (
      <Button
        danger
        block
        size="large"
        icon={<StopOutlined />}
        className="caja-diario-cancelacion-trigger"
        onClick={() => setExpanded(true)}
      >
        Cancelar crédito
      </Button>
    )
  }

  return (
    <CajaSection
      tone="credito"
      kicker="Cancelación"
      title="Cuotas por cancelación de crédito"
      icon={<StopOutlined />}
    >
      <CajaCuotasTable
        data={rows}
        loading={query.isLoading}
        showLegend
        pageSize={10}
        compact
      />
      <div className="caja-diario-cancelacion-footer">
        <span>
          {totales.cuotas} cuota(s) · Total:{' '}
          <strong>{formatMoney(totales.pagoCuota)}</strong>
        </span>
        <SpaceActions
          onHide={() => setExpanded(false)}
          onConfirm={async () => {
            if (!(await assertSinCxcPendiente(creditoId))) {
              return
            }
            cajaConfirm({
              title: '¿Cancelar crédito?',
              content: `Se registrará la cancelación por ${formatMoney(totales.pagoCuota)}.`,
              okText: 'Confirmar cancelación',
              okType: 'danger',
              onOk: () => cancelacion.mutateAsync(),
            })
          }}
          loading={cancelacion.isPending}
          disabled={rows.length === 0}
        />
      </div>
    </CajaSection>
  )
}

function SpaceActions({
  onHide,
  onConfirm,
  loading,
  disabled,
}: {
  onHide: () => void
  onConfirm: () => void
  loading: boolean
  disabled: boolean
}) {
  return (
    <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
      <Button onClick={onHide}>Ocultar</Button>
      <Button
        type="primary"
        danger
        loading={loading}
        disabled={disabled}
        onClick={onConfirm}
      >
        Confirmar cancelación
      </Button>
    </div>
  )
}
