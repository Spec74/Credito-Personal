/** Búsqueda multi-palabra en el listado de tareas (todas las palabras deben coincidir). */
export function filtraTareasPorBusqueda<
  T extends {
    clienteNombre: string
    clienteDni: string
    creditoId: number
    tareaId: number
    nombreUsuario?: string | null
  },
>(rows: T[], query: string): T[] {
  const tokens = query
    .trim()
    .toLowerCase()
    .split(/\s+/)
    .filter((t) => t.length > 0)
  if (tokens.length === 0) {
    return rows
  }
  return rows.filter((r) => {
    const blob = `${r.clienteNombre} ${r.clienteDni} ${r.creditoId} ${r.tareaId} ${r.nombreUsuario ?? ''}`.toLowerCase()
    return tokens.every((tok) => blob.includes(tok))
  })
}
