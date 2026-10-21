import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Empty, Segmented, Spin } from 'antd'
import { fetchComunicado, fetchComunicados, marcarComunicadoLeido } from '../../api/intranet'
import { useAuth } from '../../auth/useAuth'
import { CredixPage } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { esCreditoAdministrador, esCreditoEncargado } from '../../utils/creditoOperacionPermisos'
import { AdjuntoVista } from './AdjuntoVista'

type Filtro = 'todos' | 'nuevos' | 'permanentes'

export function ComunicadosPage() {
  const { session } = useAuth()
  const qc = useQueryClient()
  const puedeEditar =
    esCreditoAdministrador(session?.roles ?? []) || esCreditoEncargado(session?.roles ?? [])
  const [abierto, setAbierto] = useState<number | null>(null)
  const [filtro, setFiltro] = useState<Filtro>('todos')

  const lista = useQuery({
    queryKey: ['comunicados'],
    queryFn: () => fetchComunicados(false),
  })

  const detalle = useQuery({
    queryKey: ['comunicado', abierto],
    queryFn: () => fetchComunicado(abierto!),
    enabled: abierto != null,
  })

  const leido = useMutation({
    mutationFn: marcarComunicadoLeido,
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
  })

  const filas = (lista.data ?? []).filter((c) => {
    if (filtro === 'nuevos') return !c.leido
    if (filtro === 'permanentes') return c.permanente
    return true
  })

  return (
    <CredixPage
      title="Avisos"
      subtitle="Lo que gerencia quiere que vea toda la sede: noticias, horarios y reglamentos."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Avisos' },
      ]}
      actions={
        puedeEditar ? <Link to="/admin/comunicados">Publicar aviso</Link> : undefined
      }
    >
      <Segmented<Filtro>
        value={filtro}
        onChange={setFiltro}
        options={[
          { label: 'Todos', value: 'todos' },
          { label: 'Nuevos', value: 'nuevos' },
          { label: 'Siempre visibles', value: 'permanentes' },
        ]}
        block
        style={{ marginBottom: 12, maxWidth: 480 }}
      />

      {lista.isLoading ? (
        <Spin />
      ) : filas.length === 0 ? (
        <Empty description={filtro === 'nuevos' ? 'No hay avisos nuevos' : 'No hay avisos vigentes'} />
      ) : (
        <ul className="credix-comunicado-list">
          {filas.map((c) => (
            <li key={c.comunicadoId}>
              <button
                type="button"
                className={`credix-comunicado-card ${c.leido ? 'is-read' : ''}`}
                onClick={() => {
                  setAbierto(c.comunicadoId)
                  if (!c.leido) void leido.mutate(c.comunicadoId)
                }}
              >
                <strong>{c.titulo}</strong>
                <span className="credix-comunicado-card__meta">
                  <span>{c.permanente ? 'Siempre visible' : `Hasta ${formatFecha(c.fechaFin)}`}</span>
                  {c.alcance === 'RED' ? <span>Toda la red</span> : <span>Esta sede</span>}
                  {c.adjuntos > 0 ? <span>{c.adjuntos} archivo{c.adjuntos === 1 ? '' : 's'}</span> : null}
                  {c.leido ? null : <span>Nuevo</span>}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}

      {abierto != null && detalle.isLoading ? <Spin style={{ marginTop: 16 }} /> : null}

      {detalle.data ? (
        <article className="credix-comunicado-detalle">
          <h2>{detalle.data.cabecera.titulo}</h2>
          <p>{detalle.data.cabecera.cuerpo}</p>
          {detalle.data.adjuntos.length > 0 ? (
            <ul className="credix-comunicado-adjuntos">
              {detalle.data.adjuntos.map((a) => (
                <AdjuntoVista key={a.adjuntoId} adjunto={a} />
              ))}
            </ul>
          ) : null}
        </article>
      ) : null}
    </CredixPage>
  )
}
