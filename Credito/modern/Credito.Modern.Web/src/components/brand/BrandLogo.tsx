import { Typography } from 'antd'
import { branding } from '../../config/branding'

const { Text } = Typography

interface BrandLogoProps {
  compact?: boolean
  showTagline?: boolean
  className?: string
}

/** Recorte del PNG con alpha: 299×102. */
const LOGO_NATIVE_W = 299
const LOGO_NATIVE_H = 102

export function BrandLogo({
  compact = false,
  showTagline = false,
  className,
}: BrandLogoProps) {
  const width = compact ? 148 : branding.logoWidth
  const height = Math.round((width * LOGO_NATIVE_H) / LOGO_NATIVE_W)
  const rootClass = ['credix-brand-logo', compact ? 'is-compact' : '', className]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={rootClass}>
      <img
        className="credix-brand-logo__img"
        src={branding.logoSrc}
        alt={`${branding.companyLine1} ${branding.companyName}`}
        width={width}
        height={height}
        decoding="async"
      />
      {showTagline && !compact ? (
        <Text type="secondary" className="credix-brand-logo__tagline">
          {branding.tagline}
        </Text>
      ) : null}
    </div>
  )
}
