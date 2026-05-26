import { useEffect, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Card,
  message,
  Checkbox,
  Collapse,
  Form,
  Input,
  Select,
  Spin,
  Typography,
} from 'antd'
import { fetchOficinas } from '../api/oficinas'
import { ApiError } from '../api/errors'
import { getAuthLoginErrorMessage } from '../auth/authLoginError'
import { useAuth } from '../auth/useAuth'
import { saveLoginProfile } from '../auth/sessionProfile'
import { BrandLogo } from '../components/brand/BrandLogo'
import { branding } from '../config/branding'
import { registrarAccesoIp } from '../api/client'
import { fetchClientPublicIp } from '../utils/clientIp'

const { Text } = Typography


interface LoginFormValues {
  nombreUsuario: string
  clave: string
  oficinaId: number
  clienteAccesoManual?: string
  recordar?: boolean
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, isLoading, login } = useAuth()
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [registrandoIp, setRegistrandoIp] = useState(false)
  const [clientIp, setClientIp] = useState<string | null>(null)
  const [form] = Form.useForm<LoginFormValues>()

  const oficinasQuery = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
    retry: 2,
  })

  const from =
    (location.state as { from?: string } | null)?.from ?? '/inicio'

  useEffect(() => {
    void fetchClientPublicIp().then(setClientIp)
  }, [])

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      navigate(from, { replace: true })
    }
  }, [isLoading, isAuthenticated, from, navigate])

  useEffect(() => {
    const list = oficinasQuery.data
    if (!list?.length || form.getFieldValue('oficinaId')) {
      return
    }
    const principal = list.find((o) => o.indPrincipal) ?? list[0]
    form.setFieldValue('oficinaId', principal.oficinaId)
  }, [oficinasQuery.data, form])

  if (isLoading) {
    return (
      <div style={{ display: 'grid', placeItems: 'center', minHeight: '100vh' }}>
        <Spin size="large" />
      </div>
    )
  }

  if (isAuthenticated) {
    return <Navigate to={from} replace />
  }

  const oficinaOptions = (oficinasQuery.data ?? []).map((o) => ({
    value: o.oficinaId,
    label: o.denominacion ?? `Oficina ${o.oficinaId}`,
  }))

  const oficinasError = oficinasQuery.isError
    ? oficinasQuery.error instanceof ApiError
      ? oficinasQuery.error.message
      : 'No se pudo cargar oficinas. Verifique que el proxy (puerto 9080) esté en marcha.'
    : null

  const onFinish = async (values: LoginFormValues) => {
    setError(null)
    setSubmitting(true)
    try {
      const clienteAcceso =
        values.clienteAccesoManual?.trim() || clientIp || undefined
      await login({
        nombreUsuario: values.nombreUsuario.trim(),
        clave: values.clave,
        oficinaId: values.oficinaId,
        clienteAcceso: clienteAcceso ?? null,
      })
      const oficinaLabel =
        oficinaOptions.find((o) => o.value === values.oficinaId)?.label ??
        `Oficina ${values.oficinaId}`
      saveLoginProfile(values.nombreUsuario.trim(), oficinaLabel)
      navigate(from, { replace: true })
    } catch (err) {
      setError(getAuthLoginErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="credix-login-wrap">
      <Card className="credix-login-card" styles={{ body: { paddingTop: 20 } }}>
        <BrandLogo showTagline />

        <Text
          type="secondary"
          style={{ display: 'block', textAlign: 'center', marginTop: 8 }}
        >
          {branding.systemDescription}
        </Text>

        {oficinasError && (
          <Alert
            type="warning"
            showIcon
            style={{ marginTop: 16 }}
            message="Oficinas no disponibles"
            description={oficinasError}
          />
        )}

        {error && (
          <Alert
            type="error"
            message={error}
            showIcon
            style={{ marginTop: 16 }}
          />
        )}

        <Form<LoginFormValues>
          form={form}
          layout="vertical"
          style={{ marginTop: 20 }}
          onFinish={onFinish}
          requiredMark={false}
          initialValues={{ recordar: true }}
        >
          <Form.Item
            name="nombreUsuario"
            label="Usuario"
            rules={[{ required: true, message: 'Ingrese su usuario' }]}
          >
            <Input autoComplete="username" size="large" />
          </Form.Item>
          <Form.Item
            name="clave"
            label="Contraseña"
            rules={[{ required: true, message: 'Ingrese su contraseña' }]}
          >
            <Input.Password autoComplete="current-password" size="large" />
          </Form.Item>
          <Form.Item
            name="oficinaId"
            label="Oficina"
            rules={[{ required: true, message: 'Seleccione oficina' }]}
          >
            <Select
              size="large"
              showSearch
              optionFilterProp="label"
              loading={oficinasQuery.isLoading}
              placeholder={
                oficinasQuery.isLoading
                  ? 'Cargando oficinas…'
                  : oficinaOptions.length
                    ? 'Seleccione oficina'
                    : 'Sin oficinas'
              }
              options={oficinaOptions}
              notFoundContent={
                oficinasQuery.isLoading ? (
                  <Spin size="small" />
                ) : (
                  (oficinasError ?? 'No hay datos')
                )
              }
            />
          </Form.Item>

          <Form.Item name="recordar" valuePropName="checked" style={{ marginBottom: 8 }}>
            <Checkbox>Recordar sesión en este equipo</Checkbox>
          </Form.Item>

          <Collapse
            ghost
            size="small"
            items={[
              {
                key: 'acceso',
                label: 'Acceso por IP (equipo autorizado)',
                children: (
                  <>
                    <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                      Equivalente al campo oculto del login clásico. IP detectada:{' '}
                      <strong>{clientIp ?? 'obteniendo…'}</strong>
                    </Text>
                    <Form.Item
                      name="clienteAccesoManual"
                      label="IP manual (opcional)"
                      style={{ marginBottom: 8 }}
                    >
                      <Input placeholder="Solo si la detección automática falla" />
                    </Form.Item>
                    <Button
                      size="small"
                      loading={registrandoIp}
                      onClick={async () => {
                        const manual = form.getFieldValue('clienteAccesoManual')?.trim()
                        const ip = manual || clientIp
                        if (!ip) {
                          setError('No hay IP detectada; ingrese IP manual.')
                          return
                        }
                        setRegistrandoIp(true)
                        try {
                          await registrarAccesoIp(ip)
                          message.success(`IP ${ip} registrada en MAESTRO.Acceso`)
                        } catch (e) {
                          setError(
                            e instanceof ApiError ? e.message : 'No se pudo registrar la IP',
                          )
                        } finally {
                          setRegistrandoIp(false)
                        }
                      }}
                    >
                      Registrar equipo (Acceso)
                    </Button>
                  </>
                ),
              },
            ]}
          />

          <Button
            type="primary"
            htmlType="submit"
            block
            size="large"
            loading={submitting}
            style={{ marginTop: 8 }}
            disabled={oficinaOptions.length === 0 && !oficinasQuery.isLoading}
          >
            Iniciar sesión
          </Button>
        </Form>

        {import.meta.env.DEV && (
          <Text
            type="secondary"
            style={{ display: 'block', marginTop: 12, fontSize: 11 }}
          >
            Entorno desarrollo · API vía proxy local
          </Text>
        )}
      </Card>
    </div>
  )
}
