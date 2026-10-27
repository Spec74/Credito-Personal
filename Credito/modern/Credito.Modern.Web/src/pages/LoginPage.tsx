import { useEffect, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { BankOutlined, LockOutlined, UserOutlined } from '@ant-design/icons'
import { Alert, Button, Checkbox, Form, Input, Select, Spin, Typography } from 'antd'
import { fetchOficinas } from '../api/oficinas'
import { ApiError } from '../api/errors'
import { getAuthLoginErrorMessage } from '../auth/authLoginError'
import { useAuth } from '../auth/useAuth'
import { saveLoginProfile } from '../auth/sessionProfile'
import { BrandLogo } from '../components/brand/BrandLogo'
import { branding } from '../config/branding'
import { fetchClientPublicIp } from '../utils/clientIp'
import { parseLoginOficinaId } from '../utils/sucursalRed'
import { CredixColorSchemeToggle } from '../components/layout/CredixColorSchemeToggle'
import { pickLoginFrase } from '../utils/loginFrase'
import { capitalizarApodo, pickLoginSaludoDia } from '../utils/loginSaludo'
import '../styles/credix-login.css'

const { Text } = Typography

interface LoginFormValues {
  nombreUsuario: string
  clave: string
  oficinaId: number
  recordar?: boolean
}

function telefonoSoporte(raw: string): { href: string; label: string } {
  const digits = raw.replace(/\D/g, '')
  const label =
    digits.length === 9 ? `${digits.slice(0, 3)} ${digits.slice(3, 6)} ${digits.slice(6)}` : raw
  return { href: `tel:+51${digits}`, label }
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, isLoading, login } = useAuth()
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [clientIp, setClientIp] = useState<string | null>(null)
  const [ipResolved, setIpResolved] = useState(false)
  const [form] = Form.useForm<LoginFormValues>()
  const [frase] = useState(() => pickLoginFrase())
  const [saludoDia] = useState(() => pickLoginSaludoDia())

  const oficinasQuery = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
    retry: 2,
  })

  const from = (location.state as { from?: string } | null)?.from ?? '/inicio'

  useEffect(() => {
    let cancelled = false
    void fetchClientPublicIp().then((ip) => {
      if (!cancelled) {
        setClientIp(ip)
        setIpResolved(true)
      }
    })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      navigate(from, { replace: true })
    }
  }, [isLoading, isAuthenticated, from, navigate])

  const oficinaPrefill = parseLoginOficinaId(location.search)

  useEffect(() => {
    const list = oficinasQuery.data
    if (!list?.length) {
      return
    }
    if (oficinaPrefill > 0 && list.some((o) => o.oficinaId === oficinaPrefill)) {
      form.setFieldValue('oficinaId', oficinaPrefill)
      return
    }
    if (form.getFieldValue('oficinaId')) {
      return
    }
    const principal = list.find((o) => o.indPrincipal) ?? list[0]
    form.setFieldValue('oficinaId', principal.oficinaId)
  }, [oficinasQuery.data, form, oficinaPrefill])

  if (isLoading) {
    return (
      <div className="credix-login is-boot" translate="no">
        <Spin size="large" />
      </div>
    )
  }

  if (isAuthenticated) {
    return <Navigate to={from} replace />
  }

  const soporte = telefonoSoporte(branding.supportPhone)

  const oficinaOptions = (oficinasQuery.data ?? []).map((o) => ({
    value: o.oficinaId,
    label: o.denominacion ?? `Oficina ${o.oficinaId}`,
  }))

  const oficinasError = oficinasQuery.isError
    ? oficinasQuery.error instanceof ApiError
      ? oficinasQuery.error.message
      : 'No se pudo cargar oficinas. Compruebe la conexión con la API o recargue la página.'
    : null

  const onFinish = async (values: LoginFormValues) => {
    setError(null)
    setSubmitting(true)
    try {
      let ip = clientIp
      if (!ipResolved) {
        ip = await fetchClientPublicIp()
        setClientIp(ip)
        setIpResolved(true)
      }
      await login({
        nombreUsuario: values.nombreUsuario.trim(),
        clave: values.clave,
        oficinaId: values.oficinaId,
        clienteAcceso: ip ?? null,
        recordarSesion: values.recordar === true,
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
    <main className="credix-login" translate="no">
      <aside className="credix-login-stage">
        <div className="credix-login-stage__mesh" aria-hidden />
        <div className="credix-login-stage__grid" aria-hidden />
        <div className="credix-login-stage__rings" aria-hidden />
        <div className="credix-login-stage__hello">
          <p className="credix-login-stage__greet">
            {saludoDia.saludo}, <em>{capitalizarApodo(saludoDia.apodo)}</em>.
          </p>
          <p className="credix-login-stage__aside">{saludoDia.linea}</p>
        </div>
        <div className="credix-login-stage__body">
          <div className="credix-login-stage__brand">
            <BrandLogo onDark priority />
          </div>
          <p className="credix-login-stage__eyebrow">Acceso del personal</p>
          <blockquote className="credix-login-quote" key={frase.texto}>
            <p>{frase.texto}</p>
            {frase.fuente ? <cite>{frase.fuente}</cite> : <cite>CrediConfiable</cite>}
          </blockquote>
          <p className="credix-login-stage__lead">
            Sistema de trabajo de las sucursales. No es un portal de clientes.
          </p>
        </div>
        <p className="credix-login-stage__points">
          Entras a tu oficina · Sin alta pública · Listo para el mostrador
        </p>
      </aside>

      <section className="credix-login-panel">
        <div className="credix-login-form__head">
          <p className="credix-login-form__context">Sesión de sucursal</p>
          <CredixColorSchemeToggle variant="segmented" />
        </div>
        <div className="credix-login-form__main">
          <header className="credix-login-form__header">
            <h1 className="credix-login-form__title">Iniciar sesión</h1>
            <Text className="credix-login-form__hint">
              Usa tu usuario de trabajo. La sesión queda en la oficina que elijas.
            </Text>
          </header>

          {oficinasError ? (
            <Alert
              type="warning"
              showIcon
              className="credix-login-form__alert"
              title="Oficinas no disponibles"
              description={oficinasError}
              action={
                <Button size="small" onClick={() => void oficinasQuery.refetch()}>
                  Reintentar
                </Button>
              }
            />
          ) : null}

          {error ? (
            <Alert type="error" title={error} showIcon className="credix-login-form__alert" />
          ) : null}

          <Form<LoginFormValues>
            form={form}
            layout="vertical"
            size="large"
            className="credix-login-form__fields"
            onFinish={onFinish}
            requiredMark={false}
            initialValues={{ recordar: true }}
          >
            <Form.Item
              name="nombreUsuario"
              label="Usuario"
              rules={[{ required: true, message: 'Ingrese su usuario' }]}
            >
              <Input
                autoComplete="username"
                autoCapitalize="none"
                autoCorrect="off"
                spellCheck={false}
                autoFocus
                prefix={<UserOutlined aria-hidden />}
              />
            </Form.Item>
            <Form.Item
              name="clave"
              label="Contraseña"
              rules={[{ required: true, message: 'Ingrese su contraseña' }]}
            >
              <Input.Password
                autoComplete="current-password"
                prefix={<LockOutlined aria-hidden />}
              />
            </Form.Item>
            <Form.Item
              name="oficinaId"
              label="Oficina"
              extra="La sesión queda ligada a esta sede."
              rules={[{ required: true, message: 'Seleccione oficina' }]}
            >
              <Select
                showSearch
                optionFilterProp="label"
                prefix={<BankOutlined aria-hidden />}
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

            <Form.Item name="recordar" valuePropName="checked" className="credix-login-form__remember">
              <Checkbox>Recordar sesión en este equipo</Checkbox>
            </Form.Item>

            <Button
              type="primary"
              htmlType="submit"
              block
              className="credix-login-form__submit"
              loading={submitting}
              disabled={oficinaOptions.length === 0 && !oficinasQuery.isLoading}
            >
              Iniciar sesión
            </Button>
          </Form>
        </div>
        <div className="credix-login-form__foot">
          <p className="credix-login-form__legal">
            ¿No es colaborador? Acérquese a una agencia.{' '}
            <a href={soporte.href}>{soporte.label}</a>
          </p>
          {import.meta.env.DEV ? (
            <Text type="secondary" style={{ display: 'block', marginTop: 8, fontSize: 11 }}>
              Entorno desarrollo · API vía proxy local
            </Text>
          ) : null}
        </div>
      </section>
    </main>
  )
}
