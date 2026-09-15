import { useEffect, useMemo } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ArrowRightOutlined, HomeOutlined } from '@ant-design/icons'
import { Button, Space, Typography } from 'antd'
import {
  CredixAlertNote,
  CredixHubGrid,
  CredixHubIntro,
  CredixPage,
} from '../components/credix'
import { moduleHubSuggestions } from '../config/moduleHubSuggestions'
import { formatMenuLabel } from '../utils/formatMenuLabel'
import {
  resolveSpaPathFromLegacyUrl,
  resolveSpaPathFromModulo,
} from '../utils/legacyRoutes'

const { Paragraph, Text } = Typography

interface LocationState {
  titulo?: string | null
  modulo?: string | null
  legacyUrl?: string | null
}

export function ModulePlaceholderPage() {
  const navigate = useNavigate()
  const { menuId } = useParams()
  const location = useLocation()
  const state = (location.state as LocationState | null) ?? {}

  useEffect(() => {
    const legacy = state.legacyUrl?.trim()
    if (!legacy) return
    const spaPath = resolveSpaPathFromLegacyUrl(legacy)
    if (spaPath) navigate(spaPath, { replace: true })
  }, [state.legacyUrl, navigate])

  const titulo = formatMenuLabel(state.titulo) || `Módulo ${menuId}`
  const hubPath = resolveSpaPathFromModulo(state.modulo)
  const suggestions = useMemo(
    () => moduleHubSuggestions(state.modulo),
    [state.modulo],
  )

  const stats = useMemo(
    () => [
      { value: menuId ?? '—', label: 'Ítem menú' },
      {
        value: state.modulo ? formatMenuLabel(state.modulo) : 'General',
        label: 'Módulo',
      },
      { value: hubPath ? 'Hub SPA' : 'Atajos', label: 'Siguiente paso', tone: 'green' as const },
    ],
    [menuId, state.modulo, hubPath],
  )

  return (
    <CredixPage
      title={titulo}
      stats={stats}
      subtitle={
        state.modulo ? (
          <>
            Área: <Text strong>{formatMenuLabel(state.modulo)}</Text>
          </>
        ) : (
          'Acceso desde el menú lateral'
        )
      }
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: titulo },
      ]}
      actions={
        <Space wrap>
          {hubPath ? (
            <Button type="primary" icon={<ArrowRightOutlined />} onClick={() => navigate(hubPath)}>
              Ir al módulo
            </Button>
          ) : null}
          <Button icon={<HomeOutlined />} onClick={() => navigate('/inicio')}>
            Inicio
          </Button>
        </Space>
      }
    >
      <CredixAlertNote>
        Este ítem del menú (<Text code>#{menuId}</Text>) no tiene pantalla dedicada en la SPA.
        Elija una operación equivalente del módulo o use los accesos sugeridos.
      </CredixAlertNote>
      <CredixHubIntro>
        {hubPath ? (
          <Paragraph style={{ marginBottom: 0 }}>
            <Link to={hubPath}>Abrir hub {formatMenuLabel(state.modulo)}</Link> ·{' '}
            <Link to="/inicio">Inicio</Link>
          </Paragraph>
        ) : (
          <Paragraph style={{ marginBottom: 0 }}>
            <Link to="/inicio">Inicio</Link> · <Link to="/informes">Informes</Link>
          </Paragraph>
        )}
      </CredixHubIntro>
      <CredixHubGrid
        variant="module"
        sections={[
          {
            title: 'Operaciones sugeridas',
            links: suggestions,
          },
        ]}
      />
    </CredixPage>
  )
}
