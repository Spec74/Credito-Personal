/** @vitest-environment happy-dom */
import { afterEach, describe, expect, it } from 'vitest'
import {
  CREDIX_DENSITY_KEY,
  applyCredixDensity,
  isCredixDensity,
  persistCredixDensity,
  readCredixDensity,
} from './credixDensity'

describe('credixDensity', () => {
  afterEach(() => {
    localStorage.removeItem(CREDIX_DENSITY_KEY)
    document.body.className = ''
  })

  it('acepta solo compact | standard | comfortable', () => {
    expect(isCredixDensity('compact')).toBe(true)
    expect(isCredixDensity('standard')).toBe(true)
    expect(isCredixDensity('comfortable')).toBe(true)
    expect(isCredixDensity('dense')).toBe(false)
    expect(isCredixDensity(null)).toBe(false)
  })

  it('lee standard si no hay preferencia', () => {
    expect(readCredixDensity()).toBe('standard')
  })

  it('persiste y aplica clase en body sin tocar el tamaño de fuente', () => {
    persistCredixDensity('compact')
    expect(localStorage.getItem(CREDIX_DENSITY_KEY)).toBe('compact')
    expect(document.body.classList.contains('credix-density-compact')).toBe(true)
    applyCredixDensity('comfortable')
    expect(document.body.classList.contains('credix-density-compact')).toBe(false)
    expect(document.body.classList.contains('credix-density-comfortable')).toBe(true)
  })
})
