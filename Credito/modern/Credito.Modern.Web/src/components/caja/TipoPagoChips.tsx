import { ResumenCuentaBrandLogo } from '../../pages/tesoreria/components/resumenCuentaBrandIcons'
import '../../styles/caja-tipo-pago-chips.css'
import {
  medioEtiquetaUi,
  medioNombreMarca,
  medioNombrePorItemId,
} from '../../pages/tesoreria/components/bovedaResumenCuentaParse'
import { marcaDesdeTipoPagoId } from '../../pages/tesoreria/components/resumencuentabrands'

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
        const nombre = medioNombrePorItemId(opt.value) ?? medioNombreMarca(opt.label)
        const fromTexto = medioEtiquetaUi(nombre)
        const variant = marcaDesdeTipoPagoId(opt.value) ?? fromTexto.variant
        return (
          <button
            key={opt.value}
            type="button"
            role="radio"
            aria-checked={active}
            aria-label={nombre}
            title={nombre}
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
