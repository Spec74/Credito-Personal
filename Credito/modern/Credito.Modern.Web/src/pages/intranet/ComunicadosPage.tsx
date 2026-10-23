import { useMemo } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Empty, Input, Segmented, Spin } from 'antd'
import { ArrowLeftOutlined } from '@ant-design/icons'
import { fetchComunicado, fetchComunicados, marcarComunicadoLeido } from '../../api/intranet'
import { useAuth } from '../../auth/useAuth'
import { CredixPage } from '../../components/credix'
import { formatRelativo } from '../../utils/formatFecha'
import { puedePublicarAvisos } from '../../utils/creditoOperacionPermisos'
import { AdjuntoVista } from './AdjuntoVista'
import { destinoAviso, previewTexto, vigenciaAviso } from './intranetLabels'

type Filtro = 'todos' | 'nuevos' | 'permanentes'

export function ComunicadosPage() {
  const { session } = useAuth()
  const qc = useQueryClient()
  const [params, setParams] = useSearchParams()
  const puedeEditar = puedePublicarAvisos(session?.roles ?? [])
  const abierto = Number(params.get('id')) || null
  const filtro = (params.get('f') as Filtro) || 'todos'
  const q = params.get('q') ?? ''

  const setAbierto = (id: number | null) => {
    const next = new URLSearchParams(params)
    if (id == null) next.delete('id')
    else next.set('id', String(id))
    setParams(next, { replace: true })
  }

  const setFiltro = (f: Filtro) => {
    const next = new URLSearchParams(params)
    if (f === 'todos') next.delete('f')
    else next.set('f', f)
    setParams(next, { replace: true })
  }

  const setQ = (value: string) => {
    const next = new URLSearchParams(params)
    if (!value.trim()) next.delete('q')
    else next.set('q', value)
    setParams(next, { replace: true })
  }

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

  const vigentes = lista.data ?? []
  const nNuevos = vigentes.filter((c) => !c.leido).length
  const nPerm = vigentes.filter((c) => c.permanente).length

  const filas = useMemo(() => {
    const term = q.trim().toLowerCase()
    return vigentes.filter((c) => {
      if (filtro === 'nuevos' && c.leido) return false
      if (filtro === 'permanentes' && !c.permanente) return false
      if (!term) return true
      return `${c.titulo} ${c.cuerpo} ${c.oficinaNombre ?? ''}`.toLowerCase().includes(term)
    })
  }, [vigentes, filtro, q])

  const abrir = (id: number, yaLeido: boolean) => {
    setAbierto(id)
    if (!yaLeido) void leido.mutate(id)
  }

  return (
    <CredixPage
      title="Avisos"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Avisos' },
      ]}
      actions={
        puedeEditar ? (
          <Link to="/admin/comunicados">
            <Button type="primary">Publicar</Button>
          </Link>
        ) : undefined
      }
      stats={[
        { value: nNuevos, label: 'Nuevos', tone: nNuevos > 0 ? 'red' : 'default' },
        { value: vigentes.length, label: 'Vigentes' },
        { value: nPerm, label: 'Siempre visibles' },
      ]}
    >
      <div className="credix-inbox-toolbar">
        <Input
          allowClear
          placeholder="Buscar aviso"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          aria-label="Buscar aviso"
        />
        <Segmented<Filtro>
          value={filtro}
          onChange={setFiltro}
          options={[
            { label: 'Todos', value: 'todos' },
            { label: `Nuevos${nNuevos ? ` ${nNuevos}` : ''}`, value: 'nuevos' },
            { label: 'Siempre', value: 'permanentes' },
          ]}
        />
      </div>

      <div className={`credix-inbox${abierto != null ? ' is-reading' : ''}`}>
        <div className="credix-inbox__list">
          {lista.isLoading ? (
            <div className="credix-inbox__empty">
              <Spin />
            </div>
          ) : filas.length === 0 ? (
            <div className="credix-inbox__empty">
              <Empty
                description={
                  q
                    ? 'Ningún aviso coincide'
                    : filtro === 'nuevos'
                      ? 'No hay avisos nuevos'
                      : 'No hay avisos vigentes'
                }
              />
            </div>
          ) : (
            <ul className="credix-inbox__scroll">
              {filas.map((c) => (
                <li key={c.comunicadoId}>
                  <button
                    type="button"
                    className={`credix-inbox-row${c.leido ? ' is-read' : ''}${
                      abierto === c.comunicadoId ? ' is-active' : ''
                    }`}
                    onClick={() => abrir(c.comunicadoId, c.leido)}
                  >
                    <i className="credix-inbox-row__mark" aria-hidden />
                    <div>
                      <strong>{c.titulo}</strong>
                      <p className="credix-inbox-row__preview">{previewTexto(c.cuerpo)}</p>
                      <div className="credix-chip-row">
                        <span className={`credix-chip${c.permanente ? ' credix-chip--brand' : ''}`}>
                          {vigenciaAviso(c)}
                        </span>
                        <span className="credix-chip">{destinoAviso(c)}</span>
                        {c.adjuntos > 0 ? (
                          <span className="credix-chip">
                            {c.adjuntos} archivo{c.adjuntos === 1 ? '' : 's'}
                          </span>
                        ) : null}
                      </div>
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="credix-inbox__detail">
          {abierto != null ? (
            <Button
              type="link"
              className="credix-aviso-lectura__back"
              icon={<ArrowLeftOutlined />}
              onClick={() => setAbierto(null)}
            >
              Volver a la lista
            </Button>
          ) : null}

          {abierto == null ? (
            <div className="credix-inbox__empty">
              <Empty description="Elija un aviso para leerlo" />
            </div>
          ) : detalle.isLoading ? (
            <div className="credix-inbox__empty">
              <Spin />
            </div>
          ) : detalle.data ? (
            <article className="credix-aviso-lectura">
              <h2>{detalle.data.cabecera.titulo}</h2>
              <p className="credix-aviso-lectura__meta">
                <span>{detalle.data.cabecera.nombreUsuario?.trim() || 'Gerencia'}</span>
                <span>·</span>
                <span>{formatRelativo(detalle.data.cabecera.fechaReg)}</span>
                <span>·</span>
                <span>{destinoAviso(detalle.data.cabecera)}</span>
                <span>·</span>
                <span>{vigenciaAviso(detalle.data.cabecera)}</span>
              </p>
              <p className="credix-aviso-lectura__cuerpo">{detalle.data.cabecera.cuerpo}</p>
              {detalle.data.adjuntos.length > 0 ? (
                <ul className="credix-comunicado-adjuntos">
                  {detalle.data.adjuntos.map((a) => (
                    <AdjuntoVista key={a.adjuntoId} adjunto={a} />
                  ))}
                </ul>
              ) : null}
            </article>
          ) : (
            <Empty description="No se pudo abrir el aviso" />
          )}
        </div>
      </div>
    </CredixPage>
  )
}
