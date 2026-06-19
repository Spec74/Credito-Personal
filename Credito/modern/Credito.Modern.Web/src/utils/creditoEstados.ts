import {
  puedeAnularCreditoUi,
  puedeOperarCicloCredito,
  puedeProrrogarCreditoUi,
  puedeReprogramarCreditoUi,
} from './creditoOperacionPermisos'

export type CreditoEstadoCodigo = 'CRE' | 'PEN' | 'AP1' | 'APR' | 'DES' | 'PAG' | 'REP' | 'ANU'

type CreditoEstadoMeta = {
  codigo: CreditoEstadoCodigo
  label: string
  descripcion: string
  color: string
  grupo: 'solicitud' | 'aprobacion' | 'vigente' | 'cerrado'
}

const ESTADOS: Record<CreditoEstadoCodigo, CreditoEstadoMeta> = {
  CRE: {
    codigo: 'CRE',
    label: 'Solicitud',
    descripcion: 'Solicitud creada, pendiente de generar plan definitivo.',
    color: 'blue',
    grupo: 'solicitud',
  },
  PEN: {
    codigo: 'PEN',
    label: 'Pendiente',
    descripcion: 'Crédito generado y pendiente de aprobación.',
    color: 'gold',
    grupo: 'aprobacion',
  },
  AP1: {
    codigo: 'AP1',
    label: '1.ª aprobación',
    descripcion: 'Crédito con primera aprobación.',
    color: 'cyan',
    grupo: 'aprobacion',
  },
  APR: {
    codigo: 'APR',
    label: 'Aprobado',
    descripcion: 'Crédito aprobado, pendiente de desembolso.',
    color: 'green',
    grupo: 'aprobacion',
  },
  DES: {
    codigo: 'DES',
    label: 'Desembolsado',
    descripcion: 'Crédito activo en cobranza.',
    color: 'processing',
    grupo: 'vigente',
  },
  PAG: {
    codigo: 'PAG',
    label: 'Pagado',
    descripcion: 'Crédito cancelado.',
    color: 'success',
    grupo: 'cerrado',
  },
  REP: {
    codigo: 'REP',
    label: 'Reprogramado',
    descripcion: 'Crédito reprogramado.',
    color: 'purple',
    grupo: 'cerrado',
  },
  ANU: {
    codigo: 'ANU',
    label: 'Anulado',
    descripcion: 'Crédito anulado.',
    color: 'error',
    grupo: 'cerrado',
  },
}

export const CREDITO_ESTADO_OPTIONS = Object.values(ESTADOS).map((e) => ({
  value: e.codigo,
  label: `${e.codigo} - ${e.label}`,
}))

/** Paridad de combos legacy de reportes: no exponen AP1/APR. */
export const CREDITO_ESTADO_REPORTE_OPTIONS = ['CRE', 'PEN', 'DES', 'PAG', 'REP', 'ANU'].map(
  (codigo) => ({
    value: codigo,
    label: `${codigo} - ${ESTADOS[codigo as CreditoEstadoCodigo].label}`,
  }),
)

export function normalizeCreditoEstado(estado?: string | null): CreditoEstadoCodigo | null {
  const codigo = estado?.trim().toUpperCase()
  return codigo && codigo in ESTADOS ? (codigo as CreditoEstadoCodigo) : null
}

export function getCreditoEstadoMeta(estado?: string | null): CreditoEstadoMeta | null {
  const codigo = normalizeCreditoEstado(estado)
  return codigo ? ESTADOS[codigo] : null
}

export function describeCreditoEstado(estado?: string | null): string {
  const meta = getCreditoEstadoMeta(estado)
  return meta ? `${meta.codigo} - ${meta.label}` : estado?.trim() || 'Sin estado'
}

function estadoPermite(estado: string | null | undefined, permitidos: CreditoEstadoCodigo[]) {
  const codigo = normalizeCreditoEstado(estado)
  return codigo === null || permitidos.includes(codigo)
}

export function resolverCreditoAccionesUi(roles: string[], estado?: string | null) {
  const puedeOperar = puedeOperarCicloCredito(roles)
  return {
    cobrar: puedeOperar && estadoPermite(estado, ['DES']),
    mora: puedeOperar && estadoPermite(estado, ['DES']),
    anular:
      puedeAnularCreditoUi(roles) &&
      estadoPermite(estado, ['CRE', 'PEN', 'AP1', 'APR', 'DES']),
    prorrogar: puedeProrrogarCreditoUi(roles) && estadoPermite(estado, ['DES']),
    reprogramar: puedeReprogramarCreditoUi(roles) && estadoPermite(estado, ['DES']),
    editarGestion:
      puedeOperar && estadoPermite(estado, ['CRE', 'PEN', 'AP1', 'APR', 'DES']),
  }
}
