type Opt = { value: number; label: string }

function shortLabel(label: string): string {
  const t = label.trim()
  if (t.length <= 10) return t
  const first = t.split(/\s+/)[0] ?? t
  return first.length <= 12 ? first : `${first.slice(0, 10)}…`
}

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

/** Selector táctil de forma de pago (efectivo / Yape / Plin…). */
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
  /** En PC: varias filas en lugar de scroll horizontal infinito. */
  wrap?: boolean
}) {
  const uniq = dedupeOptions(options)
  return (
    <div
      className={[
        'caja-tipo-pago-chips',
        wrap ? 'caja-tipo-pago-chips--wrap' : '',
      ]
        .filter(Boolean)
        .join(' ')}
      role="radiogroup"
      aria-label="Forma de pago"
    >
      {uniq.map((opt) => {
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
