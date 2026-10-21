import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Segmented } from 'antd'
import { fetchCajaDiarioSesion } from '../../api/cajaDiario'
import { useCredixDensity } from '../../hooks/useCredixDensity'
import type { CredixDensity } from '../../theme/credixDensity'

export function CredixContextBar({
  oficinaId,
  oficinaNombre,
  onCambiarSede,
}: {
  oficinaId: number
  oficinaNombre: string | null
  onCambiarSede?: () => void
}) {
  const [density, setDensity] = useCredixDensity()
  const cajaQuery = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0,
    staleTime: 60_000,
    retry: false,
  })

  const caja = cajaQuery.data
  const cajaLabel = cajaQuery.isLoading
    ? 'Caja…'
    : caja
      ? `Caja abierta · ${caja.cajaDenominacion}`
      : cajaQuery.isError
        ? 'Caja no consultada'
        : 'Sin caja abierta'

  return (
    <div className="credix-context-bar" role="status">
      <div className="credix-context-bar__facts">
        <span className="credix-context-bar__office">
          {oficinaNombre ?? (oficinaId > 0 ? `Oficina ${oficinaId}` : 'Sin oficina')}
        </span>
        {onCambiarSede ? (
          <button
            type="button"
            className="credix-context-bar__cambiar"
            onClick={onCambiarSede}
          >
            Cambiar sede
          </button>
        ) : null}
        <Link
          to="/caja/diario"
          className={`credix-context-bar__caja ${caja ? 'is-open' : 'is-closed'}`}
        >
          {cajaLabel}
        </Link>
      </div>
      <Segmented
        className="credix-context-bar__density"
        size="small"
        value={density}
        aria-label="Densidad de pantalla"
        onChange={(v) => setDensity(v as CredixDensity)}
        options={[
          { label: 'C', value: 'compact' },
          { label: 'E', value: 'standard' },
          { label: 'H', value: 'comfortable' },
        ]}
      />
    </div>
  )
}
