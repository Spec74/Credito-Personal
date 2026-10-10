import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  cobroBloqueHoyKey,
  isCobroBloqueEjecutadoHoy,
  markCobroBloqueEjecutadoHoy,
} from './cobroBloqueDayLock'

function installMemoryStorage() {
  const store = new Map<string, string>()
  const storage: Storage = {
    get length() {
      return store.size
    },
    clear: () => store.clear(),
    getItem: (k) => store.get(k) ?? null,
    setItem: (k, v) => {
      store.set(k, String(v))
    },
    removeItem: (k) => {
      store.delete(k)
    },
    key: (i) => [...store.keys()][i] ?? null,
  }
  vi.stubGlobal('localStorage', storage)
}

describe('cobroBloqueDayLock', () => {
  beforeEach(() => {
    installMemoryStorage()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('marca y detecta ejecución del día', () => {
    expect(isCobroBloqueEjecutadoHoy()).toBe(false)
    markCobroBloqueEjecutadoHoy()
    expect(isCobroBloqueEjecutadoHoy()).toBe(true)
    expect(localStorage.getItem('cobroBloqueEjecutado')).toBe(cobroBloqueHoyKey())
  })

  it('no bloquea si la fecha guardada es de otro día', () => {
    localStorage.setItem('cobroBloqueEjecutado', '1/1/2000')
    expect(isCobroBloqueEjecutadoHoy()).toBe(false)
  })
})
