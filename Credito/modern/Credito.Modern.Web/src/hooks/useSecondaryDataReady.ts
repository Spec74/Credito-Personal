import { useEffect, useState } from 'react'

/**
 * Activa consultas secundarias después del primer paint (y idle si existe),
 * para no competir con LCP/FCP ni saturar CORS preflight en la carga inicial.
 */
export function useSecondaryDataReady(enabled = true): boolean {
  const [ready, setReady] = useState(false)

  useEffect(() => {
    if (!enabled) {
      setReady(false)
      return
    }
    let cancelled = false
    let idleId: number | undefined
    let timeoutId: ReturnType<typeof setTimeout> | undefined

    const mark = () => {
      if (!cancelled) setReady(true)
    }

    const afterPaint = () => {
      if (typeof window !== 'undefined' && 'requestIdleCallback' in window) {
        idleId = window.requestIdleCallback(mark, { timeout: 1200 })
      } else {
        timeoutId = setTimeout(mark, 0)
      }
    }

    const raf = requestAnimationFrame(() => {
      requestAnimationFrame(afterPaint)
    })

    return () => {
      cancelled = true
      cancelAnimationFrame(raf)
      if (idleId != null && typeof window !== 'undefined' && 'cancelIdleCallback' in window) {
        window.cancelIdleCallback(idleId)
      }
      if (timeoutId != null) clearTimeout(timeoutId)
    }
  }, [enabled])

  return ready
}
