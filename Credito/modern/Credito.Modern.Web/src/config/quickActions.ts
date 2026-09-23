/** Accesos rápidos del sidebar inferior (paridad MVC `_Layout.cshtml`). */
export type QuickActionKind = 'navigate' | 'pdf-observados' | 'pdf-vencidos' | 'pdf-inactivos'

export interface QuickAction {
  label: string
  legacyPath: string
  /** Ruta SPA (navigate) o contexto del informe. */
  spaPath?: string
  /**
   * `navigate`: abre pantalla SPA.
   * `pdf-*`: genera el PDF en pestaña nueva (paridad `window.open` del MVC).
   */
  kind: QuickActionKind
}

export const quickActions: QuickAction[] = [
  {
    label: 'Comisiones',
    legacyPath: '/Comision/Index',
    spaPath: '/admin/comisiones',
    kind: 'navigate',
  },
  {
    label: 'Simulador de crédito',
    legacyPath: '/Credito/Index',
    spaPath: '/credito/simulador',
    kind: 'navigate',
  },
  {
    label: 'Observados',
    legacyPath: '/Reporte/ReporteCreditoObservado',
    spaPath: '/informes/creditos-observados',
    kind: 'pdf-observados',
  },
  {
    label: 'Vencidos',
    legacyPath: '/Reporte/ReporteMorosidadGestor',
    spaPath: '/informes/morosidad-gestor',
    kind: 'pdf-vencidos',
  },
  {
    label: 'Clientes inactivos',
    legacyPath: '/Cliente/Index',
    spaPath: '/informes/clientes-inactivos',
    kind: 'pdf-inactivos',
  },
]
