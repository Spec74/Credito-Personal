import { describe, expect, it } from 'vitest'
import {
  canViewReporteCredito,
  canViewReporteCreditoAdmin,
  canViewReporteCreditoAprobador,
} from './reporteCreditoAccess'

describe('reporteCreditoAccess', () => {
  it('reconoce REPORTEPARCIAL como acceso al índice (paridad MVC ViewBag.rol PARCIAL)', () => {
    expect(canViewReporteCredito(['REPORTEPARCIAL'])).toBe(true)
    expect(canViewReporteCredito(['PARCIAL'])).toBe(true)
  })

  it('gestor/cajero/analista sin rol de reporte no entran al índice', () => {
    expect(canViewReporteCredito(['GESTOR'])).toBe(false)
    expect(canViewReporteCredito(['CAJERO'])).toBe(false)
    expect(canViewReporteCredito(['ANALISTA'])).toBe(false)
  })

  it('ADMIN / ADMINISTRADOR / APROBADOR* tienen acceso', () => {
    expect(canViewReporteCredito(['ADMIN'])).toBe(true)
    expect(canViewReporteCredito(['ADMINISTRADOR'])).toBe(true)
    expect(canViewReporteCredito(['APROBADOR 1'])).toBe(true)
    expect(canViewReporteCreditoAdmin(['ADMINISTRADOR'])).toBe(true)
    expect(canViewReporteCreditoAprobador(['APROBADOR 2'])).toBe(true)
  })
})
