import { useEffect, useState } from 'react'
import { Button } from 'antd'
import { adjuntoUrl, descargarAdjunto, type ComunicadoAdjunto } from '../../api/intranet'
import { getAccessToken } from '../../auth/tokenStorage'

export function AdjuntoVista({ adjunto }: { adjunto: ComunicadoAdjunto }) {
  const esImagen = adjunto.contentType.startsWith('image/')
  const [src, setSrc] = useState<string | null>(null)

  useEffect(() => {
    if (!esImagen) return
    let href: string | null = null
    let cancel = false
    void (async () => {
      const token = getAccessToken()
      const res = await fetch(adjuntoUrl(adjunto.adjuntoId), {
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      })
      if (!res.ok || cancel) return
      const blob = await res.blob()
      href = URL.createObjectURL(blob)
      if (!cancel) setSrc(href)
    })()
    return () => {
      cancel = true
      if (href) URL.revokeObjectURL(href)
    }
  }, [adjunto.adjuntoId, esImagen])

  if (esImagen && src) {
    return (
      <li>
        <img className="credix-comunicado-img" src={src} alt={adjunto.nombreOriginal} />
        <Button
          type="link"
          onClick={() => void descargarAdjunto(adjunto.adjuntoId, adjunto.nombreOriginal)}
        >
          {adjunto.nombreOriginal}
        </Button>
      </li>
    )
  }

  return (
    <li>
      <Button
        type="link"
        onClick={() => void descargarAdjunto(adjunto.adjuntoId, adjunto.nombreOriginal)}
      >
        {adjunto.nombreOriginal}
      </Button>
    </li>
  )
}
