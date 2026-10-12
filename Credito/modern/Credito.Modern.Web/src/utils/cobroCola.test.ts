import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  addCobroPendiente,
  clearCobroColaDia,
  clearCobroColaError,
  isCreditoEnviadoHoy,
  isCreditoPendiente,
  loadCobroCola,
  markCobroEnviados,
  removeCobroPendiente,
  setCobroColaError,
} from './cobroCola'
import { fechaOperacionLocal } from './cobroBloqueDraft'

function installMemoryStorage() {
  const store = new Map<string, string>()
  vi.stubGlobal('localStorage', {
    get length() {
      return store.size
    },
    clear: () => store.clear(),
    getItem: (k: string) => store.get(k) ?? null,
    setItem: (k: string, v: string) => {
      store.set(k, String(v))
    },
    removeItem: (k: string) => {
      store.delete(k)
    },
    key: (i: number) => [...store.keys()][i] ?? null,
  } satisfies Storage)
}

const CTX = { usuarioId: 7, oficinaId: 1, cajaDiarioId: 40 }

describe('cobroCola', () => {
  beforeEach(() => {
    installMemoryStorage()
    clearCobroColaDia({ usuarioId: CTX.usuarioId, cajaDiarioId: CTX.cajaDiarioId })
  })
  afterEach(() => vi.unstubAllGlobals())

  it('membresía pendiente sobrevive sin depender del monto', () => {
    const cola = addCobroPendiente(CTX, 55)
    expect(isCreditoPendiente(cola, 55)).toBe(true)
    expect(loadCobroCola(CTX).pendientes).toEqual([55])
    const still = removeCobroPendiente(CTX, 99)
    expect(still.pendientes).toEqual([55])
    const gone = removeCobroPendiente(CTX, 55)
    expect(gone.pendientes).toEqual([])
  })

  it('persiste enviados del día, limpia pendiente y error', () => {
    addCobroPendiente(CTX, 100)
    setCobroColaError(CTX, 100, 'falló')
    const cola = markCobroEnviados(CTX, [
      {
        creditoId: 100,
        personaNombre: 'Ana',
        montoPagar: 25,
        tipoPagoId: 1,
        origen: 'lote',
      },
    ])
    expect(cola.fechaOperacion).toBe(fechaOperacionLocal())
    expect(cola.enviados).toHaveLength(1)
    expect(cola.pendientes).toEqual([])
    expect(cola.errores['100']).toBeUndefined()
    expect(isCreditoEnviadoHoy(cola, 100)).toBe(true)
  })

  it('carga vacía si caja/usuario inválidos', () => {
    const cola = loadCobroCola({ usuarioId: 0, oficinaId: 1, cajaDiarioId: 0 })
    expect(cola.enviados).toEqual([])
    expect(cola.pendientes).toEqual([])
  })

  it('clearCobroColaError quita solo ese crédito', () => {
    setCobroColaError(CTX, 11, 'a')
    setCobroColaError(CTX, 22, 'b')
    const mid = loadCobroCola(CTX)
    expect(mid.errores['11']).toBe('a')
    expect(mid.errores['22']).toBe('b')
    const cola = clearCobroColaError(CTX, 11)
    expect(cola.errores['11']).toBeUndefined()
    expect(cola.errores['22']).toBe('b')
  })
})
