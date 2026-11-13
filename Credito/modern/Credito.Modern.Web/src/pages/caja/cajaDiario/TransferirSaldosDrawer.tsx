import { useEffect, useMemo, useState } from 'react'
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
import { fetchValoresTabla } from '../../../api/maestros'
import { CajaDrawer } from '../../../components/caja/CajaDrawer'
import { useEsCentral } from '../../../hooks/useEsCentral'
import { formatMoney } from '../../../utils/formatMoney'
import {
  buildTipoPagoOptions,
  tipoPagoOptionRender,
  tipoPagoPopupRender,
} from '../../tesoreria/components/TipoPagoBrand'
import {
  cajaToastError,
  cajaToastSuccess,
  cajaToastWarning,
} from './cajaFeedback'
import type { CajaSession } from './types'
import { errMsg } from './types'
import { FIELD_MAX, glosaRules, moneyRequired } from '../../../validation/formRules'

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
  const { esCentral } = useEsCentral()
  const tipoPagoId = Form.useWatch('tipoPagoId', form) as number | undefined

  const tiposPagoQuery = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    enabled: open,
    staleTime: 5 * 60_000,
  })

  const tipoPagoOptions = useMemo(
    () => buildTipoPagoOptions(tiposPagoQuery.data ?? [], { esCentral }),
    [tiposPagoQuery.data, esCentral],
  )

  const medioId = tipoPagoId && tipoPagoId > 0 ? tipoPagoId : 1

  const saldoQuery = useQuery({
    queryKey: ['caja-saldo-transferir', ctx.cajaDiarioId, medioId],
    queryFn: () => fetchSaldoCuentaCajaDiario(ctx.cajaDiarioId, medioId),
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
      tipoPagoId: number
    }) =>
      transferirSaldosCajaDiario({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        importe: values.importe,
        descripcion: values.descripcion,
        cajaIdDestino: values.cajaIdDestino ?? null,
        tipoPagoId: values.tipoPagoId,
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
    } else {
      form.setFieldsValue({ tipoPagoId: 1 })
    }
  }, [open, form])

  const saldo = saldoQuery.data?.saldo ?? null
  const medioLabel =
    tipoPagoOptions.find((o) => o.value === medioId)?.label ?? 'medio'
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
        Saldo del medio ({medioLabel}):{' '}
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
        initialValues={{ tipoPagoId: 1 }}
        onFinish={(v) => {
          if (saldo != null && v.importe > saldo) {
            cajaToastWarning(
              'El monto a transferir debe ser menor o igual al saldo del medio.',
            )
            return
          }
          transferir.mutate({
            importe: v.importe,
            descripcion: v.descripcion,
            cajaIdDestino: modo === 'caja' ? v.cajaIdDestino : null,
            tipoPagoId: v.tipoPagoId && v.tipoPagoId > 0 ? v.tipoPagoId : 1,
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

        <Form.Item
          name="tipoPagoId"
          label="Medio de pago"
          rules={[{ required: true, message: 'Seleccione el medio' }]}
        >
          <Select
            showSearch
            optionFilterProp="label"
            options={tipoPagoOptions}
            optionRender={tipoPagoOptionRender}
            popupRender={tipoPagoPopupRender}
            placeholder="Efectivo, Yape René, Plin…"
          />
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
            ...moneyRequired(),
            {
              validator: (_, value) => {
                if (saldo != null && value > saldo) {
                  return Promise.reject(
                    new Error('Monto mayor al saldo disponible del medio'),
                  )
                }
                return Promise.resolve()
              },
            },
          ]}
        >
          <InputNumber min={0.01} step={0.01} precision={2} style={{ width: '100%' }} />
        </Form.Item>

        <Form.Item name="descripcion" label="Descripción" rules={glosaRules}>
          <Input.TextArea rows={2} maxLength={FIELD_MAX.glosa} showCount />
        </Form.Item>
      </Form>

      <Alert
        type="info"
        showIcon
        message={
          modo === 'boveda'
            ? 'El medio elegido (p. ej. Yape René) se acredita igual en la bóveda; no se convierte a efectivo.'
            : 'Transfiere el saldo del medio a otra caja diaria abierta.'
        }
        style={{ marginTop: 8 }}
      />
    </CajaDrawer>
  )
}
