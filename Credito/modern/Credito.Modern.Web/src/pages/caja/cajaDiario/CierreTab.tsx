import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Space, message } from 'antd'
import {
  cerrarCajaDiario,
  downloadRptSaldosCajaPdf,
  validarCierreCajaDiario,
} from '../../../api/cajaDiario'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import type { CajaSession } from './types'
import { errMsg } from './types'

export function CierreTab({
  ctx,
  onCerrada,
}: {
  ctx: CajaSession
  onCerrada: () => void
}) {
  const validar = useMutation({
    mutationFn: () =>
      validarCierreCajaDiario(ctx.oficinaId, ctx.cajaDiarioId),
    onError: (e) => message.error(errMsg(e)),
  })

  const cerrar = useMutation({
    mutationFn: () =>
      cerrarCajaDiario({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
      }),
    onSuccess: async () => {
      message.success('Caja diario cerrada')
      try {
        await downloadRptSaldosCajaPdf(ctx.cajaDiarioId)
        message.info('Previsualización de saldo descargada (paridad btnPrevSaldoCaja)')
      } catch {
        message.warning('Caja cerrada; no se pudo descargar el PDF de saldo.')
      }
      onCerrada()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const pedirCierre = async () => {
    try {
      const resultado = await validar.mutateAsync()
      if (!resultado.puedeCerrar) {
        message.warning(
          resultado.blockers[0] ??
            'No puede cerrar la caja todavía. Revise los bloqueos.',
        )
        return
      }
      cajaConfirm({
        title: '¿Cerrar caja diario?',
        content:
          'Esta acción cierra la operación del día para esta caja. Se descargará el PDF de saldo.',
        okText: 'Cerrar',
        okType: 'danger',
        onOk: () => cerrar.mutateAsync(),
      })
    } catch {
      // error ya notificado en validar.onError
    }
  }

  return (
    <>
      <Space wrap style={{ marginBottom: 16 }}>
        <Button loading={validar.isPending} onClick={() => validar.mutate()}>
          Validar cierre
        </Button>
        <Button
          type="primary"
          danger
          loading={cerrar.isPending || validar.isPending}
          onClick={() => void pedirCierre()}
        >
          Cerrar caja
        </Button>
      </Space>
      {validar.data && (
        <Alert
          type={validar.data.puedeCerrar ? 'success' : 'warning'}
          showIcon
          message={
            validar.data.puedeCerrar
              ? 'Puede cerrar la caja'
              : 'No puede cerrar todavía'
          }
          description={
            validar.data.blockers.length > 0 ? (
              <ul style={{ marginBottom: 0, paddingLeft: 20 }}>
                {validar.data.blockers.map((b) => (
                  <li key={b}>{b}</li>
                ))}
              </ul>
            ) : null
          }
        />
      )}
    </>
  )
}
