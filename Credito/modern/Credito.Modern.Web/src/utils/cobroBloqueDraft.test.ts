import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  clearCobroBloqueDraft,
  draftHasMontos,
  fechaOperacionLocal,
  loadCobroBloqueDraft,
  purgeStaleCobroBloqueDrafts,
  saveCobroBloqueDraft,
  type CobroBloqueDraft,
} from './cobroBloqueDraft'

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

function sampleDraft(over: Partial<CobroBloqueDraft> = {}): CobroBloqueDraft {
  return {
    version: 1,
    usuarioId: 7,
    oficinaId: 1,
    cajaDiarioId: 99,
    fechaOperacion: fechaOperacionLocal(),
    updatedAt: new Date().toISOString(),
    filtro: '',
    edits: {
      '100': {
        montoPagar: 50,
        tipoPagoId: 1,
        fechaHoraTrans: '2026-10-10T10:00',
        cuotasSeleccionadas: [],
      },
    },
    ...over,
  }
}

describe('cobroBloqueDraft', () => {
  beforeEach(() => installMemoryStorage())
  afterEach(() => vi.unstubAllGlobals())

  it('guarda y recupera borrador del día', () => {
    saveCobroBloqueDraft(sampleDraft())
    const { draft, discardedStale } = loadCobroBloqueDraft({
      usuarioId: 7,
      oficinaId: 1,
      cajaDiarioId: 99,
    })
    expect(discardedStale).toBe(false)
    expect(draft?.edits['100']?.montoPagar).toBe(50)
    expect(draftHasMontos(draft)).toBe(true)
  })

  it('purga borradores de otro día y reporta discardedStale', () => {
    localStorage.setItem(
      'credix.cobroBloqueDraft.v1:7:99:2000-01-01',
      JSON.stringify(sampleDraft({ fechaOperacion: '2000-01-01' })),
    )
    const { draft, discardedStale } = loadCobroBloqueDraft({
      usuarioId: 7,
      oficinaId: 1,
      cajaDiarioId: 99,
    })
    expect(draft).toBeNull()
    expect(discardedStale).toBe(true)
    expect(purgeStaleCobroBloqueDrafts()).toBe(0)
  })

  it('clear elimina el borrador vigente', () => {
    saveCobroBloqueDraft(sampleDraft())
    clearCobroBloqueDraft({ usuarioId: 7, cajaDiarioId: 99 })
    const { draft } = loadCobroBloqueDraft({
      usuarioId: 7,
      oficinaId: 1,
      cajaDiarioId: 99,
    })
    expect(draft).toBeNull()
  })
})
