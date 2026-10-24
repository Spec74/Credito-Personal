import { describe, expect, it } from 'vitest'
import {
  horizontalOverflowState,
  resolveHorizontalWheelDelta,
  shouldForwardVerticalWheel,
  visibleBandMidOffset,
} from './useTableHorizontalWheel'

describe('horizontalOverflowState', () => {
  it('sin desborde no muestra fades', () => {
    expect(horizontalOverflowState(0, 800, 800)).toEqual({
      canScrollLeft: false,
      canScrollRight: false,
    })
  })

  it('al inicio solo avisa que hay más a la derecha', () => {
    expect(horizontalOverflowState(0, 1400, 800)).toEqual({
      canScrollLeft: false,
      canScrollRight: true,
    })
  })

  it('al final solo avisa que hay más a la izquierda', () => {
    expect(horizontalOverflowState(600, 1400, 800)).toEqual({
      canScrollLeft: true,
      canScrollRight: false,
    })
  })

  it('en el medio avisa ambos lados', () => {
    expect(horizontalOverflowState(200, 1400, 800)).toEqual({
      canScrollLeft: true,
      canScrollRight: true,
    })
  })
})

describe('resolveHorizontalWheelDelta', () => {
  it('la rueda vertical no se convierte en scroll de columnas', () => {
    expect(resolveHorizontalWheelDelta({ deltaX: 0, deltaY: 120, shiftKey: false })).toBeNull()
  })

  it('Mayús + rueda mueve columnas', () => {
    expect(resolveHorizontalWheelDelta({ deltaX: 0, deltaY: 80, shiftKey: true })).toBe(80)
  })

  it('el gesto horizontal del trackpad se respeta', () => {
    expect(resolveHorizontalWheelDelta({ deltaX: 40, deltaY: 8, shiftKey: false })).toBe(40)
  })
})

describe('shouldForwardVerticalWheel', () => {
  it('si la tabla no tiene scroll vertical, la rueda baja la página', () => {
    expect(shouldForwardVerticalWheel(400, 400)).toBe(true)
  })

  it('si la tabla sí tiene scroll vertical, no se reenvía', () => {
    expect(shouldForwardVerticalWheel(900, 400)).toBe(false)
  })
})

describe('visibleBandMidOffset', () => {
  it('con la tabla entera a la vista usa el centro visible', () => {
    expect(
      visibleBandMidOffset({
        bandTop: 100,
        bandHeight: 400,
        viewTop: 0,
        viewBottom: 800,
        buttonSize: 40,
      }),
    ).toBe(180)
  })

  it('si la tabla es más alta que la pantalla, sigue el tramo visible', () => {
    expect(
      visibleBandMidOffset({
        bandTop: -600,
        bandHeight: 2000,
        viewTop: 80,
        viewBottom: 800,
        buttonSize: 40,
      }),
    ).toBe(1020)
  })

  it('si casi no se ve la tabla, no dibuja flechas', () => {
    expect(
      visibleBandMidOffset({
        bandTop: 780,
        bandHeight: 400,
        viewTop: 0,
        viewBottom: 800,
        buttonSize: 40,
      }),
    ).toBeNull()
  })
})
