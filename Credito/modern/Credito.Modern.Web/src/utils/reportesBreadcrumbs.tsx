import { Link } from 'react-router-dom'
import type { BreadcrumbProps } from 'antd'

export function reportesCreditoIndexBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: 'Reportes' },
    { title: 'Crédito' },
  ]
}

export function reportesAlmacenIndexBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: 'Reportes' },
    { title: 'Almacén' },
  ]
}

export function reportesCobranzaBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: 'Reportes' },
    { title: 'Cobranza' },
  ]
}

export function reportesVentaIndexBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: 'Reportes' },
    { title: 'Venta' },
  ]
}

export function reportesCreditoBreadcrumb(
  currentTitle: string,
): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: <Link to="/reportes/credito">Reportes crédito</Link> },
    { title: currentTitle },
  ]
}

export function reportesAlmacenBreadcrumb(
  currentTitle: string,
): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: <Link to="/reportes/almacen">Reportes almacén</Link> },
    { title: currentTitle },
  ]
}
