/**
 * EMVCo MPM (Merchant-Presented) — helpers para QR BCRP / Estándar QR Perú.
 * No inventa merchant account: solo muta un payload oficial (tags 01/54/63).
 */

export type EmvTlv = { id: string; value: string }

export type EmvAmountResult =
  | { ok: true; payload: string; amountText: string; merchantName: string | null }
  | { ok: false; error: string; payload: string }

/** CRC-16/CCITT-FALSE (poly 0x1021, init 0xFFFF). Vector: "123456789" → 29B1. */
export function crc16CcittFalse(data: string): string {
  let crc = 0xffff
  for (let i = 0; i < data.length; i++) {
    crc ^= data.charCodeAt(i) << 8
    for (let bit = 0; bit < 8; bit++) {
      crc = (crc & 0x8000) !== 0 ? ((crc << 1) ^ 0x1021) & 0xffff : (crc << 1) & 0xffff
    }
  }
  return crc.toString(16).toUpperCase().padStart(4, '0')
}

export function parseEmvTlv(payload: string): EmvTlv[] {
  const raw = payload.trim()
  if (!raw) throw new Error('Payload EMV vacío.')
  const out: EmvTlv[] = []
  let i = 0
  while (i + 4 <= raw.length) {
    const id = raw.slice(i, i + 2)
    if (!/^\d{2}$/.test(id)) {
      throw new Error(`Tag EMV inválido en posición ${i}.`)
    }
    const lenStr = raw.slice(i + 2, i + 4)
    if (!/^\d{2}$/.test(lenStr)) {
      throw new Error(`Longitud EMV inválida en tag ${id}.`)
    }
    const len = Number(lenStr)
    i += 4
    if (i + len > raw.length) {
      throw new Error(`Payload EMV truncado en tag ${id}.`)
    }
    out.push({ id, value: raw.slice(i, i + len) })
    i += len
  }
  if (i !== raw.length) {
    throw new Error('Payload EMV con bytes sobrantes.')
  }
  return out
}

export function encodeEmvTlv(tags: EmvTlv[]): string {
  return tags
    .map((t) => {
      if (!/^\d{2}$/.test(t.id)) {
        throw new Error(`Tag EMV inválido: ${t.id}`)
      }
      if (t.value.length > 99) {
        throw new Error(`Valor del tag ${t.id} supera 99 caracteres.`)
      }
      return `${t.id}${String(t.value.length).padStart(2, '0')}${t.value}`
    })
    .join('')
}

/** Monto PEN para tag 54: dígitos + un punto decimal (2 decimales). */
export function formatEmvAmountPen(amount: number): string | null {
  if (!Number.isFinite(amount) || amount <= 0) return null
  const rounded = Math.round(amount * 100) / 100
  if (rounded <= 0 || rounded > 9_999_999_999.99) return null
  return rounded.toFixed(2)
}

export function readEmvTag(tags: EmvTlv[], id: string): string | null {
  return tags.find((t) => t.id === id)?.value ?? null
}

/**
 * Verifica CRC del payload (debe terminar en 6304XXXX).
 */
export function verifyEmvCrc(payload: string): boolean {
  try {
    const tags = parseEmvTlv(payload)
    if (tags.length < 1 || tags[tags.length - 1]?.id !== '63') return false
    const withoutCrc = encodeEmvTlv(tags.slice(0, -1))
    const expected = crc16CcittFalse(`${withoutCrc}6304`)
    return tags[tags.length - 1]!.value.toUpperCase() === expected
  } catch {
    return false
  }
}

function upsertTag(tags: EmvTlv[], id: string, value: string, afterId?: string): EmvTlv[] {
  const withoutCrc = tags.filter((t) => t.id !== '63')
  const idx = withoutCrc.findIndex((t) => t.id === id)
  if (idx >= 0) {
    return withoutCrc.map((t, i) => (i === idx ? { id, value } : t))
  }
  if (afterId) {
    const afterIdx = withoutCrc.findIndex((t) => t.id === afterId)
    if (afterIdx >= 0) {
      const copy = [...withoutCrc]
      copy.splice(afterIdx + 1, 0, { id, value })
      return copy
    }
  }
  return [...withoutCrc, { id, value }]
}

/**
 * Partiendo de un QR oficial (estático o dinámico), genera payload con monto
 * (tag 54), marca dinámico (01=12) y CRC 63 recalculado.
 * No altera tag 59 (nombre del titular del QR).
 */
export function withEmvTransactionAmount(
  basePayload: string | null | undefined,
  amount: number,
): EmvAmountResult {
  const raw = (basePayload ?? '').trim().replace(/^generate:/i, '')
  if (!raw) {
    return { ok: false, error: 'Sin payload EMV configurado.', payload: '' }
  }
  if (raw.length > 4096) {
    return { ok: false, error: 'Payload EMV demasiado largo.', payload: raw }
  }

  const amountText = formatEmvAmountPen(amount)
  if (!amountText) {
    return {
      ok: false,
      error: 'Indique un monto mayor a cero para el QR dinámico.',
      payload: raw,
    }
  }

  try {
    let tags = parseEmvTlv(raw)
    if (!tags.some((t) => t.id === '00')) {
      return { ok: false, error: 'Payload EMV inválido (falta tag 00).', payload: raw }
    }

    // Opcional: si el base trae CRC inválido, igual intentamos mutar (algunos paste
    // vienen sin CRLF limpios). Si parse ok, seguimos.
    tags = upsertTag(tags, '01', '12', '00')
    tags = upsertTag(tags, '54', amountText, '53')

    // Currency PEN si falta (604) — no forzar si ya existe otra (no debería).
    if (!tags.some((t) => t.id === '53')) {
      tags = upsertTag(tags, '53', '604', '52')
    }

    const body = encodeEmvTlv(tags.filter((t) => t.id !== '63'))
    const crc = crc16CcittFalse(`${body}6304`)
    const payload = `${body}6304${crc}`
    const merchantName = readEmvTag(parseEmvTlv(payload), '59')

    return {
      ok: true,
      payload,
      amountText,
      merchantName: merchantName?.trim() || null,
    }
  } catch (e) {
    return {
      ok: false,
      error: e instanceof Error ? e.message : 'No se pudo armar el QR dinámico.',
      payload: raw,
    }
  }
}
