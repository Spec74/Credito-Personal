import { describe, expect, it } from 'vitest'
import { horizontalOverflowState } from './useTableHorizontalWheel'

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
