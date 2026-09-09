/** Ruta interna de la SPA: empieza por `/` y no es URL absoluta. */
export function parseInternalPath(raw: string | null | undefined): string | null {
  if (!raw) return null
  const value = raw.trim()
  if (!value.startsWith('/') || value.startsWith('//')) return null
  if (value.includes('://') || value.includes('\\')) return null
  return value
}

export function withSearchParam(path: string, key: string, value: string): string {
  const qIndex = path.indexOf('?')
  const pathname = qIndex >= 0 ? path.slice(0, qIndex) : path
  const qs = qIndex >= 0 ? path.slice(qIndex + 1) : ''
  const params = new URLSearchParams(qs)
  params.set(key, value)
  const next = params.toString()
  return next ? `${pathname}?${next}` : pathname
}

export const PRENDARIO_NUEVO_PATH = '/credito/prendario/nuevo'

export function buildClientesNuevoHref(options: { returnTo: string; dni?: string }): string {
  const params = new URLSearchParams()
  params.set('returnTo', options.returnTo)
  const dni = options.dni?.replace(/\D/g, '') ?? ''
  if (dni) params.set('dni', dni)
  return `/clientes/nuevo?${params.toString()}`
}

export function buildPrendarioNuevoHref(personaId: number, origen?: 'alta'): string {
  let path = withSearchParam(PRENDARIO_NUEVO_PATH, 'personaId', String(personaId))
  if (origen) {
    path = withSearchParam(path, 'origen', origen)
  }
  return path
}

export function labelClienteFicha(p: {
  numeroDocumento: string
  nombre: string
  apePaterno?: string | null
  apeMaterno?: string | null
}): string {
  const apellidos = [p.apePaterno, p.apeMaterno].filter(Boolean).join(' ')
  const nombre = apellidos ? `${apellidos}, ${p.nombre}` : p.nombre
  return `${p.numeroDocumento} ${nombre}`.trim()
}
