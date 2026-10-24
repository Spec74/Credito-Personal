import type { ResumenCuentaVariant } from './bovedaResumenCuentaParse'
import {
  resumenCuentaLogoSrc,
  type MedioMarcaTone,
} from './resumencuentabrands'
import '../../../styles/medio-marca.css'

export type MedioMarcaSize = 'sm' | 'md' | 'lg'

type BrandLogoProps = {
  variant: ResumenCuentaVariant
  etiqueta: string
  className?: string
  size?: MedioMarcaSize
  tone?: MedioMarcaTone
  eager?: boolean
}

const PX: Record<MedioMarcaSize, number> = { sm: 20, md: 24, lg: 32 }

export function ResumenCuentaBrandLogo({
  variant,
  etiqueta,
  className,
  size = 'md',
  tone = 'mark',
  eager = false,
}: BrandLogoProps) {
  const px = PX[size]
  const logo = tone === 'logo'
  return (
    <span
      className={['medio-marca', `medio-marca--${size}`, `medio-marca--${tone}`, className]
        .filter(Boolean)
        .join(' ')}
      translate="no"
      title={etiqueta}
      aria-label={etiqueta}
    >
      <img
        src={resumenCuentaLogoSrc(variant, tone)}
        alt=""
        width={logo ? undefined : px}
        height={logo ? 28 : px}
        loading={eager ? 'eager' : 'lazy'}
        decoding="async"
      />
    </span>
  )
}
