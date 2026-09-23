import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest'
import {
  buildReportVisorUrl,
  isAllowedReportApiPath,
  KNOWN_REPORT_PDF_PATHS,
  stashReportApiPath,
  takeReportApiPath,
} from './reportVisor'

function installMemoryLocalStorage() {
  const store = new Map<string, string>()
  const memory = {
    get length() {
      return store.size
    },
    clear() {
      store.clear()
    },
    getItem(key: string) {
      return store.has(key) ? store.get(key)! : null
    },
    setItem(key: string, value: string) {
      store.set(key, String(value))
    },
    removeItem(key: string) {
      store.delete(key)
    },
    key(index: number) {
      return [...store.keys()][index] ?? null
    },
  }
  vi.stubGlobal('localStorage', memory)
  vi.stubGlobal('window', {
    location: { origin: 'http://localhost:9080' },
  })
  return memory
}

describe('reportVisor', () => {
  beforeEach(() => {
    installMemoryLocalStorage()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('permite solo exports de informes de la API moderna', () => {
    expect(
      isAllowedReportApiPath('/credito/rpt-clientes-inactivos-pdf?oficinaId=1&usuarioId=7'),
    ).toBe(true)
    expect(isAllowedReportApiPath('/almacen/rpt-stock-anulados-pdf')).toBe(true)
    expect(isAllowedReportApiPath('/credito/movimiento-caja-ticket-pdf?oficinaId=1&movimientoCajaId=9')).toBe(
      true,
    )
    expect(isAllowedReportApiPath('/ventas/codigo-barras-lst-pdf?movimientoId=1')).toBe(true)
    expect(isAllowedReportApiPath('/prendario/contrato-pdf?creditoId=1')).toBe(true)
    expect(isAllowedReportApiPath('https://evil.example/credito/x-pdf')).toBe(false)
    expect(isAllowedReportApiPath('/credito/../admin/secret-pdf')).toBe(false)
    expect(isAllowedReportApiPath('/auth/login')).toBe(false)
  })

  it('cubre el catálogo de PDFs de todos los módulos y reporteador admin', () => {
    for (const path of KNOWN_REPORT_PDF_PATHS) {
      expect(isAllowedReportApiPath(path), path).toBe(true)
      expect(isAllowedReportApiPath(`${path}?oficinaId=1&usuarioId=7`), path).toBe(true)
    }
  })

  it('guarda path opaco y no lo expone en la URL del visor', () => {
    const apiPath = '/credito/rpt-clientes-inactivos-pdf?oficinaId=1&usuarioId=7'
    const url = buildReportVisorUrl(apiPath)
    expect(url).toContain('/reportes/visor?rid=')
    expect(url).not.toContain('oficinaId')
    expect(url).not.toContain('usuarioId')
    expect(url).not.toContain('rpt-clientes-inactivos')

    const rid = new URL(url).searchParams.get('rid')
    expect(rid).toBeTruthy()
    expect(takeReportApiPath(rid!)).toBe(apiPath)
  })

  it('rechaza stash de rutas no permitidas', () => {
    expect(() => stashReportApiPath('/auth/refresh')).toThrow(/no permitida/i)
  })

  it('expira entradas viejas', () => {
    vi.spyOn(Date, 'now').mockReturnValue(1_000_000)
    const id = stashReportApiPath('/credito/rpt-cobro-diario-pdf')
    vi.spyOn(Date, 'now').mockReturnValue(1_000_000 + 6 * 60 * 1000)
    expect(takeReportApiPath(id)).toBeNull()
  })
})
