import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { LeftOutlined, RightOutlined } from '@ant-design/icons'
import {
  readTableHorizontalOverflow,
  scrollTableHorizontally,
  tableScrollBody,
  useTableHorizontalWheel,
} from '../../hooks/useTableHorizontalWheel'

type Props = {
  children: ReactNode
  className?: string
}

/**
 * Tablas anchas: rueda = página; Mayús+rueda o trackpad = columnas;
 * flechas y sombra si hay más a los lados. Analista/caja no se quedan atrapados.
 */
export function CredixWideTable({ children, className }: Props) {
  const rootRef = useRef<HTMLDivElement>(null)
  const [edges, setEdges] = useState({ canScrollLeft: false, canScrollRight: false })

  useTableHorizontalWheel(rootRef)

  const syncEdges = useCallback(() => {
    const root = rootRef.current
    if (!root) {
      return
    }
    setEdges(readTableHorizontalOverflow(root))
  }, [])

  useEffect(() => {
    const root = rootRef.current
    if (!root) {
      return
    }

    syncEdges()
    const body = tableScrollBody(root)
    body?.addEventListener('scroll', syncEdges, { passive: true })
    const resize = new ResizeObserver(syncEdges)
    resize.observe(root)
    if (body) {
      resize.observe(body)
    }

    return () => {
      body?.removeEventListener('scroll', syncEdges)
      resize.disconnect()
    }
  }, [syncEdges])

  const nudge = (direction: -1 | 1) => {
    const root = rootRef.current
    if (!root) {
      return
    }
    const body = tableScrollBody(root)
    const step = body ? Math.min(280, Math.max(160, body.clientWidth * 0.4)) : 240
    scrollTableHorizontally(root, direction * step)
    syncEdges()
  }

  const classes = [
    'credix-wide-table',
    edges.canScrollLeft ? 'credix-wide-table--start' : '',
    edges.canScrollRight ? 'credix-wide-table--end' : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div ref={rootRef} className={classes}>
      {children}
      {edges.canScrollLeft ? (
        <button
          type="button"
          className="credix-wide-table__nudge credix-wide-table__nudge--start"
          aria-label="Ver columnas anteriores"
          onClick={() => nudge(-1)}
        >
          <LeftOutlined />
        </button>
      ) : null}
      {edges.canScrollRight ? (
        <button
          type="button"
          className="credix-wide-table__nudge credix-wide-table__nudge--end"
          aria-label="Ver más columnas"
          onClick={() => nudge(1)}
        >
          <RightOutlined />
        </button>
      ) : null}
    </div>
  )
}
