import { describe, expect, it } from 'vitest'
import { previewTexto, recordatorioLabel, tamanoArchivo } from './intranetLabels'

describe('intranetLabels', () => {
  it('recorta el preview sin cortar a medias si cabe', () => {
    expect(previewTexto('Hola', 10)).toBe('Hola')
    expect(previewTexto('abcdefghijklmnop', 8)).toBe('abcdefgh…')
  })

  it('nombra el recordatorio como en Outlook', () => {
    expect(recordatorioLabel(30)).toBe('30 min antes')
    expect(recordatorioLabel(60)).toBe('1 hora antes')
    expect(recordatorioLabel(1440)).toBe('1 día antes')
    expect(recordatorioLabel(null)).toBeNull()
  })

  it('formatea el peso del archivo', () => {
    expect(tamanoArchivo(800)).toBe('800 B')
    expect(tamanoArchivo(2048)).toBe('2 KB')
    expect(tamanoArchivo(2_097_152)).toBe('2.0 MB')
  })
})
