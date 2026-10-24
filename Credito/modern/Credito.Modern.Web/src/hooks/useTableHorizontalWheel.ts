import { useEffect, type RefObject } from 'react'

export function tableScrollBody(root: HTMLElement): HTMLElement | null {
  return (
    root.querySelector<HTMLElement>('.ant-table-body') ??
    root.querySelector<HTMLElement>('.ant-table-content')
  )
}

/**
 * Solo desplaza columnas cuando el gesto ya es horizontal (trackpad) o Mayús + rueda.
 * La rueda vertical no se consume: la página sigue bajando. Excel / Sheets / GitHub.
 */
export function resolveHorizontalWheelDelta(event: {
  deltaX: number
  deltaY: number
  shiftKey: boolean
}): number | null {
  if (Math.abs(event.deltaX) > Math.abs(event.deltaY)) {
    return event.deltaX === 0 ? null : event.deltaX
  }
  if (event.shiftKey && event.deltaY !== 0) {
    return event.deltaY
  }
  return null
}

export function scrollTableHorizontally(root: HTMLElement, delta: number): boolean {
  const body = tableScrollBody(root)
  if (!body) {
    return false
  }
  const max = body.scrollWidth - body.clientWidth
  if (max <= 1 || delta === 0) {
    return false
  }
  const next = Math.max(0, Math.min(max, body.scrollLeft + delta))
  if (next === body.scrollLeft) {
    return false
  }
  body.scrollLeft = next
  return true
}

export function shouldForwardVerticalWheel(scrollHeight: number, clientHeight: number): boolean {
  return scrollHeight <= clientHeight + 1
}

export function tableHasVerticalOverflow(root: HTMLElement): boolean {
  const body = tableScrollBody(root)
  if (!body) {
    return false
  }
  return !shouldForwardVerticalWheel(body.scrollHeight, body.clientHeight)
}

export function findVerticalScrollParent(start: HTMLElement): HTMLElement | Window {
  let el: HTMLElement | null = start.parentElement
  while (el) {
    const overflowY = getComputedStyle(el).overflowY
    if (
      (overflowY === 'auto' || overflowY === 'scroll') &&
      el.scrollHeight > el.clientHeight + 1
    ) {
      return el
    }
    el = el.parentElement
  }
  return window
}

export function applyVerticalWheel(target: HTMLElement | Window, deltaY: number): boolean {
  if (deltaY === 0) {
    return false
  }
  if (target === window) {
    const max = document.documentElement.scrollHeight - window.innerHeight
    if (max <= 1) {
      return false
    }
    window.scrollBy(0, deltaY)
    return true
  }
  const node = target as HTMLElement
  const max = node.scrollHeight - node.clientHeight
  const next = Math.max(0, Math.min(max, node.scrollTop + deltaY))
  if (next === node.scrollTop) {
    return false
  }
  node.scrollTop = next
  return true
}

export function forwardVerticalWheel(start: HTMLElement, deltaY: number): boolean {
  return applyVerticalWheel(findVerticalScrollParent(start), deltaY)
}

export function useTableHorizontalWheel(rootRef: RefObject<HTMLElement | null>): void {
  useEffect(() => {
    const root = rootRef.current
    if (!root) {
      return
    }

    const verticalTarget = findVerticalScrollParent(root)

    const onWheel = (event: WheelEvent) => {
      const horizontal = resolveHorizontalWheelDelta(event)
      if (horizontal != null) {
        if (scrollTableHorizontally(root, horizontal)) {
          event.preventDefault()
        }
        return
      }
      if (event.deltaY === 0 || tableHasVerticalOverflow(root)) {
        return
      }
      if (applyVerticalWheel(verticalTarget, event.deltaY)) {
        event.preventDefault()
      }
    }

    root.addEventListener('wheel', onWheel, { passive: false })
    return () => root.removeEventListener('wheel', onWheel)
  }, [rootRef])
}

/**
 * Centro vertical de las flechas: el tramo de la tabla que se ve en pantalla,
 * no el 50 % de toda la grilla. Así suben y bajan al hacer scroll.
 */
export function visibleBandMidOffset(args: {
  bandTop: number
  bandHeight: number
  viewTop: number
  viewBottom: number
  buttonSize: number
  padding?: number
}): number | null {
  const { bandTop, bandHeight, viewTop, viewBottom, buttonSize, padding = 8 } = args
  if (bandHeight <= 0) {
    return null
  }
  const sliceTop = Math.max(bandTop, viewTop)
  const sliceBottom = Math.min(bandTop + bandHeight, viewBottom)
  if (sliceBottom - sliceTop < buttonSize + padding * 2) {
    return null
  }
  const mid = (sliceTop + sliceBottom) / 2
  const raw = mid - bandTop - buttonSize / 2
  const min = padding
  const max = Math.max(padding, bandHeight - buttonSize - padding)
  return Math.max(min, Math.min(max, raw))
}

export function horizontalOverflowState(
  scrollLeft: number,
  scrollWidth: number,
  clientWidth: number,
): { canScrollLeft: boolean; canScrollRight: boolean } {
  const max = scrollWidth - clientWidth
  if (max <= 1) {
    return { canScrollLeft: false, canScrollRight: false }
  }
  return {
    canScrollLeft: scrollLeft > 2,
    canScrollRight: scrollLeft < max - 2,
  }
}

export function readTableHorizontalOverflow(root: HTMLElement): {
  canScrollLeft: boolean
  canScrollRight: boolean
} {
  const body = tableScrollBody(root)
  if (!body) {
    return { canScrollLeft: false, canScrollRight: false }
  }
  return horizontalOverflowState(body.scrollLeft, body.scrollWidth, body.clientWidth)
}
