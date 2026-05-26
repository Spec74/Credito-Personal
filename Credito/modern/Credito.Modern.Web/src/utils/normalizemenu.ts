import type { MenuItemDto } from '../types/api'

function readNumber(value: unknown): number | null {
  if (value == null || value === '') {
    return null
  }
  const n = Number(value)
  return Number.isFinite(n) ? n : null
}

/** SQL bit / JSON pueden llegar como boolean, 0/1 o PascalCase. */
export function readMenuIndPadre(value: unknown): boolean {
  if (value === true || value === 1 || value === '1') {
    return true
  }
  if (typeof value === 'string') {
    return value === 'true' || value === 'True'
  }
  return false
}

/** Acepta camelCase y PascalCase del API. */
export function normalizeMenuItem(raw: Record<string, unknown>): MenuItemDto {
  return {
    menuId: readNumber(raw.menuId ?? raw.MenuId) ?? 0,
    denominacion: (raw.denominacion ?? raw.Denominacion ?? null) as string | null,
    modulo: (raw.modulo ?? raw.Modulo ?? null) as string | null,
    url: (raw.url ?? raw.Url ?? null) as string | null,
    icono: (raw.icono ?? raw.Icono ?? null) as string | null,
    indPadre: readMenuIndPadre(raw.indPadre ?? raw.IndPadre),
    orden: readNumber(raw.orden ?? raw.Orden),
    referencia: readNumber(raw.referencia ?? raw.Referencia),
  }
}

export function normalizeMenuItems(
  rows: Record<string, unknown>[] | MenuItemDto[],
): MenuItemDto[] {
  return rows
    .map((row): MenuItemDto => {
      if ('menuId' in row && typeof row.menuId === 'number') {
        const item = row as MenuItemDto
        return {
          ...item,
          indPadre: readMenuIndPadre(item.indPadre),
          orden: readNumber(item.orden),
          referencia: readNumber(item.referencia),
        }
      }
      return normalizeMenuItem(row as Record<string, unknown>)
    })
    .filter((i) => i.menuId > 0)
}
