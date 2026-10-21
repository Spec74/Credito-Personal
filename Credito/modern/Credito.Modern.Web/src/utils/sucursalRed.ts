/** Sedes de referencia (Huamanga central + sucursales). Los IDs salen de MAESTRO.Oficina. */
export const SEDES_REFERENCIA = [
  { clave: 'huamanga', etiqueta: 'Huamanga', rol: 'central' as const },
  { clave: 'pichari', etiqueta: 'Pichari', rol: 'sucursal' as const },
  { clave: 'huanta', etiqueta: 'Huanta', rol: 'sucursal' as const },
  { clave: 'andahuaylas', etiqueta: 'Andahuaylas', rol: 'sucursal' as const },
] as const

export type BovedaDestinoSede = {
  bovedaId: number
  oficinaId: number
  oficina: string
  saldoFinal?: number
}

/** Cierre de cajas → bóveda: nunca reescribir a oficina 1. */
export function buildCerrarCajasDiariosBody(oficinaId: number, sobrante = 0) {
  if (!(oficinaId > 0)) {
    throw new Error('Oficina de sesión requerida')
  }
  return { oficinaId, sobrante }
}

export function cierreUsaOficinaSesion(requestOficinaId: number, sesionOficinaId: number): boolean {
  return requestOficinaId > 0 && requestOficinaId === sesionOficinaId
}

export function resolverBovedaDestinoPorSede(
  destinos: BovedaDestinoSede[],
  oficinaId: number,
): BovedaDestinoSede | null {
  return destinos.find((d) => d.oficinaId === oficinaId) ?? null
}

export function coincideSedeReferencia(nombre: string, clave: string): boolean {
  return nombre.trim().toLowerCase().includes(clave)
}

/** Re-login en otra sede. El JWT lleva una sola oficina; no se cambia en caliente. */
export function loginSedePath(oficinaId: number): string {
  return oficinaId > 0 ? `/login?oficinaId=${oficinaId}` : '/login'
}

export function parseLoginOficinaId(search: string): number {
  const raw = search.startsWith('?') ? search.slice(1) : search
  const value = new URLSearchParams(raw).get('oficinaId')
  const id = Number(value)
  return Number.isInteger(id) && id > 0 ? id : 0
}

export const WIZARD_SUCURSAL_PASOS = [
  { id: 'oficina', title: 'Crear oficina', to: '/mantenimiento/oficinas' },
  { id: 'boveda', title: 'Verificar bóveda', to: '/tesoreria/boveda' },
  { id: 'usuarios', title: 'Usuarios y roles por sede', to: '/admin/usuarios' },
  { id: 'cajas', title: 'Cajas de la sucursal', to: '/mantenimiento/cajas' },
  { id: 'fondeo', title: 'Fondear desde central', to: '/tesoreria/fondear-sucursales' },
  { id: 'cajeros', title: 'Asignar a cajeros', to: '/caja/saldos' },
  { id: 'cierre', title: 'Smoke de cierre', to: '/caja/saldos' },
  { id: 'menu', title: 'Menú de la sede', to: '/admin/roles' },
] as const
