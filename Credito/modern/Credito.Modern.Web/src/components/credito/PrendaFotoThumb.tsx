import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchPrendaFotoBlob } from '../../api/prendario'

export function PrendaFotoThumb({
  prendaId,
  oficinaId,
  hasFoto,
}: {
  prendaId: number
  oficinaId: number
  hasFoto: boolean
}) {
  const foto = useQuery({
    queryKey: ['prenda-foto', oficinaId, prendaId],
    queryFn: () => fetchPrendaFotoBlob(prendaId, oficinaId),
    enabled: hasFoto && oficinaId > 0 && prendaId > 0,
    staleTime: 60_000,
  })

  const [src, setSrc] = useState<string | null>(null)

  useEffect(() => {
    if (!foto.data) {
      setSrc(null)
      return
    }
    const url = URL.createObjectURL(foto.data)
    setSrc(url)
    return () => URL.revokeObjectURL(url)
  }, [foto.data])

  if (!hasFoto) return null
  if (!src) {
    return <span className="credito-prendario-ficha__foto-ph">Foto…</span>
  }
  return <img className="credito-prendario-ficha__foto" src={src} alt="Prenda" />
}
