import { useQuery } from '@tanstack/react-query'
import { Alert } from 'antd'
import { fetchHostingUiConfig } from '../../api/hosting'

export function SpaCutoverBanner() {
  const { data } = useQuery({
    queryKey: ['hosting', 'ui-config'],
    queryFn: fetchHostingUiConfig,
    staleTime: 10 * 60_000,
  })

  if (!data?.useSpaForModule) return null

  return (
    <Alert
      type="success"
      showIcon
      style={{ marginBottom: 16 }}
      message="Sistema moderno activo (Fase 5C-7)"
      description={`Menú y operación diaria en esta SPA. Datos e informes tabulares vía API. Período de observación recomendado: ${data.observacionDias} días antes de retirar el MVC en producción.`}
    />
  )
}
