import type {
  ClientesInactivosParams,
  ClientesNuevosMesParams,
  CobroDiarioParams,
  GestorInformeParams,
} from '../types/api'
import type { CobroDiarioQueryParams } from '../api/creditoPlanes'

/** Oficina = sesión JWT (paridad pantallas modernas de crédito). */
export function toGestorInformeParams(
  oficinaSesion: number,
  usuarioId?: number,
): GestorInformeParams {
  const p: GestorInformeParams = { oficinaId: oficinaSesion }
  if (usuarioId != null && usuarioId > 0) {
    p.usuarioId = usuarioId
  }
  return p
}

export function toCobroDiarioQuery(
  oficinaSesion: number,
  usuarioId?: number,
  soloMora?: boolean,
): CobroDiarioQueryParams {
  const p: CobroDiarioQueryParams = { oficinaId: oficinaSesion }
  if (usuarioId != null && usuarioId > 0) {
    p.usuarioId = usuarioId
  }
  if (soloMora) {
    p.soloMora = true
  }
  return p
}

export function toCobroDiarioParams(oficinaSesion: number, usuarioId: number): CobroDiarioParams {
  return { oficinaId: oficinaSesion, usuarioId }
}

export function gestorLabelFromId(usuarioId?: number): string {
  return usuarioId != null && usuarioId > 0 ? `Gestor #${usuarioId}` : 'Todos los gestores'
}

export function toClientesNuevosMesParams(
  oficinaSesion: number,
  usuarioId: number | undefined,
  fechaIni: string,
  fechaFin: string,
): ClientesNuevosMesParams {
  const p: ClientesNuevosMesParams = {
    oficinaId: oficinaSesion,
    fechaIni,
    fechaFin,
  }
  if (usuarioId != null && usuarioId > 0) {
    p.usuarioId = usuarioId
  }
  return p
}

export function toClientesInactivosParams(
  oficinaSesion: number,
  usuarioId: number | undefined,
  fechaIni?: string,
  fechaFin?: string,
): ClientesInactivosParams {
  const p: ClientesInactivosParams = { oficinaId: oficinaSesion }
  if (usuarioId != null && usuarioId > 0) {
    p.usuarioId = usuarioId
  }
  if (fechaIni) {
    p.fechaIni = fechaIni
  }
  if (fechaFin) {
    p.fechaFin = fechaFin
  }
  return p
}

