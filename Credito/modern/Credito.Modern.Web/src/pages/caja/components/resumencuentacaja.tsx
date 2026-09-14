import { Popover } from 'antd'
import { InfoCircleOutlined } from '@ant-design/icons'
import { formatMoney } from '../../../utils/formatMoney'
import { parseResumenCuentaCaja } from '../../../utils/resumenCuentaCaja'

type Props = {
  resumen?: string | null
  caja: string
}

/**
 * Desglose por tipo de pago de una caja (`ufnResumenCuentaCajaDiario`). En el legado era una
 * columna de texto ancha; aquí ocupa un icono para no empujar los importes fuera de la vista.
 */
export function ResumenCuentaCaja({ resumen, caja }: Props) {
  const items = parseResumenCuentaCaja(resumen)
  const texto = resumen?.trim()

  if (!texto) {
    return <span className="caja-saldos-resumen__vacio">—</span>
  }

  return (
    <Popover
      trigger={['hover', 'click']}
      placement="left"
      title={`Resumen de cuentas · ${caja}`}
      content={
        items.length > 0 ? (
          <ul className="caja-saldos-resumen__lista">
            {items.map((item) => (
              <li key={item.cuenta}>
                <span>{item.cuenta}</span>
                <strong>{formatMoney(item.importe)}</strong>
              </li>
            ))}
          </ul>
        ) : (
          <p className="caja-saldos-resumen__crudo">{texto}</p>
        )
      }
    >
      <button
        type="button"
        className="caja-saldos-resumen__boton"
        aria-label={`Ver resumen de cuentas de ${caja}`}
      >
        <InfoCircleOutlined aria-hidden />
      </button>
    </Popover>
  )
}
