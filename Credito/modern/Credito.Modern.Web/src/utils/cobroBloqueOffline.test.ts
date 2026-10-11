import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fechaOperacionLocal } from './cobroBloqueDraft'
import {
  clearCobroBloquePendingProcess,
  enqueueCobroBloqueProcess,
  findCobroBloqueOfflineSession,
  isLikelyNetworkError,
  loadCobroBloqueCarteraCache,
  loadCobroBloquePendingProcess,
  saveCobroBloqueCarteraCache,
} from './cobroBloqueOffline'

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

describe('cobroBloqueOffline', () => {
  beforeEach(() => {
    installMemoryStorage()
    vi.stubGlobal('navigator', { onLine: true })
  })
  afterEach(() => vi.unstubAllGlobals())

  const sessionSnap = {
    oficinaId: 1,
    cajaDiarioId: 10,
    cajaId: 7,
    cajaDenominacion: 'Caja 1',
    fechaIniOperacion: '2026-10-10T08:00:00',
    saldoInicial: 100,
    entradas: 0,
    salidas: 0,
    saldoFinal: 100,
    indCierre: false,
    esCajaCentral: false,
  }

  it('cachea y recupera planilla del día (incl. GPS)', () => {
    saveCobroBloqueCarteraCache({
      version: 1,
      usuarioId: 3,
      oficinaId: 1,
      cajaDiarioId: 10,
      fechaOperacion: fechaOperacionLocal(),
      cachedAt: new Date().toISOString(),
      session: sessionSnap,
      rows: [
        {
          creditoId: 1,
          personaCodigo: 'PE1',
          personaNombre: 'Ana',
          montoCredito: 100,
          personaId: 9,
          fechaVencimiento: '2026-10-10',
          importeMora: 0,
          deudaPendiente: 50,
          orden: 1,
          celular: null,
          direccion: 'Jr. Lima',
          latitud: -13.16,
          longitud: -74.22,
          cuotaSugerida: 25,
          diasAtrazo: 0,
        },
      ],
    })
    const cache = loadCobroBloqueCarteraCache({ usuarioId: 3, cajaDiarioId: 10 })
    expect(cache?.rows).toHaveLength(1)
    expect(cache?.rows[0]?.personaNombre).toBe('Ana')
    expect(cache?.rows[0]?.latitud).toBe(-13.16)
    expect(findCobroBloqueOfflineSession(3)?.cajaDiarioId).toBe(10)
  })

  it('purga caché legacy v2 al guardar v3', () => {
    const fecha = fechaOperacionLocal()
    localStorage.setItem(
      `credix.cobroBloqueCartera.v2:3:10:${fecha}`,
      JSON.stringify({ version: 1, session: sessionSnap, rows: [] }),
    )
    saveCobroBloqueCarteraCache({
      version: 1,
      usuarioId: 3,
      oficinaId: 1,
      cajaDiarioId: 10,
      fechaOperacion: fecha,
      cachedAt: new Date().toISOString(),
      session: sessionSnap,
      rows: [],
    })
    expect(localStorage.getItem(`credix.cobroBloqueCartera.v2:3:10:${fecha}`)).toBeNull()
    expect(
      localStorage.getItem(`credix.cobroBloqueCartera.v3:3:10:${fecha}`),
    ).not.toBeNull()
  })

  it('encola y limpia proceso pendiente', () => {
    enqueueCobroBloqueProcess({
      version: 1,
      usuarioId: 3,
      oficinaId: 1,
      cajaDiarioId: 10,
      fechaOperacion: fechaOperacionLocal(),
      queuedAt: new Date().toISOString(),
      conCobro: 1,
      total: 25,
      planilla: [{ creditoId: 1, montoPagar: 25, tipoPagoId: 1, fechaHoraTrans: null }],
    })
    expect(loadCobroBloquePendingProcess({ usuarioId: 3, cajaDiarioId: 10 })?.conCobro).toBe(1)
    clearCobroBloquePendingProcess({ usuarioId: 3, cajaDiarioId: 10 })
    expect(loadCobroBloquePendingProcess({ usuarioId: 3, cajaDiarioId: 10 })).toBeNull()
  })

  it('detecta errores de red', () => {
    expect(isLikelyNetworkError(new TypeError('Failed to fetch'))).toBe(true)
    vi.stubGlobal('navigator', { onLine: false })
    expect(isLikelyNetworkError(new Error('x'))).toBe(true)
  })

  it('no lee ni purga caché con cajaDiarioId inválido', () => {
    saveCobroBloqueCarteraCache({
      version: 1,
      usuarioId: 3,
      oficinaId: 1,
      cajaDiarioId: 10,
      fechaOperacion: fechaOperacionLocal(),
      cachedAt: new Date().toISOString(),
      session: null,
      rows: [
        {
          creditoId: 1,
          personaCodigo: 'PE1',
          personaNombre: 'Ana',
          montoCredito: 100,
          personaId: 9,
          fechaVencimiento: '2026-10-10',
          importeMora: 0,
          deudaPendiente: 50,
          orden: 1,
          celular: null,
          direccion: null,
          latitud: null,
          longitud: null,
          cuotaSugerida: 25,
          diasAtrazo: 0,
        },
      ],
    })
    expect(loadCobroBloqueCarteraCache({ usuarioId: 3, cajaDiarioId: 0 })).toBeNull()
    expect(loadCobroBloqueCarteraCache({ usuarioId: 3, cajaDiarioId: 10 })?.rows).toHaveLength(1)
  })
})
