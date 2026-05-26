import dayjs, { type Dayjs } from 'dayjs'

/** Lee parámetros legacy (pFechaHasta) o API (hastaFecha) desde la URL. */
export function readUrlDay(
  search: URLSearchParams,
  legacyKey: string,
  apiKey: string,
): Dayjs | undefined {
  const raw = search.get(apiKey) ?? search.get(legacyKey)
  if (!raw) return undefined
  const dmy = dayjs(raw, 'DD/MM/YYYY', true)
  if (dmy.isValid()) return dmy
  const iso = dayjs(raw)
  return iso.isValid() ? iso : undefined
}

export function readUrlInt(
  search: URLSearchParams,
  legacyKey: string,
  apiKey: string,
): number | undefined {
  const raw = search.get(apiKey) ?? search.get(legacyKey)
  if (raw == null || raw === '') return undefined
  const n = Number(raw)
  return Number.isFinite(n) ? n : undefined
}

export function readUrlOfficeId(search: URLSearchParams): number | undefined {
  return readUrlInt(search, 'pOficinaId', 'oficinaId')
}

export function readUrlUserId(search: URLSearchParams): number | undefined {
  return readUrlInt(search, 'pUsuarioId', 'usuarioId')
}
