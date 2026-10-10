type Opt = { value: number; label: string }

function shortLabel(label: string): string {
  const t = label.trim()
  if (t.length <= 10) return t
  const first = t.split(/\s+/)[0] ?? t
  return first.length <= 12 ? first : `${first.slice(0, 10)}…`
}

/** Selector táctil de forma de pago (efectivo / Yape / Plin…). */
export function TipoPagoChips({
  value,
  options,
  onChange,
  disabled,
}: {
  value: number
  options: Opt[]
  onChange: (id: number) => void
  disabled?: boolean
}) {
  return (
    <div className="caja-tipo-pago-chips" role="radiogroup" aria-label="Forma de pago">
      {options.map((opt) => {
        const active = value === opt.value
        return (
          <button
            key={opt.value}
            type="button"
            role="radio"
            aria-checked={active}
            disabled={disabled}
            className={[
              'caja-tipo-pago-chips__btn',
              active ? 'caja-tipo-pago-chips__btn--on' : '',
            ]
              .filter(Boolean)
              .join(' ')}
            onClick={() => onChange(opt.value)}
          >
            {shortLabel(opt.label)}
          </button>
        )
      })}
    </div>
  )
}
