import { apiFetch } from './client'
import type { ProductoListItem } from '../types/api'
import { normalizeProductos } from './normalize'

export function fetchProductos(): Promise<ProductoListItem[]> {
  return apiFetch<unknown>('/productos').then(normalizeProductos)
}
