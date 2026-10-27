import { useId } from 'react'
import { branding } from '../../config/branding'

interface BrandLogoProps {
  compact?: boolean
  showTagline?: boolean
  className?: string
}

const VIEW_W = 680
const VIEW_H = 200

/**
 * Marca en SVG (no el PNG de 300 px ni invert en oscuro: eso pixelaba).
 * El trazo sigue el isotipo original: cabeza + C en cian.
 */
export function BrandLogo({
  compact = false,
  showTagline = false,
  className,
}: BrandLogoProps) {
  const uid = useId().replace(/:/g, '')
  const grad = `credixMark-${uid}`
  const width = compact ? 148 : branding.logoWidth
  const height = Math.round((width * VIEW_H) / VIEW_W)
  const rootClass = ['credix-brand-logo', compact ? 'is-compact' : '', className]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={rootClass}>
      <svg
        className="credix-brand-logo__svg"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        width={width}
        height={height}
        role="img"
        aria-label={`${branding.companyLine1} ${branding.companyName}`}
      >
        <defs>
          <linearGradient id={grad} x1="0.1" y1="0" x2="0.95" y2="1">
            <stop offset="0%" stopColor={branding.colors.mark} />
            <stop offset="100%" stopColor={branding.colors.markDeep} />
          </linearGradient>
        </defs>
        <circle cx="78" cy="46" r="24" fill={`url(#${grad})`} />
        <path
          d="M44 76c-30 14-40 62-8 90 20 18 62 20 86-2"
          fill="none"
          stroke={`url(#${grad})`}
          strokeWidth="22"
          strokeLinecap="round"
        />
        <text
          className="credix-brand-logo__line"
          x="168"
          y="62"
          fontSize="22"
          fontWeight="600"
        >
          {branding.companyLine1}
        </text>
        <text
          className="credix-brand-logo__word"
          x="168"
          y="118"
          fontSize="44"
          fontWeight="700"
        >
          {branding.companyName}
        </text>
        {showTagline ? (
          <text className="credix-brand-logo__tag" x="168" y="158" fontSize="16">
            {branding.tagline}
          </text>
        ) : null}
      </svg>
    </div>
  )
}
