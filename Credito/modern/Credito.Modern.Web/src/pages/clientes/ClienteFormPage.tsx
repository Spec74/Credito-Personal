import { useParams, useLocation } from 'react-router-dom'
import { ClienteMantenerForm } from './ClienteMantenerForm'

export function ClienteFormPage() {
  const { personaId: personaIdParam } = useParams<{ personaId: string }>()
  const location = useLocation()
  const esEdicion = location.pathname.includes('/editar/')
  const personaId = esEdicion ? Number(personaIdParam) : 0

  return <ClienteMantenerForm esEdicion={esEdicion} personaId={personaId} />
}
