import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  fetchSaldosCajaChicaDiario,
  fetchSaldosCajaDiario,
  fetchSaldosCajaDiarioBoveda,
} from './saldosCaja'

const apiFetch = vi.hoisted(() => vi.fn())

vi.mock('./client', () => ({ apiFetch }))

function urlLlamada(): URL {
  return new URL(apiFetch.mock.calls[0][0] as string, 'https://credix.test/api/v1')
}

describe('saldosCaja: paginación en servidor', () => {
  beforeEach(() => {
    apiFetch.mockReset()
    apiFetch.mockResolvedValue({ rows: [], totalRecords: 0 })
  })

  it('usa página 1 y 25 registros por defecto', async () => {
    await fetchSaldosCajaDiario(7)
    const url = urlLlamada()
    expect(url.searchParams.get('oficinaId')).toBe('7')
    expect(url.searchParams.get('page')).toBe('1')
    expect(url.searchParams.get('pageSize')).toBe('25')
    expect(url.searchParams.has('buscar')).toBe(false)
  })

  it('propaga página, tamaño y término de búsqueda', async () => {
    await fetchSaldosCajaDiario(7, { page: 3, pageSize: 50, buscar: 'caja 01' })
    const url = urlLlamada()
    expect(url.searchParams.get('page')).toBe('3')
    expect(url.searchParams.get('pageSize')).toBe('50')
    expect(url.searchParams.get('buscar')).toBe('caja 01')
  })

  it('omite búsquedas en blanco para no invalidar la caché sin motivo', async () => {
    await fetchSaldosCajaChicaDiario({ buscar: '   ' })
    expect(urlLlamada().searchParams.has('buscar')).toBe(false)
  })

  it('bóveda viaja con oficina, bóveda y paginación', async () => {
    await fetchSaldosCajaDiarioBoveda(7, 42, { page: 2, pageSize: 10 })
    const url = urlLlamada()
    expect(url.searchParams.get('oficinaId')).toBe('7')
    expect(url.searchParams.get('bovedaId')).toBe('42')
    expect(url.searchParams.get('page')).toBe('2')
    expect(url.searchParams.get('pageSize')).toBe('10')
  })
})
