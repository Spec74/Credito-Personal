/** Un solo algoritmo de céntimos para UI (evita drift roundMoney vs sumMoney). */
function toCents(value: number): number {
  return Math.round(Number(value) * 100)
}

/** Redondeo a céntimos (evita 43.649999… → 43.65 en UI). */
export function roundMoney(value: number): number {
  return toCents(value) / 100
}

/** Suma en céntimos enteros y vuelve a soles (sin deriva float). */
export function sumMoney(values: Iterable<number>): number {
  let cents = 0
  for (const v of values) {
    cents += toCents(v)
  }
  return cents / 100
}

/** Igualdad monetaria exacta al céntimo (sin float). */
export function moneyEquals(a: number, b: number): boolean {
  return toCents(a) === toCents(b)
}

/** a &gt; b al céntimo. */
export function moneyGreater(a: number, b: number): boolean {
  return toCents(a) > toCents(b)
}
