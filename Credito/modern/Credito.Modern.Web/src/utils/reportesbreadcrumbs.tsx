import { Link } from 'react-router-dom'
import type { BreadcrumbProps } from 'antd'

/** Migas para la rejilla Reportes → Crédito. */
export function reportesCreditoIndexBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: 'Reportes' },
    { title: 'Crédito' },
  ]
}

/** Migas para Reportes → Almacén (paridad MVC Reporte/Almacen). */
export function reportesAlmacenIndexBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: 'Reportes' },
    { title: 'Almacén' },
  ]
}

/** Migas para Reportes → Cobranza. */
export function reportesCobranzaBreadcrumb(): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: 'Reportes' },
    { title: 'Cobranza' },
  ]
}

/** Migas para la rejilla Reportes → Crédito. */
/** Migas para Reportes → Cobranza. */
/** Migas para informes lanzados desde Reportes → Crédito. */
export function reportesCreditoBreadcrumb(
  currentTitle: string,
): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/reportes/credito">Reportes crédito</Link> },
    { title: currentTitle },
  ]
}

/** Migas para informes lanzados desde Reportes → Almacén. */
export function reportesAlmacenBreadcrumb(
  currentTitle: string,
): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/reportes/almacen">Reportes almacén</Link> },
    { title: currentTitle },
  ]
}
