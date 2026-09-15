/** Accesos rápidos del sidebar inferior del MVC (_Layout.cshtml). */
export interface QuickAction {
  label: string
  legacyPath: string
  /** Si existe, abre la ruta SPA en lugar del MVC. */
  spaPath?: string
}

export const quickActions: QuickAction[] = [
  {
    label: 'Comisiones',
    legacyPath: '/Comision/Index',
    spaPath: '/admin/comisiones',
  },
  {
    label: 'Simulador de crédito',
    legacyPath: '/Credito/Index',
    spaPath: '/credito/simulador',
  },
  {
    label: 'Observados',
    legacyPath: '/Reporte/ReporteCreditoObservado',
    spaPath: '/informes/creditos-observados',
  },
  {
    label: 'Vencidos',
    legacyPath: '/Reporte/ReporteMorosidadGestor',
    spaPath: '/informes/morosidad-gestor',
  },
  {
    label: 'Clientes inactivos',
    legacyPath: '/Cliente/Index',
    spaPath: '/informes/clientes-inactivos',
  },
]
