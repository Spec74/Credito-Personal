import { roundMoney } from './money'

export type ResumenCuentaItem = {
  cuenta: string
  importe: number
}

/**
 * Interpreta el texto de `dbo.ufnResumenCuentaCajaDiario`, que llega como
 * `"EFECTIVO = 1065.00  YAPE = 500.00"` (`STRING_AGG` con doble espacio de separador).
 *
 * Devuelve lista vacía si el formato no es el esperado, para que el llamador muestre el texto
 * crudo en lugar de inventar datos.
 */
export function parseResumenCuentaCaja(resumen?: string | null): ResumenCuentaItem[] {
  const texto = resumen?.trim()
  if (!texto) {
    return []
  }

  const items: ResumenCuentaItem[] = []
  for (const parte of texto.split(/\s{2,}/)) {
    const match = /^(.+?)\s*=\s*(-?\d+(?:[.,]\d+)?)$/.exec(parte.trim())
    if (!match) {
      return []
    }
    items.push({
      cuenta: match[1].trim(),
      importe: roundMoney(Number(match[2].replace(',', '.'))),
    })
  }
  return items
}

export function esCuentaEfectivo(cuenta: string): boolean {
  const k = cuenta.trim().toUpperCase()
  return k === 'EFECTIVO' || k === 'CASH' || k.includes('EFECTIVO')
}

/** Efectivo tipado en el resumen de caja (para conteo de billetes). */
export function efectivoDesdeResumenCuenta(resumen?: string | null): number {
  return roundMoney(
    parseResumenCuentaCaja(resumen)
      .filter((i) => esCuentaEfectivo(i.cuenta))
      .reduce((acc, i) => acc + i.importe, 0),
  )
}
