import { describe, expect, it } from 'vitest'
import {
  motivoBloqueoOriginacion,
  montoSobreTope,
  topeCreditoVisible,
} from './clienteOriginacion'

const base = {
  bloqueado: false,
  clienteEstado: true,
  personaEstado: true,
  topeCredito: 2000,
}

describe('clienteOriginacion', () => {
  it('bloquea originación si el cliente está bloqueado o inactivo', () => {
    expect(motivoBloqueoOriginacion({ ...base, bloqueado: true })).toBe('Cliente bloqueado')
    expect(motivoBloqueoOriginacion({ ...base, clienteEstado: false })).toBe('Cliente inactivo')
    expect(motivoBloqueoOriginacion(base)).toBeNull()
  })

  it('muestra tope existente y avisa si el monto lo supera, sin inventar un tope', () => {
    expect(topeCreditoVisible(null)).toBeNull()
    expect(topeCreditoVisible(0)).toBeNull()
    expect(topeCreditoVisible(2000)).toBe(2000)
    expect(montoSobreTope(2500, 2000)).toBe(true)
    expect(montoSobreTope(1500, 2000)).toBe(false)
    expect(montoSobreTope(1500, null)).toBe(false)
  })
})
