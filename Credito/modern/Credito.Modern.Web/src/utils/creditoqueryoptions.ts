/** Tiempos de caché React Query para el módulo Crédito (menos refetch al cambiar pestañas). */
export const creditoStaleTime = {
  /** Maestros: tipos de cargo, productos simulador. */
  master: 5 * 60_000,
  /** Ficha persona, avales por persona. */
  ficha: 2 * 60_000,
  /** Plan, movimientos, contexto de un crédito activo. */
  operacion: 60_000,
  /** Bandeja aprobar, grilla persona paginada. */
  listado: 45_000,
} as const
