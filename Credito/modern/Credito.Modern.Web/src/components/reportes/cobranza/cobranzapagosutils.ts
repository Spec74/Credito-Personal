export type PagoBadge = {
  monto: string
  fecha: string
  impago: boolean
  index: number
}

export function parsePagoBadge(pago: string, index: number): PagoBadge {
  const trimmed = pago.trim()
  let monto = trimmed
  let fecha = ''
  const match = trimmed.match(/\(([^)]+)\)/)
  if (match) {
    fecha = match[1]
    monto = trimmed.replace(match[0], '').trim()
  }
  const impago = monto === '0' || monto === '0.00' || monto.startsWith('0.00')
  return { monto, fecha, impago, index: index + 1 }
}

export function countPagosLista(pagosLista: string[]) {
  let pagos = 0
  let impagos = 0
  for (let i = 0; i < pagosLista.length; i++) {
    if (parsePagoBadge(pagosLista[i], i).impago) impagos++
    else pagos++
  }
  return { pagos, impagos, total: pagosLista.length }
}
