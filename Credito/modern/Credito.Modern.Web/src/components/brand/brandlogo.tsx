import { Image, Typography } from 'antd'
import { branding } from '../../config/branding'

const { Text } = Typography

interface BrandLogoProps {
  compact?: boolean
  showTagline?: boolean
}

export function BrandLogo({ compact = false, showTagline = false }: BrandLogoProps) {
  return (
    <div style={{ textAlign: 'center', width: '100%' }}>
      <Image
        src={branding.logoSrc}
        alt={`${branding.companyLine1} ${branding.companyName}`}
        preview={false}
        width={compact ? 120 : branding.logoWidth}
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
