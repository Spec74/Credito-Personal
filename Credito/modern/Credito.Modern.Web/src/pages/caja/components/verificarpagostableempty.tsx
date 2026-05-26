import { Typography } from 'antd'

type Props = {
  buscando: boolean
}

export function VerificarPagosTableEmpty({ buscando }: Props) {
  if (buscando) {
    return (
      <div className="caja-verificar-pagos-empty">
        <Typography.Text type="secondary">
          No hay coincidencias con el filtro. Pruebe otro cliente, movimiento o nº de
          operación.
        </Typography.Text>
      </div>
    )
  }
  return (
    <div className="caja-verificar-pagos-empty">
      <Typography.Text strong>Sin depósitos pendientes</Typography.Text>
      <Typography.Text type="secondary" style={{ display: 'block', marginTop: 4 }}>
        Todos los pagos por transferencia están verificados en cuentas.
      </Typography.Text>
    </div>
  )
}
