import { Popover } from 'antd'
import { InfoCircleOutlined } from '@ant-design/icons'
import { formatMoney } from '../../../utils/formatMoney'
import { parseResumenCuentaCaja } from '../../../utils/resumenCuentaCaja'
import { ResumenCuentaBrandLogo } from '../../tesoreria/components/resumenCuentaBrandIcons'
import {
  medioEtiquetaUi,
  medioNombreMarca,
} from '../../tesoreria/components/bovedaResumenCuentaParse'

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
            {items.map((item) => {
              const { variant, aria } = medioEtiquetaUi(item.cuenta)
              const nombre = medioNombreMarca(item.cuenta)
              return (
                <li key={item.cuenta} title={aria}>
                  <span className="caja-saldos-resumen__cuenta" translate="no">
                    <ResumenCuentaBrandLogo
                      variant={variant}
                      etiqueta={nombre}
                      size="sm"
                    />
                    {nombre}
                  </span>
                  <strong>{formatMoney(item.importe)}</strong>
                </li>
              )
            })}
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
