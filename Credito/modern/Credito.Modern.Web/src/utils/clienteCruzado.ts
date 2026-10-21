/** Persona es catálogo único. La cartera se opera solo en la oficina del JWT. */
export const CLIENTE_CRUZADO_AVISO =
  'Si el cliente ya existe (otra sede incluida), úselo aquí. No cree un duplicado. Los créditos de otra oficina se ven al entrar en esa sede.'

export function esBusquedaPersonaGlobal(): boolean {
  return true
}
