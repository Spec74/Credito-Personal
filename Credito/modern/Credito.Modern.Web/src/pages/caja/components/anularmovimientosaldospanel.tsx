import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Descriptions, Input, InputNumber, Space, message } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import {
  anularMovimientoCaja,
  fetchMovimientoCajaAnular,
  validarAnularMovimientoCaja,
  type MovimientoCajaAnularPreview,
} from '../../../api/cajaDiario'
import { ApiError } from '../../../api/errors'
import { CredixPanel } from '../../../components/credix'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import { formatFechaHora } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'

type Props = {
  oficinaId: number
  onAnulado?: () => void
}

export function AnularMovimientoSaldosPanel({ oficinaId, onAnulado }: Props) {
  const [nro, setNro] = useState<number | null>(null)
  const [preview, setPreview] = useState<MovimientoCajaAnularPreview | null>(null)
  const [observacion, setObservacion] = useState('')

  const buscar = useMutation({
    mutationFn: () => fetchMovimientoCajaAnular(oficinaId, nro!),
    onSuccess: (data) => {
      setPreview(data)
      setObservacion('')
    },
    onError: (e) => {
      setPreview(null)
      message.error(e instanceof ApiError ? e.message : 'Movimiento no encontrado')
    },
  })

  const anular = useMutation({
    mutationFn: async () => {
      if (!preview) {
        throw new Error('Consulte primero el movimiento.')
      }
      if (!observacion.trim()) {
        throw new Error('Ingrese la observación para anular.')
      }
      const validacion = await validarAnularMovimientoCaja(
        oficinaId,
        preview.movimientoCajaId,
      )
      if (validacion.bloqueadoPorPagosCuota) {
        throw new Error(
          'Tiene Pagos de cuotas, No se Puede Anular el Crédito',
        )
      }
      return anularMovimientoCaja({
        oficinaId,
        movimientoCajaId: preview.movimientoCajaId,
        observacion: observacion.trim(),
      })
    },
    onSuccess: () => {
      message.success('Movimiento anulado')
      setPreview(null)
      setNro(null)
      setObservacion('')
      onAnulado?.()
    },
    onError: (e) => message.error(e instanceof ApiError ? e.message : String(e)),
  })

  const consultar = () => {
    if (nro == null || nro < 1) {
      message.warning('Indique el n° de movimiento.')
      return
    }
    buscar.mutate()
  }

  return (
    <CredixPanel
      title="Anular movimiento de caja"
      className="caja-saldos-anular-panel"
    >
      <Space wrap className="caja-saldos-anular-panel__busca">
        <InputNumber
          min={1}
          precision={0}
          value={nro}
          onChange={(v) => setNro(v)}
          placeholder="N° movimiento"
          style={{ width: 180 }}
          onPressEnter={consultar}
        />
        <Button
          icon={<SearchOutlined />}
          loading={buscar.isPending}
          onClick={consultar}
        >
          Consultar
        </Button>
      </Space>

      {preview ? (
        <>
          {preview.bloqueo ? (
            <Alert type="warning" showIcon message={preview.bloqueo} />
          ) : null}
          <Descriptions
            size="small"
            bordered
            column={{ xs: 1, sm: 2 }}
            className="caja-saldos-anular-panel__detalle"
          >
            <Descriptions.Item label="Caja diario">{preview.cajaDiarioId}</Descriptions.Item>
            <Descriptions.Item label="Operación">{preview.operacion}</Descriptions.Item>
            <Descriptions.Item label="Persona">{preview.persona ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Fecha">{formatFechaHora(preview.fechaReg)}</Descriptions.Item>
            <Descriptions.Item label="Importe">{formatMoney(preview.importePago)}</Descriptions.Item>
            <Descriptions.Item label="Descripción" span={2}>
              {preview.descripcion?.trim() || '—'}
            </Descriptions.Item>
          </Descriptions>
          {preview.puedeAnular ? (
            <>
              <Input.TextArea
                rows={2}
                placeholder="Observación (obligatoria)"
                value={observacion}
                onChange={(e) => setObservacion(e.target.value)}
              />
              <Button
                danger
                type="primary"
                loading={anular.isPending}
                disabled={!observacion.trim()}
                onClick={() => {
                  cajaConfirm({
                    title: `¿Anular movimiento N° ${preview.movimientoCajaId}?`,
                    content: 'La anulación queda registrada con la observación indicada.',
                    okText: 'Anular',
                    onOk: () => anular.mutateAsync(),
                  })
                }}
              >
                Anular movimiento
              </Button>
            </>
          ) : null}
        </>
      ) : (
        <p className="caja-saldos-anular-panel__hint">
          Ingrese el n° y pulse Enter — mismo flujo que Saldos del sistema anterior.
        </p>
      )}
    </CredixPanel>
  )
}
