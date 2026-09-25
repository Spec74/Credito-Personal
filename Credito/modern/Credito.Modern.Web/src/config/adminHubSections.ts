import type { CredixHubLink, CredixHubSection } from '../components/credix/CredixHubGrid'

/**
 * Hub Administración — paridad menú MVC:
 * Mantenimiento (Oficina, Caja) + Seguridad (Usuario, Rol) + Comisiones.
 */
export const ADMIN_HUB_SECTIONS: CredixHubSection[] = [
  {
    title: 'Mantenimiento',
    links: [
      {
        to: '/mantenimiento/oficinas',
        label: 'Oficinas',
        description: 'Sedes, denominación y datos de oficina',
      },
      {
        to: '/mantenimiento/cajas',
        label: 'Cajas',
        description: 'Maestro de cajas por oficina y gestor',
      },
    ],
  },
  {
    title: 'Seguridad',
    links: [
      {
        to: '/admin/usuarios',
        label: 'Usuarios',
        description: 'Cuentas, oficinas asignadas y estado',
      },
      {
        to: '/admin/roles',
        label: 'Roles y menús',
        description: 'Permisos y opciones del menú por rol',
      },
    ],
  },
  {
    title: 'Comisiones',
    links: [
      {
        to: '/admin/comisiones',
        label: 'Comisiones',
        description: 'Parámetros y consulta de comisiones',
      },
    ],
  },
]

/** Rutas hijas que habilitan el índice `/admin` (sin exigir ítem «Administración» en el menú). */
export const ADMIN_HUB_ENTRY_PATHS = [
  '/admin/usuarios',
  '/admin/roles',
  '/admin/comisiones',
  '/mantenimiento/oficinas',
  '/mantenimiento/cajas',
  '/caja/maestro',
] as const

export const ADMIN_HUB_QUICK_ACCESS: CredixHubLink[] = [
  {
    to: '/admin/usuarios',
    label: 'Usuarios',
    description: 'Accesos',
  },
  {
    to: '/admin/roles',
    label: 'Roles',
    description: 'Permisos',
  },
  {
    to: '/mantenimiento/oficinas',
    label: 'Oficinas',
    description: 'Sedes',
  },
]
