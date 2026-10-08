import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Button, Space } from 'antd'
import { DownloadOutlined, FilePdfOutlined, ReloadOutlined } from '@ant-design/icons'
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

/** Cabecera mágica de un PDF válido (%PDF-). */
async function assertPdfMagic(blob: Blob): Promise<Blob> {
  const head = new Uint8Array(await blob.slice(0, 5).arrayBuffer())
  const magic = String.fromCharCode(...head)
  if (magic !== '%PDF-') {
    const preview = await blob.slice(0, 200).text().catch(() => '')
    if (preview.trimStart().startsWith('{') || preview.includes('ProblemDetails')) {
      throw new Error(
        'La API no devolvió un PDF (respuesta de error). Verifique sesión y vuelva a generar el informe.',
      )
    }
    if (preview.includes('<!DOCTYPE') || preview.includes('<html')) {
      throw new Error(
        'Se recibió HTML en lugar del PDF (¿API mal configurada en Vercel?). Contacte a soporte.',
      )
    }
    throw new Error('El archivo recibido no es un PDF válido. Genere el informe de nuevo.')
  }
  if (blob.type !== 'application/pdf') {
    return new Blob([blob], { type: 'application/pdf' })
  }
  return blob
}

async function fetchReportBlob(apiPath: string): Promise<Blob> {
  const baseUrl = getApiBaseUrl().replace(/\/$/, '')
  const headers = new Headers({ Accept: 'application/pdf, application/json, */*' })
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
  if (!isPdfReportApiPath(apiPath)) {
    throw new Error('El visor solo admite PDF.')
  }
  return assertPdfMagic(await res.blob())
}

function resolveApiPath(params: URLSearchParams): { path: string | null; error: string | null } {
  const rid = params.get('rid')?.trim() ?? ''
  if (rid) {
    const stashed = takeReportApiPath(rid)
    if (!stashed) {
      return {
        path: null,
        error:
          'El enlace del informe expiró o no es válido en esta sesión. Genere el PDF de nuevo desde el simulador o el informe.',
      }
    }
    return { path: stashed, error: null }
  }

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
  // Resolver una sola vez por rid (localStorage); no en cada render estricto.
  const resolved = useMemo(() => resolveApiPath(params), [params])
  const path = resolved.path ?? ''
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [objectUrl, setObjectUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [reloadToken, setReloadToken] = useState(0)
  const mobile = useMemo(() => isMobileViewer(), [])

  useEffect(() => {
    if (authLoading) return

    if (!isAuthenticated) {
      setError(null)
      setLoading(false)
      setObjectUrl(null)
      return
    }

    if (resolved.error || !path) {
      setError(resolved.error ?? 'Ruta de informe no válida.')
      setLoading(false)
      setObjectUrl(null)
      return
    }

    let revoked: string | null = null
    let cancelled = false
    setLoading(true)
    setError(null)

    ;(async () => {
      try {
        const blob = await fetchReportBlob(path)
        if (cancelled) return
        const url = URL.createObjectURL(blob)
        revoked = url
        setObjectUrl(url)
      } catch (e) {
        if (!cancelled) {
          setObjectUrl(null)
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
  }, [path, resolved.error, authLoading, isAuthenticated, reloadToken])

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
        <Space style={{ marginTop: 16 }}>
          <Button
            type="primary"
            icon={<ReloadOutlined />}
            onClick={() => setReloadToken((n) => n + 1)}
          >
            Reintentar
          </Button>
          <Button href="/credito/simulador">Volver al simulador</Button>
        </Space>
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
    <div className="report-viewer report-viewer--desktop">
      <div className="report-viewer__toolbar">
        <Button
          size="small"
          icon={<DownloadOutlined />}
          href={objectUrl}
          download="informe-credix.pdf"
        >
          Descargar PDF
        </Button>
      </div>
      <iframe className="report-viewer__frame" title="Informe" src={objectUrl} />
    </div>
  )
}
