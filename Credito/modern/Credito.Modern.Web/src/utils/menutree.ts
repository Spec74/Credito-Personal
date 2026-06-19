import type { MenuProps } from 'antd'
import type { MenuItemDto } from '../types/api'
import { formatMenuLabel } from './formatMenuLabel'
import { readMenuIndPadre } from './normalizeMenu'

type AntMenuItem = Required<MenuProps>['items'][number]
const CREDITO_PRENDARIO_MENU_ID = -9002

function sortItems(items: MenuItemDto[]): MenuItemDto[] {
  return [...items].sort((a, b) => (a.orden ?? 0) - (b.orden ?? 0))
}

function normalizeText(value: string | null | undefined): string {
  return (value ?? '')
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
}

function toMenuNode(item: MenuItemDto): AntMenuItem {
  const key = String(item.menuId)
  const label = formatMenuLabel(item.denominacion) || `Menú ${item.menuId}`
  return { key, label }
}

function ordenKey(value: number | null | undefined): number | null {
  if (value == null) {
    return null
  }
  return Math.trunc(value)
}

function isPadre(item: MenuItemDto): boolean {
  return readMenuIndPadre(item.indPadre)
}

/**
 * Misma regla que Web/Views/Shared/_Layout.cshtml:
 * - Padres: IndPadre = true (REPORTES, CREDITO, …)
 * - Hijos: Referencia = Orden del padre (no MenuId del padre)
 * - Sin ítems huérfanos sueltos (el MVC no los pinta).
 */
export function buildAntMenuItems(items: MenuItemDto[]): AntMenuItem[] {
  const sorted = sortItems(items)
  const parents = sorted.filter((i) => isPadre(i))

  if (parents.length === 0) {
    return buildByModulo(sorted)
  }

  const submenuParents: AntMenuItem[] = []

  for (const parent of parents) {
    const parentOrden = ordenKey(parent.orden)
    const children = sorted.filter((c) => {
      if (isPadre(c)) {
        return false
      }
      const ref = ordenKey(c.referencia)
      return parentOrden != null && ref != null && ref === parentOrden
    })

    if (children.length === 0) {
      submenuParents.push(toMenuNode(parent))
      continue
    }

    submenuParents.push({
      key: `parent-${parent.menuId}`,
      label: formatMenuLabel(parent.denominacion) || `Grupo ${parent.menuId}`,
      children: children.map((c) => toMenuNode(c)),
    })
  }

  const dashboard: AntMenuItem = {
    key: 'dashboard',
    label: 'Dashboard',
  }

  return [dashboard, ...submenuParents]
}

export function ensureCreditoPrendarioMenuItem(items: MenuItemDto[]): MenuItemDto[] {
  if (items.length === 0) {
    return items
  }

  const exists = items.some((item) => {
    const label = normalizeText(item.denominacion)
    const url = normalizeText(item.url)
    return (
      label.includes('PRENDARIO') ||
      label.includes('PREDARIO') ||
      url.includes('CREDITO/PRENDARIO') ||
      url.includes('CREDITOPRENDARIO') ||
      url.includes('CREDITOPREDARIO')
    )
  })
  if (exists) {
    return items
  }

  const creditoParent = sortItems(items).find((item) => {
    if (!isPadre(item)) {
      return false
    }
    const label = normalizeText(item.denominacion)
    const modulo = normalizeText(item.modulo)
    return label === 'CREDITO' || modulo === 'CREDITO'
  })
  const parentOrden = ordenKey(creditoParent?.orden)
  if (!creditoParent || parentOrden == null) {
    return items
  }

  const childOrdenes = items
    .filter((item) => !isPadre(item) && ordenKey(item.referencia) === parentOrden)
    .map((item) => item.orden ?? 0)
  const nextOrden = childOrdenes.length > 0 ? Math.max(...childOrdenes) + 1 : parentOrden + 1

  return [
    ...items,
    {
      menuId: CREDITO_PRENDARIO_MENU_ID,
      denominacion: 'Prendario',
      modulo: creditoParent.modulo || 'CREDITO',
      url: '~/Credito/Prendario',
      icono: 'gold',
      indPadre: false,
      orden: nextOrden,
      referencia: parentOrden,
    },
  ]
}

/** Solo si el SP no marca padres (datos atípicos). */
function buildByModulo(items: MenuItemDto[]): AntMenuItem[] {
  const groups = new Map<string, MenuItemDto[]>()
  for (const item of items.filter((i) => !isPadre(i))) {
    const mod = item.modulo?.trim() || 'General'
    const list = groups.get(mod) ?? []
    list.push(item)
    groups.set(mod, list)
  }
  const dashboard: AntMenuItem = {
    key: 'dashboard',
    label: 'Dashboard',
  }
  const grouped = [...groups.entries()].map(([modulo, groupItems]) => ({
    key: `mod-${modulo}`,
    label: formatMenuLabel(modulo),
    children: sortItems(groupItems).map((i) => toMenuNode(i)),
  }))
  return [dashboard, ...grouped]
}

export function findMenuItem(
  items: MenuItemDto[],
  menuId: number,
): MenuItemDto | undefined {
  return items.find((i) => i.menuId === menuId)
}

/** Abrir todos los módulos padre por defecto (acordeón legacy suele mostrar varias secciones visibles). */
export function defaultOpenMenuKeys(items: MenuItemDto[]): string[] {
  const sorted = sortItems(items)
  return sorted.filter((i) => isPadre(i)).map((p) => `parent-${p.menuId}`)
}

/** Accesos rápidos visibles solo si el menú del usuario incluye ese módulo MVC. */
export function filterQuickActionsByMenu<
  T extends { label: string; legacyPath: string },
>(actions: T[], menuItems: MenuItemDto[]): T[] {
  if (menuItems.length === 0) {
    return []
  }
  return actions.filter((action) => {
    const segment = action.legacyPath.replace(/^~\//, '').split('/').filter(Boolean)[0]
    if (!segment) {
      return false
    }
    const needle = segment.toLowerCase()
    return menuItems.some((item) => {
      const url = (item.url ?? '').toLowerCase()
      const modulo = (item.modulo ?? '').toLowerCase()
      return url.includes(needle) || modulo.includes(needle)
    })
  })
}
