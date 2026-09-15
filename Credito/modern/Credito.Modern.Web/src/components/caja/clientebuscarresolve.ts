import type { ClienteBuscarItem } from '../../types/api'

export type ClienteHit = Pick<ClienteBuscarItem, 'personaId' | 'label'>

const norm = (s: string) => s.trim().toLowerCase()

/** Extrae código/DNI entre corchetes en etiquetas legacy: "Nombre [12345678]". */
export function codigoEnLabel(label: string): string | null {
  const m = label.match(/\[([^\]]+)\]\s*$/)
  return m ? m[1].trim() : null
}

/**
 * Términos útiles para la API (LIKE por campo suelto).
 * La etiqueta completa "44684156 NOMBRE [CC040]" no coincide en SQL; usamos DNI, código, etc.
 */
export function extractSearchTermsFromInput(term: string): string[] {
  const t = term.trim()
  if (!t) {
    return []
  }

  const out: string[] = []

  const push = (s: string) => {
    const x = s.trim()
    if (x.length >= 2 && !out.some((o) => norm(o) === norm(x))) {
      out.push(x)
    }
  }

  push(t)

  const dniLead = t.match(/^(\d{6,12})\b/)
  if (dniLead) {
    push(dniLead[1])
  }

  const bracket = t.match(/\[([^\]]+)\]/)
  if (bracket) {
    push(bracket[1])
  }

  const code = codigoEnLabel(t)
  if (code) {
    push(code)
  }

  const first = t.split(/\s+/)[0]
  if (/^\d{6,12}$/.test(first)) {
    push(first)
  }

  const namePart = t
    .replace(/^\d+\s*/, '')
    .replace(/\s*\[[^\]]+\]\s*$/, '')
    .trim()
  if (namePart.length >= 3 && namePart !== t) {
    push(namePart)
  }

  return out
}

/**
 * Un solo término para APIs de listado (un LIKE).
 * Preferencia: DNI/código → siguiente token útil → texto completo.
 */
export function primaryCatalogSearchTerm(term: string): string {
  const terms = extractSearchTermsFromInput(term)
  if (terms.length === 0) {
    return ''
  }
  const digits = terms.find((t) => /^\d{6,12}$/.test(t))
  if (digits) {
    return digits
  }
  if (terms.length > 1) {
    return terms[1]
  }
  return terms[0]
}

export function findHitInList(
  term: string,
  hits: ClienteHit[],
): ClienteHit | null {
  const q = norm(term)
  if (!q) {
    return null
  }
  return (
    hits.find((h) => norm(h.label) === q) ??
    hits.find((h) => {
      const code = codigoEnLabel(h.label)
      return code != null && norm(code) === q
    }) ??
    null
  )
}

/**
 * Elige el cliente más probable para confirmar con Enter.
 * - 1 resultado → ese
 * - etiqueta o código exacto → ese
 * - varios sin desempate claro → null (el usuario elige en la lista)
 */
export function pickBestClienteMatch(
  term: string,
  hits: ClienteHit[],
): ClienteHit | null {
  if (hits.length === 0) {
    return null
  }
  if (hits.length === 1) {
    return hits[0]
  }

  const q = norm(term)
  if (!q) {
    return null
  }

  const exactLabel = hits.find((h) => norm(h.label) === q)
  if (exactLabel) {
    return exactLabel
  }

  const exactCode = hits.find((h) => {
    const code = codigoEnLabel(h.label)
    return code != null && norm(code) === q
  })
  if (exactCode) {
    return exactCode
  }

  const byPrefix = hits.filter((h) => norm(h.label).startsWith(q))
  if (byPrefix.length === 1) {
    return byPrefix[0]
  }

  const byCodePrefix = hits.filter((h) => {
    const code = codigoEnLabel(h.label)
    return code != null && norm(code).startsWith(q)
  })
  if (byCodePrefix.length === 1) {
    return byCodePrefix[0]
  }

  return null
}
