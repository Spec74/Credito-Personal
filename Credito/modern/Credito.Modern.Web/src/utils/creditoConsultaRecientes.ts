const STORAGE_KEY = 'credix.credito.consulta.recientes'
const MAX_RECIENTES = 6

let memoria: CreditoConsultaReciente[] = []

export type CreditoConsultaReciente = {
  personaId: number
  label: string
  creditoId?: number | null
  at: number
}

function canUseStorage(): boolean {
  try {
    return typeof window !== 'undefined' && typeof window.sessionStorage !== 'undefined'
  } catch {
    return false
  }
}

function persist(rows: CreditoConsultaReciente[]): CreditoConsultaReciente[] {
  memoria = rows
  if (canUseStorage()) {
    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(rows))
  }
  return rows
}

export function leerCreditoConsultaRecientes(): CreditoConsultaReciente[] {
  if (!canUseStorage()) {
    return memoria
  }
  try {
    const raw = window.sessionStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return memoria
    }
    const parsed = JSON.parse(raw) as unknown
    if (!Array.isArray(parsed)) {
      return []
    }
    return parsed
      .filter(
        (row): row is CreditoConsultaReciente =>
          row != null &&
          typeof row === 'object' &&
          Number(row.personaId) > 0 &&
          typeof row.label === 'string',
      )
      .map((row) => ({
        personaId: Number(row.personaId),
        label: row.label.trim(),
        creditoId: row.creditoId != null && Number(row.creditoId) > 0 ? Number(row.creditoId) : null,
        at: Number(row.at) || 0,
      }))
      .filter((row) => row.label.length > 0)
      .slice(0, MAX_RECIENTES)
  } catch {
    return []
  }
}

export function recordarCreditoConsulta(entry: {
  personaId: number
  label: string
  creditoId?: number | null
}): CreditoConsultaReciente[] {
  const label = entry.label.trim()
  if (entry.personaId < 1 || !label) {
    return leerCreditoConsultaRecientes()
  }
  const next: CreditoConsultaReciente[] = [
    {
      personaId: entry.personaId,
      label,
      creditoId: entry.creditoId != null && entry.creditoId > 0 ? entry.creditoId : null,
      at: Date.now(),
    },
    ...leerCreditoConsultaRecientes().filter((row) => row.personaId !== entry.personaId),
  ].slice(0, MAX_RECIENTES)
  return persist(next)
}

export function quitarCreditoConsultaReciente(personaId: number): CreditoConsultaReciente[] {
  return persist(leerCreditoConsultaRecientes().filter((row) => row.personaId !== personaId))
}

export function resetCreditoConsultaRecientes(): void {
  persist([])
}
