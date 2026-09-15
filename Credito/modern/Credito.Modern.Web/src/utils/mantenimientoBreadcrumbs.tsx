import { Link } from 'react-router-dom'
import type { BreadcrumbProps } from 'antd'

export function mantenimientoOficinasBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: 'Mantenimiento' },
    { title: 'Oficinas' },
  ]
}

export function mantenimientoCajasBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: 'Mantenimiento' },
    { title: 'Cajas' },
  ]
}
