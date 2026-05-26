import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getApiBaseUrl } from '../../api/client'
import { getAccessToken, getRefreshToken, saveTokens } from '../../auth/tokenStorage'
import { useAuth } from '../../auth/useAuth'
import type { LoginTokenResponse } from '../../types/api'
import { parseApiError } from '../../api/errors'

async function fetchReportBlob(apiPath: string): Promise<Blob> {
  const baseUrl = getApiBaseUrl()
  const headers = new Headers({ Accept: '*/*' })
  const token = getAccessToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let res = await fetch(`${baseUrl}${apiPath}`, { method: 'GET', headers })

  if (res.status === 401 && getRefreshToken()) {
    const refreshRes = await fetch(`${baseUrl}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ refreshToken: getRefreshToken() }),
    })
    if (refreshRes.ok) {
      const data = (await refreshRes.json()) as LoginTokenResponse
      saveTokens(data.accessToken, data.refreshToken, data.expiresInSeconds)
      headers.set('Authorization', `Bearer ${data.accessToken}`)
      res = await fetch(`${baseUrl}${apiPath}`, { method: 'GET', headers })
    }
  }

  if (!res.ok) throw await parseApiError(res)
  return res.blob()
}

export function ReportViewerPage() {
  const [params] = useSearchParams()
  const path = params.get('path') ?? ''
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [objectUrl, setObjectUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (authLoading) return

    if (!isAuthenticated) {
      setError(null)
      setLoading(false)
      return
    }

    if (!path.startsWith('/')) {
      setError('Ruta de informe no válida. Vuelva a generar el informe desde Reportes.')
      setLoading(false)
      return
    }

    let revoked: string | null = null
    let cancelled = false

    ;(async () => {
      try {
        const blob = await fetchReportBlob(path)
        if (cancelled) return
        const url = URL.createObjectURL(blob)
        revoked = url
        setObjectUrl(url)
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'No se pudo cargar el informe.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()

    return () => {
      cancelled = true
      if (revoked) URL.revokeObjectURL(revoked)
    }
  }, [path, authLoading, isAuthenticated])

  if (authLoading || (loading && isAuthenticated)) {
    return (
      <div className="report-viewer report-viewer--loading">
        Cargando informe…
      </div>
    )
  }

  if (!isAuthenticated) {
    const returnTo = `/reportes/visor${window.location.search}`
    return (
      <div className="report-viewer report-viewer--error">
        <p>Sesión no disponible en esta pestaña.</p>
        <p>
          <Link to="/login" state={{ from: returnTo }}>
            Iniciar sesión
          </Link>{' '}
          para ver el informe.
        </p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="report-viewer report-viewer--error">
        <p>{error}</p>
      </div>
    )
  }

  if (!objectUrl) {
    return null
  }

  return (
    <iframe
      className="report-viewer__frame"
      title="Informe"
      src={objectUrl}
    />
  )
}
