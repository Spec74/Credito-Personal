import { Alert, Spin, Typography } from 'antd'
import { WalletOutlined } from '@ant-design/icons'
import type { BovedaMediosRed } from '../../../api/boveda'
import { ApiError } from '../../../api/errors'
import { formatMoney } from '../../../utils/formatMoney'
import { etiquetaSede, type ResumenCuentaVariant } from './bovedaResumenCuentaParse'
import { ResumenCuentaBrandLogo } from './resumenCuentaBrandIcons'
import { marcaDesdeTipoPagoId } from './resumencuentabrands'

const { Text } = Typography

const VARIANTS: ReadonlySet<string> = new Set([
  'efectivo',
  'yape',
  'plin',
  'interbank',
  'bcp',
  'banco-nacion',
  'bbva',
  'scotiabank',
  'transferencia',
  'otro',
])

function asVariant(raw: string): ResumenCuentaVariant {
  return (VARIANTS.has(raw) ? raw : 'otro') as ResumenCuentaVariant
}

type Props = {
  data?: BovedaMediosRed | null
  loading: boolean
  isError: boolean
  error?: unknown
  hideHeader?: boolean
  eagerLogos?: boolean
}

export function BovedaResumenCuenta({
  data,
  loading,
  isError,
  error,
  hideHeader = false,
  eagerLogos = false,
}: Props) {
  if (loading) {
    return (
      <div className="boveda-resumen-cuenta boveda-resumen-cuenta--loading">
        <Spin size="small" />
        <Text type="secondary">Cargando saldos por medio de pago…</Text>
      </div>
    )
  }

  if (isError) {
    const msg = error instanceof ApiError ? error.message : 'No se pudo cargar el resumen.'
    return (
      <Alert type="warning" showIcon message="Medios de pago" description={msg} />
    )
  }

  if (!data || data.marcas.length === 0) {
    return (
      <div className="boveda-resumen-cuenta boveda-resumen-cuenta--empty">
        <WalletOutlined className="boveda-resumen-cuenta__icon" aria-hidden />
        <Text type="secondary">Sin saldos por medio de pago para esta bóveda.</Text>
      </div>
    )
  }

  const red = data.esCentral

  return (
    <div
      className={
        red
          ? 'boveda-resumen-cuenta boveda-resumen-cuenta--red'
          : 'boveda-resumen-cuenta'
      }
      role="region"
      aria-label={red ? 'Medios de pago por sucursal' : 'Medios de pago'}
      translate="no"
    >
      {hideHeader ? null : (
        <div className="boveda-resumen-cuenta__header">
          <WalletOutlined className="boveda-resumen-cuenta__icon" aria-hidden />
          <span className="boveda-resumen-cuenta__title">Saldos por entidad</span>
        </div>
      )}
      <div className="boveda-resumen-cuenta__body">
        <div className="boveda-resumen-cuenta__grid">
          {data.marcas.map((marca) => {
            const variant = marcaDesdeTipoPagoId(marca.tipoPagoId) ?? asVariant(marca.variant)
            return (
              <div
                key={marca.tipoPagoId}
                className={`boveda-resumen-cuenta__item boveda-resumen-cuenta__item--${variant}`}
                title={marca.marca}
              >
                <div className="boveda-resumen-cuenta__item-icon">
                  <ResumenCuentaBrandLogo
                    variant={variant}
                    etiqueta={marca.marca}
                    tone="logo"
                    eager={eagerLogos}
                  />
                </div>
                <div className="boveda-resumen-cuenta__item-meta">
                  <span
                    className={
                      marca.total < 0
                        ? 'boveda-resumen-cuenta__item-monto boveda-resumen-cuenta__item-monto--neg'
                        : 'boveda-resumen-cuenta__item-monto'
                    }
                    translate="no"
                  >
                    S/ {formatMoney(marca.total)}
                  </span>
                </div>
                {red && marca.sedes.length > 0 ? (
                  <ul className="boveda-resumen-cuenta__sedes">
                    {marca.sedes.map((sede) => {
                      const nombre = sede.esLegado
                        ? sede.oficina
                        : etiquetaSede(sede.oficina, sede.indPrincipal)
                      return (
                        <li
                          key={`${marca.tipoPagoId}-${sede.oficinaId}-${sede.esLegado ? 'leg' : 'real'}`}
                          className={
                            sede.esLegado
                              ? 'boveda-resumen-cuenta__sede boveda-resumen-cuenta__sede--legado'
                              : 'boveda-resumen-cuenta__sede'
                          }
                        >
                          <span className="boveda-resumen-cuenta__sede-nom" translate="no">
                            {nombre}
                          </span>
                          <span
                            className={
                              sede.monto < 0
                                ? 'boveda-resumen-cuenta__sede-monto boveda-resumen-cuenta__sede-monto--neg'
                                : 'boveda-resumen-cuenta__sede-monto'
                            }
                            translate="no"
                          >
                            {formatMoney(sede.monto)}
                          </span>
                        </li>
                      )
                    })}
                  </ul>
                ) : null}
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
