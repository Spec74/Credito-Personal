import { afterEach, describe, expect, it } from 'vitest'
import {
  leerCreditoConsultaRecientes,
  quitarCreditoConsultaReciente,
  recordarCreditoConsulta,
  resetCreditoConsultaRecientes,
} from './creditoConsultaRecientes'

describe('creditoConsultaRecientes', () => {
  afterEach(() => {
    resetCreditoConsultaRecientes()
  })

  it('recuerda el último cliente primero y no duplica', () => {
    recordarCreditoConsulta({ personaId: 1, label: 'Uno', creditoId: 10 })
    recordarCreditoConsulta({ personaId: 2, label: 'Dos' })
    recordarCreditoConsulta({ personaId: 1, label: 'Uno actualizado', creditoId: 11 })

    const rows = leerCreditoConsultaRecientes()
    expect(rows.map((r) => r.personaId)).toEqual([1, 2])
    expect(rows[0]).toMatchObject({ label: 'Uno actualizado', creditoId: 11 })
  })

  it('quita un reciente', () => {
    recordarCreditoConsulta({ personaId: 8, label: 'Ocho' })
    expect(quitarCreditoConsultaReciente(8)).toEqual([])
  })
})
