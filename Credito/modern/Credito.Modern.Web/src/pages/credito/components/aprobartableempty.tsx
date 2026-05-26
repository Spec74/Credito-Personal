import { FileSearchOutlined } from '@ant-design/icons'
import { Button } from 'antd'
import { Link } from 'react-router-dom'

type Props = {
  buscando: boolean
  terminoCorto?: boolean
}

export function AprobarTableEmpty({ buscando, terminoCorto }: Props) {
  if (terminoCorto) {
    return (
      <div className="credito-aprobacion-empty">
        <FileSearchOutlined className="credito-aprobacion-empty__icon" aria-hidden />
        <p className="credito-aprobacion-empty__title">Escriba un poco más</p>
        <p className="credito-aprobacion-empty__desc">
          Use al menos 2 letras, o solo números si busca DNI o Nº de crédito.
        </p>
      </div>
    )
  }

  return (
    <div className="credito-aprobacion-empty">
      <FileSearchOutlined className="credito-aprobacion-empty__icon" aria-hidden />
      <p className="credito-aprobacion-empty__title">
        {buscando ? 'Sin coincidencias' : 'No hay créditos pendientes'}
      </p>
      <p className="credito-aprobacion-empty__desc">
        {buscando
          ? 'Pruebe otro nombre, DNI, código, gestor o Nº crédito. Puede usar varias palabras (ej. apellido + DNI).'
          : 'Cuando existan solicitudes en estado PEN aparecerán en esta bandeja.'}
      </p>
      {!buscando ? (
        <Link to="/credito/consulta">
          <Button type="link">Ir a consulta de crédito</Button>
        </Link>
      ) : null}
    </div>
  )
}
