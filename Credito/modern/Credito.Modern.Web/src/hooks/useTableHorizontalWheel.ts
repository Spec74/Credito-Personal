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

export function forwardVerticalWheel(start: HTMLElement, deltaY: number): boolean {
  if (deltaY === 0) {
    return false
  }
  let el: HTMLElement | null = start.parentElement
  while (el) {
    const style = getComputedStyle(el)
    const overflowY = style.overflowY
    const canScroll =
      (overflowY === 'auto' || overflowY === 'scroll') && el.scrollHeight > el.clientHeight + 1
    if (canScroll) {
      const max = el.scrollHeight - el.clientHeight
      const next = Math.max(0, Math.min(max, el.scrollTop + deltaY))
      if (next === el.scrollTop) {
        return false
      }
      el.scrollTop = next
      return true
    }
    el = el.parentElement
  }
  const max = document.documentElement.scrollHeight - window.innerHeight
  if (max <= 1) {
    return false
  }
  window.scrollBy(0, deltaY)
  return true
}

export function useTableHorizontalWheel(rootRef: RefObject<HTMLElement | null>): void {
  useEffect(() => {
    const root = rootRef.current
    if (!root) {
      return
    }

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
      if (forwardVerticalWheel(root, event.deltaY)) {
        event.preventDefault()
      }
    }

    root.addEventListener('wheel', onWheel, { passive: false })
    return () => root.removeEventListener('wheel', onWheel)
  }, [rootRef])
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
