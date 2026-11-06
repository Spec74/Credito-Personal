import { Navigate, useSearchParams } from 'react-router-dom'

/**
 * Alias legacy: la morosidad por gestor es el cobro diario con filtro «solo mora».
 * Conservamos la ruta para menús/bookmarks antiguos.
 */
export function MorosidadGestorPage() {
  const [searchParams] = useSearchParams()
  const next = new URLSearchParams(searchParams)
  next.set('soloMora', '1')
  const qs = next.toString()
  return <Navigate to={qs ? `/informes/cobro-diario?${qs}` : '/informes/cobro-diario?soloMora=1'} replace />
}
