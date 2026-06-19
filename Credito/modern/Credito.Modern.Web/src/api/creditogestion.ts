import { apiFetch } from './client'

/** Paridad cabecera `Creditos.cshtml` + `CreditoController.Creditos(pPersonaId)`. */
export interface PersonaCreditoFicha {
  personaId: number
  numeroDocumento: string
  nombreCompleto: string
  codigo: string | null
  calificacion: string
  calificacionLabel: string
  clienteActivo: boolean
  estadoCliente: string
  bloqueado: boolean
  clasificacionRiesgoSbsItemId: number | null
  clasificacionRiesgoSbsCodigo: string | null
  clasificacionRiesgoSbsLabel: string | null
  clasificacionRiesgoSbsObs: string | null
  topeCredito: number
  totalCreditos: number
  creditosPendientes: number
  solicitudCreditoId: number | null
  depuradoDescripcion: string | null
  puedeCrearSolicitud: boolean
}

/** Paridad cabecera `Creditos.cshtml` + `CreditoController.Creditos(pPersonaId)`. */
export interface CreditoContexto {
  creditoId: number
  personaId: number
  personaNombre: string | null
  observacion: string | null
  indCondonacion: boolean
  montoCondonacion: number
  indIrrecuperable: boolean
  montoGastosAdm: number
  centralRiesgo: number
  personaAvalId: number | null
  personaAvalNombre: string | null
}

export interface SolicitudCreditoDetalle {
  solicitudCreditoId: number
  personaId: number
  cliente: string
  productoId: number | null
  montoCredito: number
  formaPago: string | null
  numeroCuotas: number
  interes: number
  fechaPrimerPago: string | null
  montoGastosAdm: number
  observacion: string | null
  centralRiesgo: number
}

export interface CargoCreditoRow {
  cargoId: number
  tipoCargo: string
  numCuota: number
  descripcion: string
  importe: number
  usuarioCargo: string
  estado: string
}

export interface CreditoEvidencia {
  id: number
  creditoId: number
  imagen: string
  url: string | null
}

export interface CreditoPrenda {
  creditoPrendaId: number
  creditoId: number
  descripcion: string
  montoTasacion: number
  fechaRemate: string
  observacion: string | null
  estado: boolean
}

export interface CreditoGrillaPersonaRow {
  creditoId: number
  personaId: number
  estado: string
  montoCredito: number
  fechaPrimerPago: string | null
  descripcion: string | null
}

export interface CreditoGrillaPersonaPage {
  items: CreditoGrillaPersonaRow[]
  totalCount: number
  page: number
  pageSize: number
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchPersonaCreditoFicha(
  oficinaId: number,
  personaId: number,
): Promise<PersonaCreditoFicha> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    personaId: String(personaId),
  })
  return apiFetch<PersonaCreditoFicha>(`/credito/persona-credito-ficha?${q}`)
}

export function fetchCreditoContexto(creditoId: number): Promise<CreditoContexto> {
  return apiFetch<CreditoContexto>(`/credito/credito-contexto?creditoId=${creditoId}`)
}

export function fetchSolicitudCredito(
  oficinaId: number,
  solicitudCreditoId: number,
): Promise<SolicitudCreditoDetalle> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    solicitudCreditoId: String(solicitudCreditoId),
  })
  return apiFetch<SolicitudCreditoDetalle>(`/credito/solicitud-credito?${q}`)
}

export function fetchCargosCredito(
  oficinaId: number,
  creditoId: number,
): Promise<CargoCreditoRow[]> {
  return apiFetch<CargoCreditoRow[]>(
    `/credito/cargos-credito?oficinaId=${oficinaId}&creditoId=${creditoId}`,
  )
}

export function fetchEvidenciasCredito(
  oficinaId: number,
  creditoId: number,
): Promise<CreditoEvidencia[]> {
  return apiFetch<CreditoEvidencia[]>(
    `/credito/evidencias-credito?oficinaId=${oficinaId}&creditoId=${creditoId}`,
  )
}

export function fetchCreditoPrenda(
  oficinaId: number,
  creditoId: number,
): Promise<CreditoPrenda | null> {
  return apiFetch<CreditoPrenda | null>(
    `/credito/credito-prenda?oficinaId=${oficinaId}&creditoId=${creditoId}`,
  )
}

export function fetchCreditosGrillaPersona(params: {
  oficinaId: number
  personaId: number
  grupoActivo: boolean
  page?: number
  pageSize?: number
}): Promise<CreditoGrillaPersonaPage> {
  const q = new URLSearchParams({
    oficinaId: String(params.oficinaId),
    personaId: String(params.personaId),
    grupoActivo: String(params.grupoActivo),
    page: String(params.page ?? 1),
    pageSize: String(params.pageSize ?? 25),
  })
  return apiFetch<CreditoGrillaPersonaPage>(`/credito/creditos-grilla-persona?${q}`)
}

export function condonarCredito(body: {
  oficinaId: number
  creditoId: number
  montoCxc: number
  montoCondonacion: number
  observacion: string
}) {
  return postJson<{ success: boolean; mensaje: string | null }>('/credito/condonar-credito', body)
}

export function observarCredito(body: {
  oficinaId: number
  creditoId: number
  observacion: string
}) {
  return postJson<{ success: boolean; mensaje: string | null }>('/credito/observar-credito', body)
}

export function guardarCargoCredito(body: {
  oficinaId: number
  creditoId: number
  tipoCargoId: number
  monto: number
  descripcion: string
  final: boolean
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/guardar-cargo-credito',
    body,
  )
}

export async function subirEvidenciaCredito(
  oficinaId: number,
  creditoId: number,
  file: File,
): Promise<{ success: boolean; mensaje: string | null }> {
  const form = new FormData()
  form.append('oficinaId', String(oficinaId))
  form.append('creditoId', String(creditoId))
  form.append('imagen', file)
  return apiFetch('/credito/subir-evidencia-credito', {
    method: 'POST',
    body: form,
  })
}

export function eliminarEvidenciaCredito(body: {
  oficinaId: number
  creditoImagenId: number
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/eliminar-evidencia-credito',
    body,
  )
}

export function cambiarAnalistaCredito(body: {
  oficinaId: number
  creditoId: number
  analistaId: number
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/cambiar-analista-credito',
    body,
  )
}

export function actualizarTopeCredito(body: {
  oficinaId: number
  personaId: number
  topeCredito: number
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/actualizar-tope-credito',
    body,
  )
}

export function depurarPersonaCredito(body: {
  oficinaId: number
  personaId: number
  observacion: string
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/depurar-persona-credito',
    body,
  )
}

export function actualizarIrrecuperableCredito(body: {
  oficinaId: number
  creditoId: number
  indIrrecuperable: boolean
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/actualizar-irrecuperable-credito',
    body,
  )
}

export function modificarTramiteAdmCredito(body: {
  oficinaId: number
  creditoId: number
  valor: number
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/modificar-tramite-adm-credito',
    body,
  )
}

export function modificarCentralRiesgoCredito(body: {
  oficinaId: number
  creditoId: number
  valor: number
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/modificar-central-riesgo-credito',
    body,
  )
}

export function actualizarDescuentoPlanPago(body: {
  oficinaId: number
  creditoId: number
  planPagoId: number
  descuento: number
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/actualizar-descuento-plan-pago',
    body,
  )
}

export function actualizarAvalCredito(body: {
  oficinaId: number
  creditoId: number
  personaAvalId: number | null
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/actualizar-aval-credito',
    body,
  )
}

export function guardarPrendaCredito(body: {
  oficinaId: number
  creditoId: number
  descripcion: string
  montoTasacion: number
  fechaRemate: string
  observacion?: string | null
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/guardar-prenda-credito',
    body,
  )
}
