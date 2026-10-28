import { branding } from '../../config/branding'

interface BrandLogoProps {
  compact?: boolean
  showTagline?: boolean
  className?: string
  /** Sobre azul de marca: placa blanca ceñida. El lockup no se invierte. */
  onDark?: boolean
  priority?: boolean
}

/** Arte Corel recortado al lockup (PNG con alpha). */
const LOGO_NATIVE_W = 250
const LOGO_NATIVE_H = 91

/**
 * Lockup oficial de Inversiones CrediConfiable.
 * No se redibuja en SVG: el isotipo inventado parecía otra empresa.
 */
export function BrandLogo({
  compact = false,
  showTagline = false,
  className,
  onDark = false,
  priority = false,
}: BrandLogoProps) {
  const width = compact ? 148 : onDark ? 440 : branding.logoWidth
  const height = Math.round((width * LOGO_NATIVE_H) / LOGO_NATIVE_W)
  const rootClass = [
    'credix-brand-logo',
    compact ? 'is-compact' : '',
    onDark ? 'credix-brand-logo--on-dark' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={rootClass}>
      <div className="credix-brand-logo__plate">
        <img
          className="credix-brand-logo__img"
          src={branding.logoSrc}
          srcSet={`${branding.logoSrc} 1x, ${branding.logoSrc3x} 3x`}
          sizes={`${width}px`}
          alt={`${branding.companyLine1} ${branding.companyName}`}
          width={width}
          height={height}
          decoding="async"
          loading={priority ? 'eager' : 'lazy'}
          fetchPriority={priority ? 'high' : 'low'}
        />
      </div>
      {showTagline && !compact ? (
        <span className="credix-brand-logo__tagline">{branding.tagline}</span>
      ) : null}
    </div>
  )
}
