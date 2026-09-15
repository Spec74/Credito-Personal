import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import {
  readTableHorizontalOverflow,
  useTableHorizontalWheel,
} from '../../hooks/useTableHorizontalWheel'

type Props = {
  children: ReactNode
  className?: string
}

/**
 * Contenedor para tablas anchas de consulta: la rueda del mouse desplaza las columnas y un
 * degradado avisa que hay más contenido a los lados. La tabla no es interactiva, así que no
 * se pierde ningún gesto de fila.
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
    const body =
      root.querySelector<HTMLElement>('.ant-table-body') ??
      root.querySelector<HTMLElement>('.ant-table-content')
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
    </div>
  )
}
