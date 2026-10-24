/** @vitest-environment happy-dom */
import { afterEach, describe, expect, it } from 'vitest'
import {
  applyCredixDensity,
  resolveCredixDensity,
} from './credixDensity'

describe('credixDensity', () => {
  afterEach(() => {
    document.body.className = ''
  })

  it('mostrador: ratón y ancho de escritorio → compacto', () => {
    expect(resolveCredixDensity({ coarse: false, width: 1280 })).toBe('compact')
  })

  it('teléfono o táctil → holgado (40–48 px)', () => {
    expect(resolveCredixDensity({ coarse: true, width: 1280 })).toBe('comfortable')
    expect(resolveCredixDensity({ coarse: false, width: 390 })).toBe('comfortable')
  })

  it('aplica clase en body sin tocar el tamaño de fuente', () => {
    applyCredixDensity('compact')
    expect(document.body.classList.contains('credix-density-compact')).toBe(true)
    applyCredixDensity('comfortable')
    expect(document.body.classList.contains('credix-density-compact')).toBe(false)
    expect(document.body.classList.contains('credix-density-comfortable')).toBe(true)
  })
})
