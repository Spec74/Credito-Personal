const appBase = import.meta.env.BASE_URL || '/'
const publicBase = appBase.endsWith('/') ? appBase : `${appBase}/`

/** Marca alineada al legado (logo embebido en los RDLC legacy). */
export const branding = {
  appShortName: 'CREDIX',
  companyLine1: 'Inversiones',
  companyName: 'CrediConfiable',
  tagline: 'Tramitamos tu préstamo hoy mismo...',
  systemDescription: 'Sistema comercial de ventas y crédito',
  supportPhone: '954892020',
  logoSrc: `${publicBase}brand/credix.png`,
  logoSrc3x: `${publicBase}brand/credix-3x.png`,
  logoWidth: 248,
  colors: {
    /** Alineado a `--credix-brand` y Ant Design `colorPrimary`. */
    primary: '#114885',
    accent: '#2e69ae',
    /** Isotipo original (cian del PNG de marca). */
    mark: '#3DB5EA',
    markDeep: '#1A8FD4',
    background: '#f8f9fb',
  },
} as const
