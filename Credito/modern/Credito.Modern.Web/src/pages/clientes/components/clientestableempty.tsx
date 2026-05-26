import { Link } from 'react-router-dom'
import { TeamOutlined } from '@ant-design/icons'
import { Button } from 'antd'

type Props = {
  buscandoCatalogo: boolean
  terminoCorto: boolean
}

export function ClientesTableEmpty({ buscandoCatalogo, terminoCorto }: Props) {
  let title = 'Sin clientes en esta vista'
  let desc =
    'Los clientes vinculados a sus créditos aparecerán aquí. Use la búsqueda (mín. 2 caracteres) para explorar el catálogo completo.'

  if (terminoCorto) {
    title = 'Escriba al menos 2 caracteres'
    desc = 'Mientras tanto se muestran los clientes de sus créditos activos, igual que el grid legacy sin filtro.'
  } else if (buscandoCatalogo) {
    title = 'Sin coincidencias en el catálogo'
    desc = 'Pruebe con apellidos, DNI, código, celular o correo. Puede registrar un cliente nuevo.'
  }

  return (
    <div className="clientes-empty">
      <TeamOutlined className="clientes-empty__icon" aria-hidden />
      <p className="clientes-empty__title">{title}</p>
      <p className="clientes-empty__desc">{desc}</p>
      <Link to="/clientes/nuevo">
        <Button type="primary">Nuevo cliente</Button>
      </Link>
    </div>
  )
}
