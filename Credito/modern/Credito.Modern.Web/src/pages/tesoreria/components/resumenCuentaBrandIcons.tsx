import { useId } from 'react'
import type { FC, SVGProps } from 'react'
import type { ResumenCuentaVariant } from './bovedaResumenCuentaParse'

type IconProps = SVGProps<SVGSVGElement> & { uid: string }

function IconEfectivo({ uid, ...props }: IconProps) {
  const g = `ef-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="8" y1="6" x2="40" y2="42" gradientUnits="userSpaceOnUse">
          <stop stopColor="#4ade80" />
          <stop offset="1" stopColor="#16a34a" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <rect x="11" y="15" width="26" height="16" rx="4" fill="#fff" fillOpacity="0.95" />
      <circle cx="33" cy="19" r="4" fill="#fbbf24" />
      <text
        x="22"
        y="27"
        textAnchor="middle"
        fill="#15803d"
        fontSize="11"
        fontWeight="800"
        fontFamily="system-ui, sans-serif"
      >
        S/
      </text>
    </svg>
  )
}

function IconYape({ uid, ...props }: IconProps) {
  const g = `yp-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="6" y1="4" x2="42" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#8b3aab" />
          <stop offset="1" stopColor="#5c1578" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <text
        x="24"
        y="29"
        dominantBaseline="middle"
        textAnchor="middle"
        fill="#fff"
        fontSize="15"
        fontWeight="700"
        fontStyle="italic"
        letterSpacing="-0.6"
        fontFamily="'Segoe UI', system-ui, sans-serif"
      >
        yape
      </text>
      <circle cx="36" cy="14" r="8" fill="#00d4aa" />
      <text
        x="36"
        y="17"
        textAnchor="middle"
        fill="#5c1578"
        fontSize="8"
        fontWeight="800"
        fontFamily="system-ui, sans-serif"
      >
        S/
      </text>
    </svg>
  )
}

function IconPlin({ uid, ...props }: IconProps) {
  const g = `pl-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#38bdf8" />
          <stop offset="1" stopColor="#0369a1" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <text
        x="24"
        y="31"
        textAnchor="middle"
        fill="#fff"
        fontSize="17"
        fontWeight="800"
        fontFamily="system-ui, sans-serif"
        letterSpacing="-0.5"
      >
        plin
      </text>
    </svg>
  )
}

function IconInterbank({ uid, ...props }: IconProps) {
  const g = `ib-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#ff7a1a" />
          <stop offset="1" stopColor="#e85d04" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <circle cx="24" cy="24" r="14" stroke="#fff" strokeWidth="2.2" fill="none" opacity="0.95" />
      <circle cx="24" cy="24" r="2.6" fill="#fff" opacity="0.95" />
      <g stroke="#fff" strokeWidth="2.2" strokeLinecap="round" opacity="0.95">
        {/* Rays (compass-like) */}
        <path d="M24 10 L24 16" />
        <path d="M24 32 L24 38" />
        <path d="M10 24 L16 24" />
        <path d="M32 24 L38 24" />
        <path d="M15 15 L19 19" />
        <path d="M29 19 L33 15" />
        <path d="M15 33 L19 29" />
        <path d="M29 29 L33 33" />
      </g>
    </svg>
  )
}

function IconBcp({ uid, ...props }: IconProps) {
  const g = `bcp-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#1e4fd6" />
          <stop offset="1" stopColor="#002395" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <rect x="8" y="10" width="32" height="6" rx="3" fill="#ff6b00" />
      <text
        x="24"
        y="34"
        dominantBaseline="middle"
        textAnchor="middle"
        fill="#fff"
        stroke="#0a3a8a"
        strokeWidth="1.6"
        paintOrder="stroke"
        fontSize="17"
        fontWeight="900"
        fontFamily="Arial, system-ui, sans-serif"
        letterSpacing="-0.6"
      >
        BCP
      </text>
    </svg>
  )
}

function IconBancoNacion({ uid, ...props }: IconProps) {
  const g = `bn-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#22c55e" />
          <stop offset="1" stopColor="#047857" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      {/* Cube outline (matches the "Banco de la Nación" visual in your screenshot) */}
      <g fill="none" stroke="#fff" strokeWidth="2.2" strokeLinejoin="round" opacity="0.95">
        <polygon points="18,18 24,14 30,18 24,22" />
        <polygon points="18,18 24,22 24,34 18,30" />
        <polygon points="30,18 24,22 24,34 30,30" />
      </g>
    </svg>
  )
}

function IconBbva({ uid, ...props }: IconProps) {
  const g = `bbva-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#2563eb" />
          <stop offset="1" stopColor="#004481" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <text
        x="24"
        y="31"
        textAnchor="middle"
        fill="#fff"
        fontSize="15"
        fontWeight="800"
        fontFamily="system-ui, sans-serif"
        letterSpacing="1"
      >
        BBVA
      </text>
    </svg>
  )
}

function IconScotiabank({ uid, ...props }: IconProps) {
  const g = `sc-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#ef4444" />
          <stop offset="1" stopColor="#b91c1c" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <text
        x="24"
        y="22"
        textAnchor="middle"
        fill="#fff"
        fontSize="8.5"
        fontWeight="800"
        fontFamily="system-ui, sans-serif"
      >
        SCOTIA
      </text>
      <text
        x="24"
        y="32"
        textAnchor="middle"
        fill="#fff"
        fontSize="8"
        fontWeight="700"
        fontFamily="system-ui, sans-serif"
        opacity="0.9"
      >
        BANK
      </text>
    </svg>
  )
}

function IconTransferencia({ uid, ...props }: IconProps) {
  const g = `tr-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#64748b" />
          <stop offset="1" stopColor="#334155" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <path
        fill="#fff"
        d="M30 17h-7v-4l-9 6.5 9 6.5v-4h7c2.2 0 4 1.8 4 4v2h3v-2c0-4.5-4-5-7-5zm-12 14h7v4l9-6.5-9-6.5v4h-7c-2.2 0-4-1.8-4-4v-2h-3v2c0 4.5 4 5 7 5z"
      />
    </svg>
  )
}

function IconOtro({ uid, ...props }: IconProps) {
  const g = `ot-${uid}`
  return (
    <svg viewBox="0 0 48 48" fill="none" {...props}>
      <defs>
        <linearGradient id={g} x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#2270b8" />
          <stop offset="1" stopColor="#114885" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="14" fill={`url(#${g})`} />
      <text
        x="24"
        y="32"
        textAnchor="middle"
        fill="#fff"
        fontSize="18"
        fontWeight="700"
        fontFamily="system-ui, sans-serif"
      >
        S/
      </text>
    </svg>
  )
}

const ICONS: Record<ResumenCuentaVariant, FC<IconProps>> = {
  efectivo: IconEfectivo,
  yape: IconYape,
  plin: IconPlin,
  interbank: IconInterbank,
  bcp: IconBcp,
  'banco-nacion': IconBancoNacion,
  bbva: IconBbva,
  scotiabank: IconScotiabank,
  transferencia: IconTransferencia,
  otro: IconOtro,
}

type BrandLogoProps = {
  variant: ResumenCuentaVariant
  etiqueta: string
  className?: string
}

export function ResumenCuentaBrandLogo({ variant, etiqueta, className }: BrandLogoProps) {
  const uid = useId().replace(/:/g, '')
  const Icon = ICONS[variant] ?? ICONS.otro
  return (
    <Icon
      uid={uid}
      className={className}
      aria-label={etiqueta}
      width={48}
      height={48}
    />
  )
}
