/** Redondeo a céntimos (evita 43.649999… → 43.64 en UI). */
export function roundMoney(value: number): number {
  return Math.round((Number(value) + Number.EPSILON) * 100) / 100
}

/** Suma en céntimos enteros y vuelve a soles (sin deriva float). */
export function sumMoney(values: Iterable<number>): number {
  let cents = 0
  for (const v of values) {
    cents += Math.round(Number(v) * 100)
  }
  return cents / 100
}

/** Igualdad monetaria exacta al céntimo (sin float). */
export function moneyEquals(a: number, b: number): boolean {
  return Math.round(Number(a) * 100) === Math.round(Number(b) * 100)
}
