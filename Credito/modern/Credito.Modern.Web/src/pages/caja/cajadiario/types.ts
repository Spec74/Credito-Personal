import { ApiError } from '../../../api/errors'

export interface CajaSession {
  oficinaId: number
  cajaDiarioId: number
  cajaId: number
  cajaDenominacion: string
  fechaIniOperacion: string
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  indCierre: boolean
  esCajaCentral: boolean
}

export const CAJA_DIARIO_TABS = [
  'cobranzas',
  'desembolsos',
  'egreso-ingreso',
  'arqueo',
  'cxc',
  'cierre',
] as const

export type CajaDiarioTabKey = (typeof CAJA_DIARIO_TABS)[number]

export function isCajaDiarioTabKey(v: string | null): v is CajaDiarioTabKey {
  return v != null && (CAJA_DIARIO_TABS as readonly string[]).includes(v)
}

export function errMsg(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Error en la operación'
}
