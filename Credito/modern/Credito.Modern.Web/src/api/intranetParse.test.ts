import { describe, expect, it } from 'vitest'
import { archivosDeUpload, idDeRespuesta } from './intranetParse'

describe('idDeRespuesta', () => {
  it('lee camelCase y PascalCase', () => {
    expect(idDeRespuesta({ id: 12 })).toBe(12)
    expect(idDeRespuesta({ Id: 7 })).toBe(7)
  })

  it('rechaza vacío o no numérico', () => {
    expect(idDeRespuesta(undefined)).toBe(0)
    expect(idDeRespuesta({})).toBe(0)
    expect(idDeRespuesta({ id: 0 })).toBe(0)
  })
})

describe('archivosDeUpload', () => {
  it('toma originFileObj File y omite huecos', () => {
    const pdf = new File([new Uint8Array([1, 2, 3])], 'aviso.pdf', { type: 'application/pdf' })
    expect(archivosDeUpload([{ originFileObj: pdf }, {}])).toEqual([pdf])
    expect(archivosDeUpload(undefined)).toEqual([])
  })
})
