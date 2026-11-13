/** Geometría compartida de gráficos del tablero (ejes “nice”, ticks X). */

export function formatAxisMoney(value: number): string {
  if (value >= 1000) {
    return `S/ ${(value / 1000).toFixed(value >= 10000 ? 0 : 1)}k`
  }
  return `S/ ${value.toFixed(0)}`
}

export function niceMax(max: number): number {
  if (max <= 0) return 1
  const exp = Math.floor(Math.log10(max))
  const base = 10 ** exp
  const n = max / base
  const nice = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10
  return nice * base
}

export function uniqueTicks(length: number): number[] {
  if (length <= 0) return []
  if (length <= 6) {
    return Array.from({ length }, (_, i) => i)
  }
  const last = length - 1
  const step = Math.ceil(last / 5)
  const ticks = [0]
  for (let i = step; i < last; i += step) {
    ticks.push(i)
  }
  if (ticks[ticks.length - 1] !== last) {
    ticks.push(last)
  }
  return ticks
}

/** Colores de decisión (Credix navy + semáforo corporativo; sin púrpura genérica). */
export const DASH_CHART_COLORS = {
  cobrado: 'var(--credix-ink-brand, #2e69ae)',
  cobradoFill: 'var(--credix-ink-brand, #2e69ae)',
  desembolso: 'var(--credix-info, #0e7490)',
  desembolsoFill: 'var(--credix-info, #0e7490)',
  promedio: 'var(--credix-text-muted, #64748b)',
  sinMora: 'var(--credix-success, #15803d)',
  conMora: '#9f1239',
  vencido: 'var(--credix-danger, #b91c1c)',
} as const
