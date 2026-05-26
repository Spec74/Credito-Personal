import { Link } from 'react-router-dom'
import type { BreadcrumbProps } from 'antd'

/** Migas de pan para pantallas del índice Crédito (paridad Reportes → Crédito en MVC). */
export function creditoInformeBreadcrumb(
  currentTitle: string,
): NonNullable<BreadcrumbProps['items']> {
  return [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/credito">Crédito</Link> },
    { title: currentTitle },
  ]
}
