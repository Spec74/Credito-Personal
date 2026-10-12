import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  clearCobroDraftCreditos,
  removeCreditosFromPendingProcess,
  syncDiarioPagoHaciaCobro,
} from './cajaOperacionSync'
import {
  clearCobroColaDia,
  isCreditoEnviadoHoy,
  loadCobroCola,
} from './cobroCola'
import {
  clearCobroBloqueDraft,
  loadCobroBloqueDraft,
  saveCobroBloqueDraft,
  fechaOperacionLocal,
} from './cobroBloqueDraft'
import {
  clearCobroBloquePendingProcess,
  enqueueCobroBloqueProcess,
  loadCobroBloquePendingProcess,
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

const CTX = { usuarioId: 7, oficinaId: 1, cajaDiarioId: 40 }

describe('cajaOperacionSync', () => {
  beforeEach(() => {
    installMemoryStorage()
    clearCobroColaDia({ usuarioId: CTX.usuarioId, cajaDiarioId: CTX.cajaDiarioId })
    clearCobroBloqueDraft(CTX)
    clearCobroBloquePendingProcess(CTX)
  })
  afterEach(() => vi.unstubAllGlobals())

  it('Diario → Enviados + limpia borrador y pending offline', () => {
    saveCobroBloqueDraft({
      version: 1,
      ...CTX,
      fechaOperacion: fechaOperacionLocal(),
      updatedAt: new Date().toISOString(),
      filtro: '',
      edits: {
        '55': {
          montoPagar: 20,
          tipoPagoId: 1,
          fechaHoraTrans: '',
          cuotasSeleccionadas: [],
        },
      },
    })
    enqueueCobroBloqueProcess({
      version: 1,
      ...CTX,
      fechaOperacion: fechaOperacionLocal(),
      queuedAt: new Date().toISOString(),
      conCobro: 1,
      total: 20,
      planilla: [{ creditoId: 55, montoPagar: 20, tipoPagoId: 1 }],
    })

    syncDiarioPagoHaciaCobro({
      ...CTX,
      creditoId: 55,
      personaNombre: 'Ana',
      montoPagar: 20,
      tipoPagoId: 1,
      movimientoCajaId: 9001,
    })

    const cola = loadCobroCola(CTX)
    expect(isCreditoEnviadoHoy(cola, 55)).toBe(true)
    expect(cola.enviados[0]?.origen).toBe('diario')
    expect(cola.enviados[0]?.movimientoCajaId).toBe(9001)

    const { draft } = loadCobroBloqueDraft(CTX)
    expect(draft?.edits['55']).toBeUndefined()
    expect(loadCobroBloquePendingProcess(CTX)).toBeNull()
  })

  it('clearCobroDraftCreditos solo quita ids pedidos', () => {
    saveCobroBloqueDraft({
      version: 1,
      ...CTX,
      fechaOperacion: fechaOperacionLocal(),
      updatedAt: new Date().toISOString(),
      filtro: '',
      edits: {
        '1': {
          montoPagar: 10,
          tipoPagoId: 1,
          fechaHoraTrans: '',
          cuotasSeleccionadas: [],
        },
        '2': {
          montoPagar: 15,
          tipoPagoId: 1,
          fechaHoraTrans: '',
          cuotasSeleccionadas: [],
        },
      },
    })
    clearCobroDraftCreditos(CTX, [1])
    const { draft } = loadCobroBloqueDraft(CTX)
    expect(draft?.edits['1']).toBeUndefined()
    expect(draft?.edits['2']?.montoPagar).toBe(15)
  })

  it('removeCreditosFromPendingProcess deja el resto', () => {
    enqueueCobroBloqueProcess({
      version: 1,
      ...CTX,
      fechaOperacion: fechaOperacionLocal(),
      queuedAt: new Date().toISOString(),
      conCobro: 2,
      total: 30,
      planilla: [
        { creditoId: 1, montoPagar: 10, tipoPagoId: 1 },
        { creditoId: 2, montoPagar: 20, tipoPagoId: 1 },
      ],
    })
    removeCreditosFromPendingProcess(CTX, [1])
    const pending = loadCobroBloquePendingProcess(CTX)
    expect(pending?.planilla).toEqual([
      { creditoId: 2, montoPagar: 20, tipoPagoId: 1 },
    ])
  })
})
