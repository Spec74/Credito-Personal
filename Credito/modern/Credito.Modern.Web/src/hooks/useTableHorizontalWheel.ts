import { useEffect, type RefObject } from 'react'

function tableScrollBody(root: HTMLElement): HTMLElement | null {
  return (
    root.querySelector<HTMLElement>('.ant-table-body') ??
    root.querySelector<HTMLElement>('.ant-table-content')
  )
}

/**
 * Convierte la rueda del mouse en desplazamiento horizontal sobre el cuerpo de la tabla.
 *
 * En tablas anchas y no interactivas (Saldos) el usuario no debería tener que bajar hasta la
 * barra para ver las últimas columnas. Si el gesto ya es horizontal (trackpad) se respeta;
 * si la tabla no puede desplazarse más en esa dirección, no se consume el evento y la página
 * sigue pudiendo hacer scroll.
 */
export function useTableHorizontalWheel(rootRef: RefObject<HTMLElement | null>): void {
  useEffect(() => {
    const root = rootRef.current
    if (!root) {
      return
    }

    const onWheel = (event: WheelEvent) => {
      const body = tableScrollBody(root)
      if (!body) {
        return
      }

      const max = body.scrollWidth - body.clientWidth
      if (max <= 1) {
        return
      }

      const delta =
        Math.abs(event.deltaX) > Math.abs(event.deltaY) ? event.deltaX : event.deltaY
      if (delta === 0) {
        return
      }

      const next = Math.max(0, Math.min(max, body.scrollLeft + delta))
      if (next === body.scrollLeft) {
        return
      }

      event.preventDefault()
      body.scrollLeft = next
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
