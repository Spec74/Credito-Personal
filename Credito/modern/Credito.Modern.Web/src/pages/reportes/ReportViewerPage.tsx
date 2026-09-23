import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Button, Space } from 'antd'
import { DownloadOutlined, FilePdfOutlined } from '@ant-design/icons'
import { getApiBaseUrl } from '../../api/client'
import { getAccessToken, getRefreshToken, saveTokens } from '../../auth/tokenStorage'
import { useAuth } from '../../auth/useAuth'
import type { LoginTokenResponse } from '../../types/api'
import { parseApiError } from '../../api/errors'
import { isAllowedReportApiPath, isPdfReportApiPath, takeReportApiPath } from '../../utils/reportVisor'

function isMobileViewer(): boolean {
  if (typeof navigator === 'undefined') return false
  return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent)
}

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
  const blob = await res.blob()
  if (!isPdfReportApiPath(apiPath)) {
    throw new Error('El visor solo admite PDF.')
  }
  // Algunos móviles no embeben blobs sin MIME PDF explícito.
  if (blob.type !== 'application/pdf') {
    return new Blob([blob], { type: 'application/pdf' })
  }
  return blob
}

function resolveApiPath(params: URLSearchParams): { path: string | null; error: string | null } {
  const rid = params.get('rid')?.trim() ?? ''
  if (rid) {
    const stashed = takeReportApiPath(rid)
    if (!stashed) {
      return {
        path: null,
        error:
          'El enlace del informe expiró o no es válido en esta sesión. Genérelo de nuevo desde Reportes.',
      }
    }
    return { path: stashed, error: null }
  }

  // Compatibilidad temporal con bookmarks antiguos ?path=/credito/...-pdf
  const legacy = params.get('path') ?? ''
  if (legacy) {
    if (!isAllowedReportApiPath(legacy) || !isPdfReportApiPath(legacy)) {
      return { path: null, error: 'Ruta de informe no válida.' }
    }
    return { path: legacy, error: null }
  }

  return {
    path: null,
    error: 'Ruta de informe no válida. Vuelva a generar el informe desde Reportes.',
  }
}

export function ReportViewerPage() {
  const [params] = useSearchParams()
  const resolved = useMemo(() => resolveApiPath(params), [params])
  const path = resolved.path ?? ''
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [objectUrl, setObjectUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const mobile = useMemo(() => isMobileViewer(), [])

  useEffect(() => {
    if (authLoading) return

    if (!isAuthenticated) {
      setError(null)
      setLoading(false)
      return
    }

    if (resolved.error || !path) {
      setError(resolved.error ?? 'Ruta de informe no válida.')
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
  }, [path, resolved.error, authLoading, isAuthenticated])

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

  if (mobile) {
    return (
      <div className="report-viewer report-viewer--mobile">
        <FilePdfOutlined className="report-viewer__icon" />
        <p>El visor PDF del navegador móvil no embebe el archivo. Ábralo o descárguelo:</p>
        <Space direction="vertical" size="middle" style={{ width: 'min(100%, 320px)' }}>
          <Button
            type="primary"
            block
            size="large"
            icon={<FilePdfOutlined />}
            href={objectUrl}
            target="_blank"
            rel="noopener noreferrer"
            onClick={() => {
              window.open(objectUrl, '_blank', 'noopener,noreferrer')
            }}
          >
            Abrir PDF
          </Button>
          <Button
            block
            size="large"
            icon={<DownloadOutlined />}
            href={objectUrl}
            download="informe-credix.pdf"
          >
            Descargar PDF
          </Button>
        </Space>
      </div>
    )
  }

  return (
    <iframe
      className="report-viewer__frame"
      title="Informe"
      src={objectUrl}
    />
  )
}
