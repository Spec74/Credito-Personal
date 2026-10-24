import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchCajaDiarioSesion } from '../../api/cajaDiario'

/** Chip de caja en el encabezado. La oficina y Salir ya viven arriba; no se duplican. */
export function CredixContextBar({ oficinaId }: { oficinaId: number }) {
  const cajaQuery = useQuery({
    queryKey: ['caja-diario-sesion', oficinaId],
    queryFn: () => fetchCajaDiarioSesion(oficinaId),
    enabled: oficinaId > 0,
    staleTime: 60_000,
    retry: false,
  })

  const caja = cajaQuery.data
  const label = cajaQuery.isLoading
    ? 'Caja…'
    : caja
      ? `Caja · ${caja.cajaDenominacion}`
      : cajaQuery.isError
        ? 'Caja —'
        : 'Sin caja'

  return (
    <Link
      to="/caja/diario"
      className={`credix-header-caja ${caja ? 'is-open' : 'is-closed'}`}
      title={caja ? `Caja abierta · ${caja.cajaDenominacion}` : 'Ir a caja diario'}
      translate="no"
    >
      {label}
    </Link>
  )
}
