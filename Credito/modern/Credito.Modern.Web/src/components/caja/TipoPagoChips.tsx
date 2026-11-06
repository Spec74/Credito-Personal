import { useCallback, useId, useRef, type KeyboardEvent } from 'react'
import { ResumenCuentaBrandLogo } from '../../pages/tesoreria/components/resumenCuentaBrandIcons'
import '../../styles/caja-tipo-pago-chips.css'
import {
  medioEtiquetaUi,
  medioNombreMarca,
} from '../../pages/tesoreria/components/bovedaResumenCuentaParse'
import {
  marcaDesdeTipoPagoId,
  medioNombrePorItemId,
} from '../../utils/tipoPago'

type Opt = { value: number; label: string }

function dedupeOptions(options: Opt[]): Opt[] {
  const seen = new Set<number>()
  const out: Opt[] = []
  for (const opt of options) {
    if (!Number.isFinite(opt.value) || seen.has(opt.value)) continue
    seen.add(opt.value)
    out.push(opt)
  }
  return out
}

/** Selector táctil de forma de pago (efectivo, Yape, Plin, BCP, Interbank, Nación…). */
export function TipoPagoChips({
  value,
  options,
  onChange,
  disabled,
  wrap = false,
}: {
  value: number
  options: Opt[]
  onChange: (id: number) => void
  disabled?: boolean
  /** En PC: varias filas en lugar de scroll horizontal. En móvil el CSS también apila. */
  wrap?: boolean
}) {
  const uniq = dedupeOptions(options)
  const groupId = useId()
  const btnRefs = useRef<(HTMLButtonElement | null)[]>([])

  const focusAt = useCallback((index: number) => {
    const el = btnRefs.current[index]
    el?.focus()
  }, [])

  const onKeyDown = useCallback(
    (e: KeyboardEvent<HTMLDivElement>) => {
      if (disabled || uniq.length === 0) return
      const keys = ['ArrowRight', 'ArrowLeft', 'ArrowDown', 'ArrowUp', 'Home', 'End']
      if (!keys.includes(e.key)) return
      e.preventDefault()
      const found = uniq.findIndex((o) => o.value === value)
      const i = found >= 0 ? found : 0
      let next = i
      if (e.key === 'ArrowRight' || e.key === 'ArrowDown') next = (i + 1) % uniq.length
      else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp')
        next = (i - 1 + uniq.length) % uniq.length
      else if (e.key === 'Home') next = 0
      else if (e.key === 'End') next = uniq.length - 1
      onChange(uniq[next].value)
      focusAt(next)
    },
    [disabled, uniq, value, onChange, focusAt],
  )

  const activeIndex = uniq.findIndex((o) => o.value === value)

  return (
    <div
      id={groupId}
      className={[
        'caja-tipo-pago-chips',
        wrap ? 'caja-tipo-pago-chips--wrap' : '',
      ]
        .filter(Boolean)
        .join(' ')}
      role="radiogroup"
      aria-label="Forma de pago"
      translate="no"
      onKeyDown={onKeyDown}
    >
      {uniq.map((opt, index) => {
        const active = value === opt.value
        const nombre = medioNombrePorItemId(opt.value) ?? medioNombreMarca(opt.label)
        const fromTexto = medioEtiquetaUi(nombre)
        const variant = marcaDesdeTipoPagoId(opt.value) ?? fromTexto.variant
        const tabStop = active || (activeIndex < 0 && index === 0)
        return (
          <button
            key={opt.value}
            ref={(el) => {
              btnRefs.current[index] = el
            }}
            type="button"
            role="radio"
            aria-checked={active}
            aria-label={nombre}
            title={nombre}
            tabIndex={tabStop ? 0 : -1}
            disabled={disabled}
            className={[
              'caja-tipo-pago-chips__btn',
              active ? 'caja-tipo-pago-chips__btn--on' : '',
            ]
              .filter(Boolean)
              .join(' ')}
            onClick={() => onChange(opt.value)}
          >
            <ResumenCuentaBrandLogo variant={variant} etiqueta={nombre} size="sm" tone="mark" />
            <span translate="no">{nombre}</span>
          </button>
        )
      })}
    </div>
  )
}
