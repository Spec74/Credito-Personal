import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { LeftOutlined, RightOutlined } from '@ant-design/icons'
import {
  readTableHorizontalOverflow,
  scrollTableHorizontally,
  tableScrollBody,
  useTableHorizontalWheel,
  visibleBandMidOffset,
} from '../../hooks/useTableHorizontalWheel'

type Props = {
  children: ReactNode
  className?: string
}

const NUDGE_SIZE = 40

function tableVisualBand(root: HTMLElement): HTMLElement {
  return (
    root.querySelector<HTMLElement>('.ant-table-container') ??
    root.querySelector<HTMLElement>('.ant-table') ??
    root
  )
}

function readSafeViewport(): { top: number; bottom: number } {
  const header = document.querySelector('.credix-header')
  const context = document.querySelector('.credix-context-bar')
  const top = Math.max(
    header?.getBoundingClientRect().bottom ?? 0,
    context?.getBoundingClientRect().bottom ?? 0,
    0,
  )
  return { top: top + 8, bottom: window.innerHeight - 16 }
}

/**
 * Tablas anchas: rueda = página; Mayús+rueda o trackpad = columnas;
 * flechas en el tramo visible (suben y bajan con el scroll) y sombra si hay más.
 */
export function CredixWideTable({ children, className }: Props) {
  const rootRef = useRef<HTMLDivElement>(null)
  const [edges, setEdges] = useState({ canScrollLeft: false, canScrollRight: false })
  const [nudgeTop, setNudgeTop] = useState<number | null>(null)

  useTableHorizontalWheel(rootRef)

  const syncEdges = useCallback(() => {
    const root = rootRef.current
    if (!root) {
      return
    }
    setEdges(readTableHorizontalOverflow(root))
  }, [])

  const syncNudgeTop = useCallback(() => {
    const root = rootRef.current
    if (!root) {
      return
    }
    const band = tableVisualBand(root).getBoundingClientRect()
    const wrap = root.getBoundingClientRect()
    const view = readSafeViewport()
    const inBand = visibleBandMidOffset({
      bandTop: band.top,
      bandHeight: band.height,
      viewTop: view.top,
      viewBottom: view.bottom,
      buttonSize: NUDGE_SIZE,
    })
    setNudgeTop(inBand == null ? null : inBand + (band.top - wrap.top))
  }, [])

  useEffect(() => {
    const root = rootRef.current
    if (!root) {
      return
    }

    syncEdges()
    syncNudgeTop()
    const body = tableScrollBody(root)
    const onScroll = () => {
      syncEdges()
      syncNudgeTop()
    }
    body?.addEventListener('scroll', onScroll, { passive: true })
    document.addEventListener('scroll', onScroll, { passive: true, capture: true })
    window.addEventListener('resize', onScroll)
    const resize = new ResizeObserver(onScroll)
    resize.observe(root)
    if (body) {
      resize.observe(body)
    }

    return () => {
      body?.removeEventListener('scroll', onScroll)
      document.removeEventListener('scroll', onScroll, true)
      window.removeEventListener('resize', onScroll)
      resize.disconnect()
    }
  }, [syncEdges, syncNudgeTop])

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

  const showNudges = nudgeTop != null

  return (
    <div ref={rootRef} className={classes}>
      {children}
      {showNudges && edges.canScrollLeft ? (
        <button
          type="button"
          className="credix-wide-table__nudge credix-wide-table__nudge--start"
          style={{ top: nudgeTop }}
          aria-label="Ver columnas anteriores"
          onClick={() => nudge(-1)}
        >
          <LeftOutlined />
        </button>
      ) : null}
      {showNudges && edges.canScrollRight ? (
        <button
          type="button"
          className="credix-wide-table__nudge credix-wide-table__nudge--end"
          style={{ top: nudgeTop }}
          aria-label="Ver más columnas"
          onClick={() => nudge(1)}
        >
          <RightOutlined />
        </button>
      ) : null}
    </div>
  )
}
