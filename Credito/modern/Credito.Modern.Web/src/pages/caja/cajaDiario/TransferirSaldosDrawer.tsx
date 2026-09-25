import { useEffect, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Form,
  Input,
  InputNumber,
  Radio,
  Select,
  Typography,
} from 'antd'
import {
  fetchCajasAbiertasTransferencia,
  fetchSaldoCuentaCajaDiario,
  transferirSaldosCajaDiario,
} from '../../../api/cajaDiario'
import { CajaDrawer } from '../../../components/caja/CajaDrawer'
import { formatMoney } from '../../../utils/formatMoney'
import {
  cajaToastError,
  cajaToastSuccess,
  cajaToastWarning,
} from './cajaFeedback'
import type { CajaSession } from './types'
import { errMsg } from './types'

const { Text } = Typography

type Modo = 'boveda' | 'caja'

export function TransferirSaldosDrawer({
  open,
  ctx,
  onClose,
  onSuccess,
}: {
  open: boolean
  ctx: CajaSession
  onClose: () => void
  onSuccess: () => void
}) {
  const [form] = Form.useForm()
  const [modo, setModo] = useState<Modo>('boveda')

  const saldoQuery = useQuery({
    queryKey: ['caja-saldo-transferir', ctx.cajaDiarioId],
    queryFn: () => fetchSaldoCuentaCajaDiario(ctx.cajaDiarioId),
    enabled: open,
  })

  const cajasQuery = useQuery({
    queryKey: ['cajas-abiertas-transferir', ctx.oficinaId],
    queryFn: () => fetchCajasAbiertasTransferencia(ctx.oficinaId),
    enabled: open && modo === 'caja',
  })

  const transferir = useMutation({
    mutationFn: (values: {
      importe: number
      descripcion: string
      cajaIdDestino?: number | null
    }) =>
      transferirSaldosCajaDiario({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        importe: values.importe,
        descripcion: values.descripcion,
        cajaIdDestino: values.cajaIdDestino ?? null,
      }),
    onSuccess: () => {
      cajaToastSuccess('Transferencia registrada', 'caja-transfer')
      form.resetFields()
      onSuccess()
      onClose()
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  useEffect(() => {
    if (!open) {
      form.resetFields()
      setModo('boveda')
    }
  }, [open, form])

  const saldo = saldoQuery.data?.saldo ?? null
  const cajasOptions = (cajasQuery.data ?? [])
    .filter((c) => c.cajaId !== ctx.cajaId)
    .map((c) => ({ value: c.cajaId, label: c.etiqueta }))

  return (
    <CajaDrawer
      title="Transferir saldos"
      open={open}
      onClose={onClose}
      width={400}
      destroyOnClose
      footer={
        <Button
          type="primary"
          loading={transferir.isPending}
          onClick={() => form.submit()}
        >
          Transferir
        </Button>
      }
    >
      <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
        Saldo en caja (efectivo):{' '}
        <strong>
          {saldoQuery.isLoading
            ? '…'
            : saldo != null
              ? formatMoney(saldo)
              : '—'}
        </strong>
      </Text>

      <Form
        form={form}
        layout="vertical"
        onFinish={(v) => {
          if (saldo != null && v.importe > saldo) {
            cajaToastWarning(
              'El monto a transferir debe ser menor o igual al saldo en caja.',
            )
            return
          }
          transferir.mutate({
            importe: v.importe,
            descripcion: v.descripcion,
            cajaIdDestino: modo === 'caja' ? v.cajaIdDestino : null,
          })
        }}
      >
        <Form.Item label="Destino">
          <Radio.Group
            value={modo}
            onChange={(e) => setModo(e.target.value as Modo)}
          >
            <Radio value="boveda">Bóveda de la oficina</Radio>
            <Radio value="caja">Otra caja abierta</Radio>
          </Radio.Group>
        </Form.Item>

        {modo === 'caja' && (
          <Form.Item
            name="cajaIdDestino"
            label="Caja destino"
            rules={[{ required: true, message: 'Seleccione caja' }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              loading={cajasQuery.isLoading}
              options={cajasOptions}
              placeholder="Caja abierta"
            />
          </Form.Item>
        )}

        <Form.Item
          name="importe"
          label="Monto"
          rules={[
            { required: true, type: 'number', min: 0.01 },
            {
              validator: (_, value) => {
                if (saldo != null && value > saldo) {
                  return Promise.reject(
                    new Error('Monto mayor al saldo disponible'),
                  )
                }
                return Promise.resolve()
              },
            },
          ]}
        >
          <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
        </Form.Item>

        <Form.Item
          name="descripcion"
          label="Descripción"
          rules={[{ required: true, whitespace: true }]}
        >
          <Input.TextArea rows={2} />
        </Form.Item>
      </Form>

      <Alert
        type="info"
        showIcon
        message="Misma operación que el diálogo legacy «Transferir saldos» en arqueo."
        style={{ marginTop: 8 }}
      />
    </CajaDrawer>
  )
}
