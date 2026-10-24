import { useCallback, useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react'
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
  const top = header?.getBoundingClientRect().bottom ?? 0
  return { top: top + 8, bottom: window.innerHeight - 16 }
}

function findScrollParents(el: HTMLElement): Array<HTMLElement | Window> {
  const out: Array<HTMLElement | Window> = [window]
  let p = el.parentElement
  while (p) {
    const style = getComputedStyle(p)
    if (/(auto|scroll|overlay)/.test(style.overflowY) || /(auto|scroll|overlay)/.test(style.overflowX)) {
      out.push(p)
    }
    p = p.parentElement
  }
  return out
}

function placeNudge(
  button: HTMLButtonElement | null,
  top: number | null,
  side: 'start' | 'end',
  wrap: DOMRect,
) {
  if (!button) {
    return
  }
  if (top == null) {
    button.style.visibility = 'hidden'
    return
  }
  button.style.visibility = 'visible'
  button.style.top = `${Math.round(top)}px`
  if (side === 'start') {
    button.style.left = `${Math.round(wrap.left + 8)}px`
    button.style.right = 'auto'
  } else {
    button.style.left = 'auto'
    button.style.right = `${Math.round(window.innerWidth - wrap.right + 8)}px`
  }
}

/**
 * Tablas anchas: rueda = página (nativo); Mayús+rueda = columnas.
 * Flechas fijas al tramo visible, sin re-render en cada scroll.
 */
export function CredixWideTable({ children, className }: Props) {
  const rootRef = useRef<HTMLDivElement>(null)
  const startRef = useRef<HTMLButtonElement>(null)
  const endRef = useRef<HTMLButtonElement>(null)
  const [edges, setEdges] = useState({ canScrollLeft: false, canScrollRight: false })

  useTableHorizontalWheel(rootRef)

  const syncEdges = useCallback(() => {
    const root = rootRef.current
    if (!root) {
      return
    }
    setEdges(readTableHorizontalOverflow(root))
  }, [])

  const syncNudgePlace = useCallback(() => {
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
    const top = inBand == null ? null : band.top + inBand
    placeNudge(startRef.current, top, 'start', wrap)
    placeNudge(endRef.current, top, 'end', wrap)
  }, [])

  useEffect(() => {
    const root = rootRef.current
    if (!root) {
      return
    }

    syncEdges()
    syncNudgePlace()

    let raf = 0
    const onMove = () => {
      if (raf) {
        return
      }
      raf = window.requestAnimationFrame(() => {
        raf = 0
        syncNudgePlace()
      })
    }
    const onTableScroll = () => {
      syncEdges()
      onMove()
    }

    const body = tableScrollBody(root)
    body?.addEventListener('scroll', onTableScroll, { passive: true })
    const parents = findScrollParents(root)
    for (const node of parents) {
      node.addEventListener('scroll', onMove, { passive: true })
    }
    window.addEventListener('resize', onMove)
    const resize = new ResizeObserver(onMove)
    resize.observe(root)
    if (body) {
      resize.observe(body)
    }

    return () => {
      body?.removeEventListener('scroll', onTableScroll)
      for (const node of parents) {
        node.removeEventListener('scroll', onMove)
      }
      window.removeEventListener('resize', onMove)
      resize.disconnect()
      if (raf) {
        window.cancelAnimationFrame(raf)
      }
    }
  }, [syncEdges, syncNudgePlace])

  useLayoutEffect(() => {
    syncNudgePlace()
  }, [edges.canScrollLeft, edges.canScrollRight, syncNudgePlace])

  const nudge = (direction: -1 | 1) => {
    const root = rootRef.current
    if (!root) {
      return
    }
    const body = tableScrollBody(root)
    const step = body ? Math.min(280, Math.max(160, body.clientWidth * 0.4)) : 240
    scrollTableHorizontally(root, direction * step)
    syncEdges()
    syncNudgePlace()
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
          ref={startRef}
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
          ref={endRef}
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
