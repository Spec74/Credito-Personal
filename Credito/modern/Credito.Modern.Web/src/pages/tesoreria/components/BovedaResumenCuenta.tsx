import { Alert, Checkbox, Spin, Typography } from 'antd'
import { WalletOutlined } from '@ant-design/icons'
import type { BovedaMedioMarca, BovedaMediosRed } from '../../../api/boveda'
import { ApiError } from '../../../api/errors'
import { formatMoney } from '../../../utils/formatMoney'
import {
  CENTRAL_WALLET_IDS,
  TIPO_PAGO_EFECTIVO,
  TIPO_PAGO_INTERBANK,
  TIPO_PAGO_INTERBANK_GERENTE,
  TIPO_PAGO_INTERBANK_RENE,
  TIPO_PAGO_YAPE,
  TIPO_PAGO_YAPE_GERENTE,
  TIPO_PAGO_YAPE_RENE,
  esTipoPagoBilleteraCentral,
} from '../../../utils/tipoPago'
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

function casiCero(n: number): boolean {
  return Math.abs(Number(n) || 0) < 0.01
}

function etiquetaBilletera(tipoPagoId: number): string | null {
  switch (tipoPagoId) {
    case TIPO_PAGO_YAPE_GERENTE:
    case TIPO_PAGO_INTERBANK_GERENTE:
      return 'Gerente'
    case TIPO_PAGO_YAPE_RENE:
    case TIPO_PAGO_INTERBANK_RENE:
      return 'René'
    default:
      return null
  }
}

/** Hijos de ledger bajo el genérico de familia (vista Central). */
const FAMILY_CHILDREN: Readonly<Record<number, readonly number[]>> = {
  [TIPO_PAGO_YAPE]: [TIPO_PAGO_YAPE_GERENTE, TIPO_PAGO_YAPE_RENE],
  [TIPO_PAGO_INTERBANK]: [TIPO_PAGO_INTERBANK_GERENTE, TIPO_PAGO_INTERBANK_RENE],
}

type FamilyCard = {
  /** Marca mostrada (padre o medio suelto). */
  marca: BovedaMedioMarca
  /** Billeteras anidadas (solo Central / familias). */
  hijos: BovedaMedioMarca[]
  /** Cobros aún en caja para este tipo (o suma de hijos). */
  enCaja: number
}

function buildCards(
  marcas: BovedaMedioMarca[],
  enCajas: BovedaMedioMarca[] | null | undefined,
  agruparFamilias: boolean,
): FamilyCard[] {
  const byId = new Map(marcas.map((m) => [m.tipoPagoId, m]))
  const enCajaById = new Map(
    (enCajas ?? []).map((m) => [m.tipoPagoId, Number(m.total) || 0]),
  )

  if (!agruparFamilias) {
    return marcas.map((marca) => ({
      marca,
      hijos: [],
      enCaja: enCajaById.get(marca.tipoPagoId) ?? 0,
    }))
  }

  const used = new Set<number>()
  const cards: FamilyCard[] = []

  for (const parentId of [TIPO_PAGO_YAPE, TIPO_PAGO_INTERBANK]) {
    const childIds = FAMILY_CHILDREN[parentId] ?? []
    const hijos = childIds
      .map((id) => byId.get(id))
      .filter((m): m is BovedaMedioMarca => !!m)
    const parent = byId.get(parentId)
    if (!parent && hijos.length === 0) continue

    const marca: BovedaMedioMarca = parent ?? {
      tipoPagoId: parentId,
      marca: parentId === TIPO_PAGO_YAPE ? 'Yape' : 'Interbank',
      variant: parentId === TIPO_PAGO_YAPE ? 'yape' : 'interbank',
      total: hijos.reduce((s, h) => s + Number(h.total), 0),
      sedes: [],
    }

    used.add(parentId)
    for (const id of childIds) used.add(id)

    const enCaja =
      (enCajaById.get(parentId) ?? 0) +
      childIds.reduce((s, id) => s + (enCajaById.get(id) ?? 0), 0)

    cards.push({ marca, hijos, enCaja })
  }

  for (const marca of marcas) {
    if (used.has(marca.tipoPagoId)) continue
    if (CENTRAL_WALLET_IDS.includes(marca.tipoPagoId)) continue
    cards.push({
      marca,
      hijos: [],
      enCaja: enCajaById.get(marca.tipoPagoId) ?? 0,
    })
  }

  return cards
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
  const cards = buildCards(data.marcas, data.enCajas, red).filter((card) => {
    if (mostrarCeros) return true
    const hijosSum = card.hijos.reduce((s, h) => s + Math.abs(Number(h.total)), 0)
    if (!casiCero(card.marca.total) || hijosSum > 0.01) return true
    if (!casiCero(card.enCaja)) return true
    // Efectivo siempre visible: es el medio de cuadre operativo.
    if (card.marca.tipoPagoId === TIPO_PAGO_EFECTIVO) return true
    return false
  })

  if (cards.length === 0) {
    return (
      <div className="boveda-resumen-cuenta boveda-resumen-cuenta--empty">
        <WalletOutlined className="boveda-resumen-cuenta__icon" aria-hidden />
        <Text type="secondary">
          Todos los medios están en cero. Active «Mostrar en cero» para verlos.
        </Text>
        {onMostrarCerosChange ? (
          <Checkbox
            checked={mostrarCeros}
            onChange={(e) => onMostrarCerosChange(e.target.checked)}
            style={{ marginTop: 8 }}
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
          : 'boveda-resumen-cuenta'
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
            const mostrarSedes =
              red &&
              marca.sedes.length > 0 &&
              !esTipoPagoBilleteraCentral(marca.tipoPagoId) &&
              hijos.length === 0

            return (
              <div
                key={marca.tipoPagoId}
                className={`boveda-resumen-cuenta__item boveda-resumen-cuenta__item--${variant}${
                  hijos.length > 0 ? ' boveda-resumen-cuenta__item--familia' : ''
                }`}
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
