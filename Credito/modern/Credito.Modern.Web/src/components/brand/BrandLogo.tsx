import { Image, Typography } from 'antd'
import { branding } from '../../config/branding'

const { Text } = Typography

interface BrandLogoProps {
  compact?: boolean
  showTagline?: boolean
}

/** Native asset is 312×110 — lock aspect ratio to avoid CLS (Lighthouse unsized-images). */
const LOGO_NATIVE_W = 312
const LOGO_NATIVE_H = 110

export function BrandLogo({ compact = false, showTagline = false }: BrandLogoProps) {
  const width = compact ? 120 : branding.logoWidth
  const height = Math.round((width * LOGO_NATIVE_H) / LOGO_NATIVE_W)

  return (
    <div style={{ textAlign: 'center', width: '100%' }}>
      <Image
        src={branding.logoSrc}
        alt={`${branding.companyLine1} ${branding.companyName}`}
        preview={false}
        width={width}
        height={height}
        style={{ maxWidth: '100%', height: 'auto' }}
      />
      {showTagline && !compact && (
        <Text
          type="secondary"
          style={{ display: 'block', marginTop: 8, fontSize: 13 }}
        >
          {branding.tagline}
        </Text>
      )}
    </div>
  )
}
