import { apiFetch } from './client'
import type { MenuItemDto } from '../types/api'
import { normalizeMenuItems } from '../utils/normalizeMenu'

export async function fetchMenu(): Promise<MenuItemDto[]> {
  const rows = await apiFetch<Record<string, unknown>[] | MenuItemDto[]>('/menu')
  return normalizeMenuItems(rows)
}
