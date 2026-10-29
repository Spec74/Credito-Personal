import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import type { SelectProps } from 'antd'
import { fetchArticulosGestion, type ArticuloGestionRow } from '../../api/articulosCrud'
import { CredixSelect } from './CredixSelect'

export type CredixArticuloSelectProps = Omit<
  SelectProps<number>,
  'options' | 'optionFilterProp' | 'showSearch'
> & {
  onArticulo?: (item: ArticuloGestionRow | undefined) => void
}

function etiqueta(a: ArticuloGestionRow): string {
  const nombre = a.denominacion?.trim() || `Artículo ${a.articuloId}`
  const codigo = a.codArticulo?.trim()
  const stock = a.stockEnAlmacen ?? 0
  const base = codigo ? `${nombre} · ${codigo}` : nombre
  return `${base} · stock ${stock}`
}

/** Busca artículo por nombre o código. No pide el id interno. */
export function CredixArticuloSelect({
  onArticulo,
  onChange,
  notFoundContent,
  ...props
}: CredixArticuloSelectProps) {
  const query = useQuery({
    queryKey: ['articulos-gestion', false],
    queryFn: () => fetchArticulosGestion(false),
  })

  const options = useMemo(
    () =>
      (query.data ?? [])
        .filter((a) => a.articuloId > 0)
        .map((a) => ({ value: a.articuloId, label: etiqueta(a) })),
    [query.data],
  )

  return (
    <CredixSelect<number>
      allowClear
      placeholder="Buscar artículo"
      loading={query.isLoading}
      options={options}
      notFoundContent={
        notFoundContent ??
        (options.length === 0 && !query.isLoading ? (
          <span>
            No hay artículos.{' '}
            <Link to="/maestros/articulos">Crear artículo</Link>
          </span>
        ) : (
          'Sin coincidencias. Cambie la búsqueda.'
        ))
      }
      {...props}
      onChange={(value, option) => {
        onChange?.(value, option)
        onArticulo?.(query.data?.find((a) => a.articuloId === value))
      }}
    />
  )
}
