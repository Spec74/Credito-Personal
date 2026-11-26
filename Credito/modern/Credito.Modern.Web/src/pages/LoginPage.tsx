import { useEffect, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { BankOutlined, LockOutlined, UserOutlined } from '@ant-design/icons'
import { Alert, Button, Checkbox, Form, Grid, Input, Select, Spin } from 'antd'
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
import { pickLoginSaludoDia } from '../utils/loginSaludo'
import '../styles/credix-login.css'

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
  const screens = Grid.useBreakpoint()
  const isCompact = screens.md !== true
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [clientIp, setClientIp] = useState<string | null>(null)
  const [ipResolved, setIpResolved] = useState(false)
  const [form] = Form.useForm<LoginFormValues>()
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
      <div className="credix-login-shell">
        <div className="credix-login-orbs" aria-hidden>
          <span className="credix-login-orb credix-login-orb--a" />
          <span className="credix-login-orb credix-login-orb--b" />
          <span className="credix-login-orb credix-login-orb--c" />
          <span className="credix-login-orb credix-login-orb--d" />
        </div>

        {/* Izquierda: solo atmósfera + bienvenida (logo vive en el formulario). */}
        <aside className="credix-login-stage" aria-label="Bienvenida">
          <p className="credix-login-stage__welcome">Bienvenido</p>
          <p className="credix-login-stage__hello">
            {saludoDia.saludo}. {saludoDia.linea}
          </p>
        </aside>

        <section className="credix-login-panel">
          <div className="credix-login-panel__top">
            <CredixColorSchemeToggle variant="segmented" />
          </div>

          <div className="credix-login-panel__body">
            <header className="credix-login-form__header">
              <div className="credix-login-form__brand">
                <BrandLogo className="credix-login-form__logo" fluid priority />
              </div>
              <h1 className="credix-login-form__title">Iniciar sesión</h1>
              <div className="credix-login-form__alerts" aria-live="polite">
                {oficinasError ? (
                  <Alert
                    type="warning"
                    showIcon
                    banner
                    className="credix-login-form__alert"
                    title="Oficinas no disponibles"
                    action={
                      <Button size="small" onClick={() => void oficinasQuery.refetch()}>
                        Reintentar
                      </Button>
                    }
                  />
                ) : null}
                {error ? (
                  <Alert type="error" banner showIcon title={error} className="credix-login-form__alert" />
                ) : null}
              </div>
            </header>

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
                rules={[{ required: true, message: 'Ingrese su usuario' }]}
              >
                <Input
                  autoComplete="username"
                  autoCapitalize="none"
                  autoCorrect="off"
                  spellCheck={false}
                  autoFocus={!isCompact}
                  aria-label="Usuario"
                  placeholder="Usuario"
                  prefix={<UserOutlined aria-hidden />}
                />
              </Form.Item>
              <Form.Item name="clave" rules={[{ required: true, message: 'Ingrese su contraseña' }]}>
                <Input.Password
                  autoComplete="current-password"
                  aria-label="Contraseña"
                  placeholder="Contraseña"
                  prefix={<LockOutlined aria-hidden />}
                />
              </Form.Item>
              <Form.Item
                name="oficinaId"
                rules={[{ required: true, message: 'Seleccione oficina' }]}
              >
                <Select
                  showSearch={!isCompact}
                  optionFilterProp="label"
                  prefix={<BankOutlined aria-hidden />}
                  loading={oficinasQuery.isLoading}
                  aria-label="Oficina"
                  placeholder={
                    oficinasQuery.isLoading
                      ? 'Cargando oficinas…'
                      : oficinaOptions.length
                        ? 'Oficina'
                        : 'Sin oficinas'
                  }
                  options={oficinaOptions}
                  listHeight={isCompact ? 220 : 256}
                  getPopupContainer={(node) => node.parentElement ?? document.body}
                  notFoundContent={
                    oficinasQuery.isLoading ? <Spin size="small" /> : (oficinasError ?? 'No hay datos')
                  }
                />
              </Form.Item>

              <Form.Item name="recordar" valuePropName="checked" className="credix-login-form__remember">
                <Checkbox>Recordarme</Checkbox>
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

            <p className="credix-login-form__foot">
              ¿No es colaborador? Acérquese a una agencia.{' '}
              <a href={soporte.href}>{soporte.label}</a>
            </p>
          </div>
        </section>
      </div>
    </main>
  )
}
