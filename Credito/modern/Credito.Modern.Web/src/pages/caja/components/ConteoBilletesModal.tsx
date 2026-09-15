import { useEffect, useMemo, useState } from 'react'
import { Form, InputNumber, Modal, Typography } from 'antd'
import { formatMoney } from '../../../utils/formatMoney'

const { Text } = Typography

const DENOMINACIONES = [
  { name: 'd200', label: '200 soles', unit: 200 },
  { name: 'd100', label: '100 soles', unit: 100 },
  { name: 'd50', label: '50 soles', unit: 50 },
  { name: 'd20', label: '20 soles', unit: 20 },
  { name: 'd10', label: '10 soles', unit: 10 },
  { name: 'd5', label: '5 soles', unit: 5 },
  { name: 'd2', label: '2 soles', unit: 2 },
  { name: 'd1', label: '1 sol', unit: 1 },
  { name: 'c50', label: '50 céntimos', unit: 0.5 },
  { name: 'c20', label: '20 céntimos', unit: 0.2 },
  { name: 'c10', label: '10 céntimos', unit: 0.1 },
] as const

type FormValues = Record<(typeof DENOMINACIONES)[number]['name'], number>

type Props = {
  open: boolean
  importeCierre: number
  loading?: boolean
  onCancel: () => void
  onConfirm: (sobrante: number) => void | Promise<void>
}

export function ConteoBilletesModal({
  open,
  importeCierre,
  loading,
  onCancel,
  onConfirm,
}: Props) {
  const [form] = Form.useForm<FormValues>()
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    if (!open) {
      form.resetFields()
      setRevision(0)
    }
  }, [open, form])

  const conteoTotal = useMemo(() => {
    const cantidades = form.getFieldsValue()
    return DENOMINACIONES.reduce((sum, d) => {
      const qty = Number(cantidades[d.name] ?? 0)
      return sum + qty * d.unit
    }, 0)
    // eslint-disable-next-line react-hooks/exhaustive-deps -- revision fuerza recálculo al editar cantidades
  }, [form, revision])

  const sobrante = Math.max(0, conteoTotal - importeCierre)

  const handleOk = async () => {
    const values = await form.validateFields()
    const total = DENOMINACIONES.reduce((sum, d) => {
      const qty = Number(values[d.name] ?? 0)
      return sum + qty * d.unit
    }, 0)
    if (total < importeCierre) {
      Modal.warning({
        title: 'Conteo insuficiente',
        content:
          'El conteo de billetes debe ser mayor o igual al saldo final de cajas (paridad legacy).',
      })
      return
    }
    await onConfirm(sobrante)
  }

  return (
    <Modal
      title="Conteo de billetes"
      open={open}
      onCancel={onCancel}
      onOk={() => void handleOk()}
      okText="Cerrar y transferir"
      cancelText="Salir"
      confirmLoading={loading}
      width={520}
      destroyOnClose
      className="caja-saldos-conteo-modal"
    >
      <p className="caja-saldos-conteo-modal__resumen">
        Saldo final cajas: <strong>S/ {formatMoney(importeCierre)}</strong>
      </p>
      <Form
        form={form}
        layout="vertical"
        initialValues={{}}
        onValuesChange={() => setRevision((n) => n + 1)}
      >
        <div className="caja-saldos-conteo-modal__grid">
          {DENOMINACIONES.map((d) => (
            <Form.Item
              key={d.name}
              name={d.name}
              label={d.label}
              initialValue={0}
              rules={[{ type: 'number', min: 0 }]}
            >
              <InputNumber min={0} step={1} precision={0} style={{ width: '100%' }} />
            </Form.Item>
          ))}
        </div>
      </Form>
      <div className="caja-saldos-conteo-modal__totales">
        <Text>
          Conteo billetes: <strong>S/ {formatMoney(conteoTotal)}</strong>
        </Text>
        <Text type={sobrante > 0 ? 'success' : 'secondary'}>
          Sobrante: <strong>S/ {formatMoney(sobrante)}</strong>
        </Text>
      </div>
    </Modal>
  )
}
