import type { ResumenCuentaVariant } from './bovedaResumenCuentaParse'
import { resumenCuentaLogoSrc } from './resumencuentabrands'

type BrandLogoProps = {
  variant: ResumenCuentaVariant
  etiqueta: string
  className?: string
  size?: number
}

/**
 * Marca visual por medio de pago (assets en public/assets/resumen-cuenta).
 * Colores y formas alineados a la identidad reconocible de cada entidad en Perú.
 */
export function ResumenCuentaBrandLogo({
  variant,
  etiqueta,
  className,
  size = 48,
}: BrandLogoProps) {
  return (
    <img
      src={resumenCuentaLogoSrc(variant)}
      alt={etiqueta}
      title={etiqueta}
      width={size}
      height={size}
      className={className}
      loading="lazy"
      decoding="async"
    />
  )
}
