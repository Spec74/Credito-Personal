import { useEffect } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { HomeOutlined } from '@ant-design/icons'
import { Button, Typography } from 'antd'
import { CredixAlertNote, CredixPage } from '../components/credix'
import { formatMenuLabel } from '../utils/formatMenuLabel'
import {
  resolveSpaPathFromLegacyUrl,
  resolveSpaPathFromModulo,
} from '../utils/legacyRoutes'

const { Text } = Typography

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

  useEffect(() => {
    const hubPath = resolveSpaPathFromModulo(state.modulo)
    if (hubPath) navigate(hubPath, { replace: true })
  }, [state.modulo, navigate])

  const titulo = formatMenuLabel(state.titulo) || `Módulo ${menuId}`

  return (
    <CredixPage
      title={titulo}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: titulo },
      ]}
      actions={
        <Button icon={<HomeOutlined />} onClick={() => navigate('/inicio')}>
          Inicio
        </Button>
      }
    >
      <CredixAlertNote>
        Este ítem del menú (<Text code>#{menuId}</Text>) aún no tiene pantalla disponible.
        Use el menú lateral para las operaciones disponibles.
      </CredixAlertNote>
    </CredixPage>
  )
}
