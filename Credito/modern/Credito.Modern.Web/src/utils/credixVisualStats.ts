import type { CredixStatItem } from '../components/credix'
import type { CredixHubSection } from '../components/credix/CredixHubGrid'

/** Cuenta enlaces en secciones de hub. */
export function countHubLinks(sections: CredixHubSection[]): number {
  return sections.reduce((n, s) => n + s.links.length, 0)
}

/** KPIs estándar para pantallas hub de módulo (misma franja que inicio/caja). */
export function buildModuleHubStats(
  moduleLabel: string,
  sections: CredixHubSection[],
  opts?: {
    oficinaLabel?: string | number
    usuarioLabel?: string | number
    extra?: CredixStatItem[]
  },
): CredixStatItem[] {
  const linkCount = countHubLinks(sections)
  const items: CredixStatItem[] = [
    { value: moduleLabel, label: 'Módulo' },
    { value: linkCount, label: 'Accesos en esta pantalla' },
  ]
  if (opts?.oficinaLabel != null) {
    items.push({ value: opts.oficinaLabel, label: 'Oficina' })
  }
  if (opts?.usuarioLabel != null) {
    items.push({ value: opts.usuarioLabel, label: 'Usuario' })
  }
  if (opts?.extra?.length) {
    items.push(...opts.extra)
  }
  items.push({ value: 'SPA', label: 'Interfaz', tone: 'green' })
  return items
}

/** Franja para listados CRUD tras cargar datos. */
export function buildCrudListStats(
  rows: unknown[] | undefined,
  opts?: { entityLabel?: string; activeCount?: number },
): CredixStatItem[] {
  const list = rows ?? []
  const activos =
    opts?.activeCount ??
    list.filter((r) => (r as { estado?: boolean }).estado === true).length
  const entity = opts?.entityLabel ?? 'Registros'
  return [
    { value: list.length, label: `Total ${entity}` },
    {
      value: activos,
      label: 'Activos',
      tone: activos > 0 ? 'green' : 'default',
    },
    {
      value: Math.max(0, list.length - activos),
      label: 'Inactivos',
    },
  ]
}

export type InformeStatsInput = {
  /** Filas mostradas; null si aún no consultó. */
  rowCount?: number | null
  /** true tras submit exitoso o error de consulta */
  queried?: boolean
  oficinaId?: number | null
  extras?: CredixStatItem[]
}

/** KPIs para informes tabulares (sustituye Statistic sueltos). */
export function buildInformeStats(input: InformeStatsInput): CredixStatItem[] {
  const items: CredixStatItem[] = []
  if (input.oficinaId != null && input.oficinaId > 0) {
    items.push({ value: input.oficinaId, label: 'Oficina consulta' })
  }
  if (input.queried) {
    const n = input.rowCount ?? 0
    items.push({
      value: n,
      label: 'Filas en resultado',
      tone: n > 0 ? 'green' : 'default',
    })
    items.push({
      value: n > 0 ? 'Con datos' : 'Sin filas',
      label: 'Estado',
      tone: n > 0 ? 'green' : 'default',
    })
  } else {
    items.push({ value: '—', label: 'Filas en resultado' })
    items.push({ value: 'Pendiente', label: 'Estado' })
  }
  if (input.extras?.length) {
    items.push(...input.extras)
  }
  return items
}
