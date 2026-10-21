import { apiFetch } from './client'

export interface PlaParametro {
  plaParametroId: number
  individualUsd: number
  multipleUsd: number
  umbralIndividualMenorUsd: number | null
  umbralMultipleMenorUsd: number | null
  individualEfectivoUsd: number
  multipleEfectivoUsd: number
  tcVentaMesAnterior: number | null
  tcPeriodo: string | null
  fuenteNorma: string
  tieneTipoCambio: boolean
}

export interface GuardarPlaParametroRequest {
  tcVentaMesAnterior: number | null
  tcPeriodo: string | null
  umbralIndividualMenorUsd: number | null
  umbralMultipleMenorUsd: number | null
}

export interface PlaCandidatoRo {
  tipo: string
  creditoId: number | null
  personaId: number | null
  numeroDocumento: string | null
  nombreCompleto: string | null
  monto: number
  fechaOp: string
  superaUmbral: boolean
  motivoRo: string
  faltaDocumento: boolean
  faltaDireccion: boolean
  faltaOcupacion: boolean
  individualEfectivoUsd: number
  multipleEfectivoUsd: number
  tcVentaMesAnterior: number | null
  umbralIndividualPen: number | null
  umbralMultiplePen: number | null
  tieneTipoCambio: boolean
  montoMes: number | null
}

export interface PlaInusual {
  plaInusualId: number
  oficinaId: number
  personaId: number
  numeroDocumento: string | null
  nombreCompleto: string | null
  creditoId: number | null
  calificacion: string
  motivo: string
  usuarioRegId: number
  nombreUsuario: string | null
  fechaReg: string
}

export interface RegistrarPlaInusualRequest {
  oficinaId: number
  personaId: number
  creditoId?: number | null
  calificacion: string
  motivo: string
}

export function fetchPlaParametro() {
  return apiFetch<PlaParametro>('/pla/parametro')
}

export function savePlaParametro(body: GuardarPlaParametroRequest) {
  return apiFetch<PlaParametro>('/pla/parametro', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchPlaCandidatosRo(oficinaId: number, anio: number, mes: number) {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    anio: String(anio),
    mes: String(mes),
  })
  return apiFetch<PlaCandidatoRo[]>(`/pla/candidatos-ro?${q}`)
}

export function fetchPlaInusual(oficinaId: number, anio: number, mes: number) {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    anio: String(anio),
    mes: String(mes),
  })
  return apiFetch<PlaInusual[]>(`/pla/inusual?${q}`)
}

export function registrarPlaInusual(body: RegistrarPlaInusualRequest) {
  return apiFetch<{ plaInusualId: number; calificacion: string }>('/pla/inusual', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
