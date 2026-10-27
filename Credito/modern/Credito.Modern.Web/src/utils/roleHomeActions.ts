import {
  esCreditoAdministrador,
  esCreditoAnalista,
  esCreditoAprobador1,
  esCreditoEncargado,
  esCreditoPerfilSoloBandeja,
  esRolCaja,
  puedeCompletarBienesPrendarioUi,
} from './creditoOperacionPermisos'

export type RoleHomeAction = {
  to: string
  label: string
  primary?: boolean
}

/** Acciones de “Hoy”: pocas, por rol. El menú sigue siendo el catálogo. */
export function buildRoleHomeActions(roles: string[]): RoleHomeAction[] {
  if (esCreditoPerfilSoloBandeja(roles)) {
    return [{ to: '/credito/aprobar', label: 'Aprobar créditos', primary: true }]
  }

  const actions: RoleHomeAction[] = []
  const push = (item: RoleHomeAction) => {
    if (actions.some((a) => a.to === item.to)) return
    actions.push(item)
  }

  const esCaja = esRolCaja(roles) || roles.some((r) => r.toUpperCase().includes('GESTOR'))
  const esAdmin = esCreditoAdministrador(roles)
  const esEncargado = esCreditoEncargado(roles) || esAdmin
  const esAprobador = esCreditoAprobador1(roles) || esAdmin
  const esAnalista = esCreditoAnalista(roles)

  if (esCaja) {
    push({ to: '/caja/diario', label: 'Caja del día', primary: true })
  } else if (esAnalista && puedeCompletarBienesPrendarioUi(roles)) {
    push({ to: '/credito/prendario/nuevo', label: 'Nuevo prendario', primary: true })
  } else if (esAnalista) {
    push({ to: '/credito/nuevo', label: 'Nueva solicitud', primary: true })
  } else if (esAprobador) {
    push({ to: '/credito/aprobar', label: 'Aprobar', primary: true })
  }

  if (esAprobador) {
    push({ to: '/credito/aprobar', label: 'Aprobar', primary: !actions.some((a) => a.primary) })
  }
  if (esEncargado) {
    push({ to: '/caja/saldos', label: 'Saldos y cierre' })
    push({ to: '/tesoreria/fondear-sucursales', label: 'Fondear sedes' })
    push({ to: '/tesoreria/red', label: 'Red' })
  }
  if (esAdmin) {
    push({ to: '/informes', label: 'Informes' })
  }

  if (esCaja) {
    push({ to: '/caja/cobro', label: 'Cobrar' })
    push({ to: '/credito/consulta', label: 'Consulta' })
  }

  if (esAnalista) {
    if (puedeCompletarBienesPrendarioUi(roles)) {
      push({
        to: '/credito/prendario/nuevo',
        label: 'Nuevo prendario',
        primary: !actions.some((a) => a.primary),
      })
    }
    push({ to: '/credito/nuevo', label: 'Nueva solicitud', primary: !actions.some((a) => a.primary) })
    push({ to: '/credito/simulador', label: 'Simulador' })
    push({ to: '/credito/consulta', label: 'Consulta' })
    push({ to: '/clientes', label: 'Clientes' })
    push({ to: '/credito/tareas', label: 'Tareas' })
  }

  if (actions.length === 0) {
    push({ to: '/credito/consulta', label: 'Consulta', primary: true })
    push({ to: '/clientes', label: 'Clientes' })
  }

  return actions.slice(0, 6)
}
