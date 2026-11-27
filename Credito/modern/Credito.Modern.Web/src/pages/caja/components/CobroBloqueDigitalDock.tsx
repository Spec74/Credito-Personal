import { Button, Typography } from 'antd'
import { QrcodeOutlined } from '@ant-design/icons'
import { CobroMedioInstrucciones } from '../../../components/caja/CobroMedioInstrucciones'
import { formatMoney } from '../../../utils/formatMoney'
import { medioNombreMarca } from '../../tesoreria/components/bovedaResumenCuentaParse'

const { Text } = Typography

export type CobroBloqueDigitalDockItem = {
  creditoId: number
  personaNombre: string
  tipoPagoId: number
  tipoPagoLabel: string
  importe: number
  /** Índice estable si hay varios medios del mismo crédito (mixto). */
  lineKey: string
}

type Props = {
  items: CobroBloqueDigitalDockItem[]
  activeKey: string | null
  onActiveKeyChange: (key: string) => void
  onVoucherValidated: (item: CobroBloqueDigitalDockItem, fechaOperacionLocal: string) => void
  onMontoMismatch?: () => void
}

/**
 * Panel de cobro digital fuera del scroll horizontal de la planilla.
 * En PC la tabla puede desplazarse; el QR permanece usable a ancho del viewport.
 */
export function CobroBloqueDigitalDock({
  items,
  activeKey,
  onActiveKeyChange,
  onVoucherValidated,
  onMontoMismatch,
}: Props) {
  if (items.length === 0) return null

  const active =
    items.find((it) => it.lineKey === activeKey) ?? items[0] ?? null
  if (!active) return null

  return (
    <aside className="cobro-bloque-digital-dock" aria-label="Cobro digital">
      <header className="cobro-bloque-digital-dock__head">
        <div className="cobro-bloque-digital-dock__title">
          <QrcodeOutlined aria-hidden />
          <div>
            <strong>Cobro digital</strong>
            <Text type="secondary" className="cobro-bloque-digital-dock__sub">
              Fuera de la tabla · muestre el QR al cliente
            </Text>
          </div>
        </div>
        {items.length > 1 ? (
          <div
            className="cobro-bloque-digital-dock__tabs"
            role="tablist"
            aria-label="Créditos tipados digitales"
          >
            {items.map((it) => {
              const selected = it.lineKey === active.lineKey
              const marca = medioNombreMarca(it.tipoPagoLabel)
              return (
                <Button
                  key={it.lineKey}
                  size="small"
                  type={selected ? 'primary' : 'default'}
                  role="tab"
                  aria-selected={selected}
                  className="cobro-bloque-digital-dock__tab"
                  onClick={() => onActiveKeyChange(it.lineKey)}
                >
                  <span className="cobro-bloque-digital-dock__tab-name" translate="no">
                    {it.personaNombre.split(' ').slice(0, 2).join(' ')}
                  </span>
                  <span className="cobro-bloque-digital-dock__tab-meta" translate="no">
                    {marca} · S/ {formatMoney(it.importe)}
                  </span>
                </Button>
              )
            })}
          </div>
        ) : (
          <p className="cobro-bloque-digital-dock__single" translate="no">
            {active.personaNombre} · {medioNombreMarca(active.tipoPagoLabel)} · S/{' '}
            {formatMoney(active.importe)}
          </p>
        )}
      </header>

      <div className="cobro-bloque-digital-dock__body">
        <CobroMedioInstrucciones
          tipoPagoId={active.tipoPagoId}
          monto={active.importe}
          onVoucherValidated={(v) => {
            onVoucherValidated(active, v.fechaOperacionLocal)
            if (!v.coincideMonto) onMontoMismatch?.()
          }}
        />
      </div>
    </aside>
  )
}
