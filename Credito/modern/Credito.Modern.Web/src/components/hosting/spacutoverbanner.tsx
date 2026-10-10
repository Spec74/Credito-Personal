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
      message="Credix web activo"
      description={`Menú y operación diaria en esta aplicación. Datos e informes vía API. Período de observación: ${data.observacionDias} días.`}
    />
  )
}
