import { useEffect, useMemo, useState } from 'react'
import { Form, InputNumber, Modal, Typography } from 'antd'
import { formatMoney } from '../../../utils/formatMoney'
import { roundMoney, sumMoney } from '../../../utils/money'

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
  /** Efectivo tipado a cubrir con billetes (no incluye Yape/Interbank…). */
  importeCierre: number
  /** Posición total (efectivo + digitales) solo informativa. */
  importeTotal?: number
  loading?: boolean
  onCancel: () => void
  onConfirm: (sobrante: number) => void | Promise<void>
}

export function ConteoBilletesModal({
  open,
  importeCierre,
  importeTotal,
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

  const sumarConteo = (cantidades: Partial<FormValues>) =>
    sumMoney(
      DENOMINACIONES.map((d) =>
        roundMoney(Number(cantidades[d.name] ?? 0) * d.unit),
      ),
    )

  const conteoTotal = useMemo(() => {
    return sumarConteo(form.getFieldsValue())
    // eslint-disable-next-line react-hooks/exhaustive-deps -- revision fuerza recálculo al editar cantidades
  }, [form, revision])

  const sobrante = Math.max(0, roundMoney(conteoTotal - importeCierre))

  const handleOk = async () => {
    const values = await form.validateFields()
    const total = sumarConteo(values)
    const sobranteConfirm = Math.max(0, roundMoney(total - importeCierre))
    if (total < importeCierre) {
      Modal.warning({
        title: 'Conteo insuficiente',
        content: `El conteo de billetes (S/ ${formatMoney(total)}) debe ser mayor o igual al efectivo tipado en caja (S/ ${formatMoney(importeCierre)}). Los medios digitales no se cuentan aquí: pasan a bóveda con su mismo tipo de pago.`,
      })
      return
    }
    await onConfirm(sobranteConfirm)
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
        Efectivo a contar: <strong>S/ {formatMoney(importeCierre)}</strong>
        {importeTotal != null && importeTotal !== importeCierre ? (
          <>
            {' '}
            · Posición total (con digitales): <strong>S/ {formatMoney(importeTotal)}</strong>
          </>
        ) : null}
      </p>
      <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
        Solo cuente billetes/monedas. Yape, Interbank, BCP, Plin, etc. se transfieren a bóveda
        tipados; el sobrante del conteo se registra como efectivo.
      </Text>
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
          Sobrante (efectivo): <strong>S/ {formatMoney(sobrante)}</strong>
        </Text>
      </div>
    </Modal>
  )
}
