import type { ReactNode } from 'react'
import { message } from 'antd'
import {
  openLegacyClientesInactivosGestor,
  openLegacyCreditoObservado,
  openLegacyMorosidadGestor,
} from '../../config/legacyreporturls'
import {
  esCreditoAdministrador,
  esCreditoAnalista,
  esCreditoEncargado,
  esRolCaja,
} from '../../utils/creditoOperacionPermisos'

/** Rutas SPA de atajos de cartera (paridad accesos rápidos MVC). */
export const DASHBOARD_CARTERA_ALLOWED_PATHS = [
  '/informes/cobro-diario',
  '/informes/morosidad-gestor',
  '/informes/creditos-observados',
  '/informes/clientes-inactivos',
] as const

export type DashboardCarteraKind =
  | 'navigate'
  | 'pdf-observados'
  | 'pdf-vencidos'
  | 'pdf-inactivos'

export interface DashboardCarteraAction {
  id: string
  label: string
  kind: DashboardCarteraKind
  spaPath?: string
  icon?: ReactNode
}

function puedeUsarCajaDiario(roles: string[]): boolean {
  return (
    esRolCaja(roles) ||
    esCreditoAdministrador(roles) ||
    esCreditoEncargado(roles)
  )
}

/** Atajos del tablero gestor / analista. */
export function buildAnalistaCarteraActions(
  roles: string[],
): Omit<DashboardCarteraAction, 'icon'>[] {
  const actions: Omit<DashboardCarteraAction, 'icon'>[] = [
    {
      id: 'cobro-diario',
      label: 'Cobro diario',
      kind: 'navigate',
      spaPath: '/informes/cobro-diario',
    },
    {
      id: 'vencidos',
      label: 'Vencidos',
      kind: 'pdf-vencidos',
      spaPath: '/informes/morosidad-gestor',
    },
    {
      id: 'observados',
      label: 'Observados',
      kind: 'pdf-observados',
      spaPath: '/informes/creditos-observados',
    },
    {
      id: 'inactivos',
      label: 'Clientes inactivos',
      kind: 'pdf-inactivos',
      spaPath: '/informes/clientes-inactivos',
    },
    {
      id: 'simulador',
      label: 'Simulador',
      kind: 'navigate',
      spaPath: '/credito/simulador',
    },
  ]

  if (puedeUsarCajaDiario(roles)) {
    actions.push({
      id: 'caja-diario',
      label: 'Caja diario',
      kind: 'navigate',
      spaPath: '/caja/diario',
    })
  }

  return actions
}

/** Atajos del tablero admin / gerencia. */
export function buildAdminCarteraActions(
  roles: string[],
  opts?: { puedeCierreGerencial?: boolean },
): Omit<DashboardCarteraAction, 'icon'>[] {
  const actions: Omit<DashboardCarteraAction, 'icon'>[] = [
    {
      id: 'vencidos',
      label: 'Morosidad / vencidos',
      kind: 'pdf-vencidos',
      spaPath: '/informes/morosidad-gestor',
    },
    {
      id: 'cobro-diario',
      label: 'Cobro diario',
      kind: 'navigate',
      spaPath: '/informes/cobro-diario',
    },
    {
      id: 'observados',
      label: 'Observados',
      kind: 'pdf-observados',
      spaPath: '/informes/creditos-observados',
    },
    {
      id: 'inactivos',
      label: 'Clientes inactivos',
      kind: 'pdf-inactivos',
      spaPath: '/informes/clientes-inactivos',
    },
    {
      id: 'saldo-cartera',
      label: 'Saldo cartera',
      kind: 'navigate',
      spaPath: '/informes/saldo-cartera',
    },
  ]

  if (puedeUsarCajaDiario(roles)) {
    actions.push({
      id: 'caja-saldos',
      label: 'Saldos y cierres',
      kind: 'navigate',
      spaPath: '/caja/saldos',
    })
  }

  if (opts?.puedeCierreGerencial) {
    actions.push({
      id: 'cierre-gerencial',
      label: 'Cierre gerencial',
      kind: 'navigate',
      spaPath: '/informes/cierre-gerencial',
    })
  }

  if (esCreditoAnalista(roles)) {
    actions.push({
      id: 'vista-analista',
      label: 'Mi tablero analista',
      kind: 'navigate',
      spaPath: '/inicio?vista=analista',
    })
  }

  actions.push({
    id: 'mapa-modulos',
    label: 'Mapa de módulos',
    kind: 'navigate',
    spaPath: '/inicio?vista=modulos',
  })

  return actions
}

export function runDashboardCarteraAction(
  action: Pick<DashboardCarteraAction, 'kind' | 'spaPath' | 'label'>,
  ctx: {
    oficinaId: number
    usuarioId: number
    navigate: (to: string) => void
  },
): void {
  const { oficinaId, usuarioId, navigate } = ctx

  if (action.kind === 'navigate') {
    if (!action.spaPath) {
      message.warning(`Acción «${action.label}» sin ruta configurada.`)
      return
    }
    navigate(action.spaPath)
    return
  }

  if (usuarioId < 1) {
    message.warning('Sesión incompleta: no se puede generar el reporte.')
    return
  }

  try {
    if (action.kind === 'pdf-observados') {
      openLegacyCreditoObservado(undefined, usuarioId, 'PDF')
      return
    }
    if (action.kind === 'pdf-vencidos') {
      openLegacyMorosidadGestor(undefined, usuarioId, 'PDF')
      return
    }
    if (action.kind === 'pdf-inactivos') {
      openLegacyClientesInactivosGestor(oficinaId, usuarioId)
      return
    }
  } catch {
    message.error('No se pudo abrir el reporte. Permita ventanas emergentes.')
  }
}
