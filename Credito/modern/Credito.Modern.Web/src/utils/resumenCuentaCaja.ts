import { roundMoney, sumMoney } from './money'

export type ResumenCuentaItem = {
  cuenta: string
  importe: number
}

const PAIR_RE =
  /([A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑ0-9\s./-]*?)\s*=\s*(-?\d+(?:[.,]\d+)?)/gi

/**
 * Interpreta el texto de `dbo.ufnResumenCuentaCajaDiario` /
 * `usp_ResumenCuentaBoveda` (pares `CUENTA = importe`).
 * Paridad con `ResumenCuentaCajaParser` (C#): regex tolerante; pares rotos se omiten.
 */
export function parseResumenCuentaCaja(resumen?: string | null): ResumenCuentaItem[] {
  let texto = resumen?.trim() ?? ''
  if (!texto) {
    return []
  }

  texto = texto.replace(
    /^RESUMEN\s+(?:CAJA\s+DIARIO|BOVEDA|CUENTA)\s*:\s*/i,
    '',
  )

  const items: ResumenCuentaItem[] = []
  for (const match of texto.matchAll(PAIR_RE)) {
    const cuenta = match[1]?.trim() ?? ''
    const bruto = (match[2] ?? '').trim().replace(',', '.')
    if (!cuenta) continue
    const importe = Number(bruto)
    if (!Number.isFinite(importe)) continue
    items.push({ cuenta, importe: roundMoney(importe) })
  }
  return items
}

export function esCuentaEfectivo(cuenta: string): boolean {
  const k = cuenta.trim().toUpperCase()
  return k === 'EFECTIVO' || k === 'CASH' || k.includes('EFECTIVO')
}

/** Efectivo tipado en el resumen de caja (para conteo de billetes). */
export function efectivoDesdeResumenCuenta(resumen?: string | null): number {
  return sumMoney(
    parseResumenCuentaCaja(resumen)
      .filter((i) => esCuentaEfectivo(i.cuenta))
      .map((i) => i.importe),
  )
}

/** Suma de medios no efectivo (Yape, Interbank, BCP…). */
export function digitalesDesdeResumenCuenta(resumen?: string | null): number {
  return sumMoney(
    parseResumenCuentaCaja(resumen)
      .filter((i) => !esCuentaEfectivo(i.cuenta))
      .map((i) => i.importe),
  )
}
