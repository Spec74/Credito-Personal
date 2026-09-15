/** Clave unificada para `GET creditos-grilla-persona` (consulta + persona). */
export function creditosGrillaPersonaQueryKey(
  oficinaId: number,
  personaId: number,
  grupoActivo: boolean,
  page: number,
  pageSize: number,
) {
  return ['creditos-grilla-persona', oficinaId, personaId, grupoActivo, page, pageSize] as const
}
