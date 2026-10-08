/** @vitest-environment happy-dom */
import { afterEach, describe, expect, it } from 'vitest'
import {
  clearTokens,
  getAccessToken,
  getRefreshToken,
  isRefreshPersisted,
  saveTokens,
} from './tokenStorage'

describe('tokenStorage', () => {
  afterEach(() => {
    clearTokens()
  })

  it('guarda access en localStorage (visor multi-pestaña) y refresh en session por defecto', () => {
    saveTokens('access-a', 'refresh-a', 3600, false)
    expect(getAccessToken()).toBe('access-a')
    expect(localStorage.getItem('credito.access')).toBe('access-a')
    expect(getRefreshToken()).toBe('refresh-a')
    expect(sessionStorage.getItem('credito.refresh')).toBe('refresh-a')
    expect(localStorage.getItem('credito.refresh')).toBeNull()
    expect(isRefreshPersisted()).toBe(false)
  })

  it('persiste refresh en localStorage solo con Recordar sesión', () => {
    saveTokens('access-b', 'refresh-b', 3600, true)
    expect(getRefreshToken()).toBe('refresh-b')
    expect(localStorage.getItem('credito.refresh')).toBe('refresh-b')
    expect(sessionStorage.getItem('credito.refresh')).toBeNull()
    expect(isRefreshPersisted()).toBe(true)
  })

  it('clearTokens limpia memoria y ambos storages', () => {
    saveTokens('access-c', 'refresh-c', 3600, true)
    clearTokens()
    expect(getAccessToken()).toBeNull()
    expect(getRefreshToken()).toBeNull()
  })
})
