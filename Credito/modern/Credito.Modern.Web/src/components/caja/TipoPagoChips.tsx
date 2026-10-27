import { ResumenCuentaBrandLogo } from '../../pages/tesoreria/components/resumenCuentaBrandIcons'
import '../../styles/caja-tipo-pago-chips.css'
import {
  medioEtiquetaUi,
  medioNombreMarca,
} from '../../pages/tesoreria/components/bovedaResumenCuentaParse'

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
      translate="no"
    >
      {uniq.map((opt) => {
        const active = value === opt.value
        const { variant, aria } = medioEtiquetaUi(opt.label)
        const nombre = medioNombreMarca(opt.label)
        return (
          <button
            key={opt.value}
            type="button"
            role="radio"
            aria-checked={active}
            aria-label={aria}
            title={aria}
            disabled={disabled}
            className={[
              'caja-tipo-pago-chips__btn',
              active ? 'caja-tipo-pago-chips__btn--on' : '',
            ]
              .filter(Boolean)
              .join(' ')}
            onClick={() => onChange(opt.value)}
          >
            <ResumenCuentaBrandLogo variant={variant} etiqueta={nombre} size="sm" />
            <span translate="no">{nombre}</span>
          </button>
        )
      })}
    </div>
  )
}
