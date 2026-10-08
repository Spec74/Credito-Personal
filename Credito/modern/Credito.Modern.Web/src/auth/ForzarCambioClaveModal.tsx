import { useState } from 'react'
import { Alert, Form, Input, Modal, Typography, message } from 'antd'
import { cambiarClave } from '../api/auth'
import { ApiError } from '../api/errors'
import { claveNuevaRule } from '../validation/formRules'
import { useAuth } from './useAuth'

const { Paragraph } = Typography

interface FormValues {
  claveActual: string
  claveNueva: string
  confirmar: string
}

/** Modal bloqueante: no se puede cerrar hasta cambiar la clave temporal. */
export function ForzarCambioClaveModal() {
  const { requiereCambioClave, clearRequiereCambioClave, logout } = useAuth()
  const [form] = Form.useForm<FormValues>()
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!requiereCambioClave) return null

  const onOk = async () => {
    setError(null)
    try {
      const values = await form.validateFields()
      setSubmitting(true)
      const res = await cambiarClave(values.claveActual, values.claveNueva)
      if (!res.success) {
        setError(res.mensaje ?? 'No se pudo cambiar la clave')
        return
      }
      clearRequiereCambioClave()
      form.resetFields()
      message.success('Clave actualizada. Ya puede continuar.')
    } catch (e) {
      if (e && typeof e === 'object' && 'errorFields' in e) return
      setError(e instanceof ApiError ? e.message : 'No se pudo cambiar la clave')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title="Cambio de clave obligatorio"
      closable={false}
      maskClosable={false}
      keyboard={false}
      okText="Guardar nueva clave"
      cancelText="Cerrar sesión"
      confirmLoading={submitting}
      onOk={() => void onOk()}
      onCancel={() => logout()}
      destroyOnHidden
    >
      <Paragraph type="secondary" style={{ marginBottom: 12 }}>
        Su clave es temporal. Debe definir una nueva (mínimo 8 caracteres, con letra y número)
        antes de continuar.
      </Paragraph>
      {error ? (
        <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />
      ) : null}
      <Form form={form} layout="vertical" requiredMark={false}>
        <Form.Item
          name="claveActual"
          label="Clave actual"
          rules={[{ required: true, message: 'Ingrese su clave actual' }]}
        >
          <Input.Password autoComplete="current-password" />
        </Form.Item>
        <Form.Item name="claveNueva" label="Nueva clave" rules={[claveNuevaRule]}>
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Form.Item
          name="confirmar"
          label="Confirmar nueva clave"
          dependencies={['claveNueva']}
          rules={[
            { required: true, message: 'Confirme la nueva clave' },
            ({ getFieldValue }) => ({
              validator: async (_, value: string | undefined) => {
                if ((value ?? '') !== (getFieldValue('claveNueva') ?? '')) {
                  throw new Error('Las claves no coinciden')
                }
              },
            }),
          ]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>
      </Form>
    </Modal>
  )
}
