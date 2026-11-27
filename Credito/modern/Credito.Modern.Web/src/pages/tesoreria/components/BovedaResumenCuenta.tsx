import { useMemo } from 'react'
import { Alert, Checkbox, Spin, Typography } from 'antd'
import { WalletOutlined } from '@ant-design/icons'
import type { BovedaMediosRed } from '../../../api/boveda'
import { ApiError } from '../../../api/errors'
import { formatMoney } from '../../../utils/formatMoney'
import { esTipoPagoBilleteraCentral } from '../../../utils/tipoPago'
import { etiquetaSede, type ResumenCuentaVariant } from './bovedaResumenCuentaParse'
import {
  buildBovedaMedioCards,
  casiCero,
  etiquetaBilletera,
  filterBovedaMedioCards,
} from './bovedaResumenCuentaModel'
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
  /** Si false, oculta medios en cero sin pendiente en caja. Default false. */
  mostrarCeros?: boolean
  onMostrarCerosChange?: (v: boolean) => void
}

export function BovedaResumenCuenta({
  data,
  loading,
  isError,
  error,
  hideHeader = false,
  eagerLogos = false,
  mostrarCeros = false,
  onMostrarCerosChange,
}: Props) {
  const red = data?.esCentral === true

  const cards = useMemo(() => {
    if (!data?.marcas?.length) return []
    return filterBovedaMedioCards(
      buildBovedaMedioCards(data.marcas, data.enCajas, red),
      mostrarCeros,
    )
  }, [data, red, mostrarCeros])

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

  if (cards.length === 0) {
    return (
      <div className="boveda-resumen-cuenta boveda-resumen-cuenta--empty boveda-resumen-cuenta--empty-col">
        <div className="boveda-resumen-cuenta__empty-row">
          <WalletOutlined className="boveda-resumen-cuenta__icon" aria-hidden />
          <Text type="secondary">
            Todos los medios están en cero. Active «Mostrar en cero» para verlos.
          </Text>
        </div>
        {onMostrarCerosChange ? (
          <Checkbox
            checked={mostrarCeros}
            onChange={(e) => onMostrarCerosChange(e.target.checked)}
          >
            Mostrar en cero
          </Checkbox>
        ) : null}
      </div>
    )
  }

  return (
    <div
      className={
        red
          ? 'boveda-resumen-cuenta boveda-resumen-cuenta--red'
          : 'boveda-resumen-cuenta boveda-resumen-cuenta--sucursal'
      }
      role="region"
      aria-label={red ? 'Medios de pago por familia' : 'Medios de pago'}
      translate="no"
    >
      {hideHeader ? null : (
        <div className="boveda-resumen-cuenta__header">
          <WalletOutlined className="boveda-resumen-cuenta__icon" aria-hidden />
          <span className="boveda-resumen-cuenta__title">Saldos por entidad</span>
        </div>
      )}
      {onMostrarCerosChange ? (
        <div className="boveda-resumen-cuenta__toolbar">
          <Checkbox
            checked={mostrarCeros}
            onChange={(e) => onMostrarCerosChange(e.target.checked)}
          >
            Mostrar medios en cero
          </Checkbox>
        </div>
      ) : null}
      <div className="boveda-resumen-cuenta__body">
        <div className="boveda-resumen-cuenta__grid">
          {cards.map((card) => {
            const { marca, hijos, enCaja } = card
            const variant =
              marcaDesdeTipoPagoId(marca.tipoPagoId) ?? asVariant(marca.variant)
            const etiquetaVisible =
              hijos.length === 0 ? etiquetaBilletera(marca.tipoPagoId) : null
            // Central: sedes de red + billeteras pueden coexistir (pistas distintas).
            const mostrarSedes =
              red &&
              marca.sedes.length > 0 &&
              !esTipoPagoBilleteraCentral(marca.tipoPagoId)

            return (
              <div
                key={marca.tipoPagoId}
                className={`boveda-resumen-cuenta__item boveda-resumen-cuenta__item--${variant}${
                  hijos.length > 0 ? ' boveda-resumen-cuenta__item--familia' : ''
                }`}
                title={marca.marca}
              >
                <div className="boveda-resumen-cuenta__item-top">
                  <div className="boveda-resumen-cuenta__item-icon">
                    <ResumenCuentaBrandLogo
                      variant={variant}
                      etiqueta={marca.marca}
                      tone="logo"
                      eager={eagerLogos}
                    />
                  </div>
                  <div className="boveda-resumen-cuenta__item-meta">
                    {etiquetaVisible ? (
                      <span className="boveda-resumen-cuenta__item-label" translate="no">
                        {etiquetaVisible}
                      </span>
                    ) : null}
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
                    {!casiCero(enCaja) ? (
                      <span className="boveda-resumen-cuenta__item-caja" translate="no">
                        En caja {formatMoney(enCaja)}
                      </span>
                    ) : null}
                  </div>
                </div>

                {hijos.length > 0 ? (
                  <ul className="boveda-resumen-cuenta__wallets">
                    {hijos.map((hijo) => {
                      const label = etiquetaBilletera(hijo.tipoPagoId) ?? hijo.marca
                      return (
                        <li
                          key={hijo.tipoPagoId}
                          className="boveda-resumen-cuenta__wallet"
                        >
                          <span
                            className="boveda-resumen-cuenta__wallet-nom"
                            translate="no"
                          >
                            {label}
                          </span>
                          <span
                            className={
                              hijo.total < 0
                                ? 'boveda-resumen-cuenta__wallet-monto boveda-resumen-cuenta__wallet-monto--neg'
                                : 'boveda-resumen-cuenta__wallet-monto'
                            }
                            translate="no"
                          >
                            {formatMoney(hijo.total)}
                          </span>
                        </li>
                      )
                    })}
                  </ul>
                ) : null}

                {mostrarSedes ? (
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
                    {marca.sedes.length > 1 ? (
                      <li className="boveda-resumen-cuenta__sede boveda-resumen-cuenta__sede--total">
                        <span className="boveda-resumen-cuenta__sede-nom" translate="no">
                          Total
                        </span>
                        <span
                          className={
                            marca.total < 0
                              ? 'boveda-resumen-cuenta__sede-monto boveda-resumen-cuenta__sede-monto--neg'
                              : 'boveda-resumen-cuenta__sede-monto'
                          }
                          translate="no"
                        >
                          {formatMoney(marca.total)}
                        </span>
                      </li>
                    ) : null}
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
