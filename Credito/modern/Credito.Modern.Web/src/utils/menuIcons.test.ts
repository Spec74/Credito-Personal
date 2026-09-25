import { describe, expect, it } from 'vitest'
import { createElement } from 'react'
import {
  BankOutlined,
  CalculatorOutlined,
  CheckCircleOutlined,
  EnvironmentOutlined,
  EyeOutlined,
  GoldOutlined,
  ScheduleOutlined,
  SolutionOutlined,
  TeamOutlined,
  WalletOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { menuItemIcon, parentMenuIcon } from './menuIcons'

function iconType(node: ReturnType<typeof menuItemIcon>) {
  return (node as { type?: unknown })?.type
}

describe('menuIcons — iconos por función (no herencia del módulo)', () => {
  it('hijos de CREDITO usan icono distinto según denominación', () => {
    const modulo = 'CREDITO'
    expect(iconType(menuItemIcon(null, 'Simulador', modulo))).toBe(CalculatorOutlined)
    expect(iconType(menuItemIcon(null, 'Caja Diario', modulo))).toBe(WalletOutlined)
    expect(iconType(menuItemIcon(null, 'Cliente', modulo))).toBe(TeamOutlined)
    expect(iconType(menuItemIcon(null, 'Creditos', modulo))).toBe(SolutionOutlined)
    expect(iconType(menuItemIcon(null, 'Credito Prendario', modulo))).toBe(GoldOutlined)
    expect(iconType(menuItemIcon(null, 'Aprobar', modulo))).toBe(CheckCircleOutlined)
    expect(iconType(menuItemIcon(null, 'Tareas', modulo))).toBe(ScheduleOutlined)
    expect(iconType(menuItemIcon(null, 'Boveda', modulo))).toBe(BankOutlined)
  })

  it('reportes de cartera no comparten el mismo icono genérico', () => {
    expect(iconType(menuItemIcon(null, 'Observados', 'REPORTES'))).toBe(EyeOutlined)
    expect(iconType(menuItemIcon(null, 'Vencidos', 'REPORTES'))).toBe(WarningOutlined)
    expect(iconType(menuItemIcon(null, 'Cobro diario', 'REPORTES'))).toBe(
      EnvironmentOutlined,
    )
  })

  it('padre CREDITO mantiene icono de módulo (banco)', () => {
    expect(iconType(parentMenuIcon('Credito'))).toBe(BankOutlined)
  })

  it('icono legacy genérico (list) cede a la denominación', () => {
    expect(iconType(menuItemIcon('icon-list', 'Simulador', 'CREDITO'))).toBe(
      CalculatorOutlined,
    )
  })

  it('smoke: createElement no rompe con nodos de icono', () => {
    const node = menuItemIcon(null, 'Caja Chica', 'CAJA')
    expect(createElement('span', null, node)).toBeTruthy()
  })
})
