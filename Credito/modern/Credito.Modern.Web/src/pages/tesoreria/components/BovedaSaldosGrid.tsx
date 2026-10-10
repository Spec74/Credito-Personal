import { formatMoney } from '../../../utils/formatMoney'

type Props = {
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
}

export function BovedaSaldosGrid({
  saldoInicial,
  entradas,
  salidas,
  saldoFinal,
}: Props) {
  const items = [
    { label: 'Saldo inicial', value: saldoInicial },
    { label: 'Entradas', value: entradas },
    { label: 'Salidas', value: salidas },
    { label: 'Saldo final', value: saldoFinal, highlight: true },
  ]

  return (
    <div className="boveda-saldos-grid">
      {items.map((item) => (
        <div
          key={item.label}
          className={
            item.highlight
              ? 'boveda-saldos-grid__card boveda-saldos-grid__card--final'
              : 'boveda-saldos-grid__card'
          }
        >
          <span className="boveda-saldos-grid__value">{formatMoney(item.value)}</span>
          <span className="boveda-saldos-grid__label">{item.label}</span>
        </div>
      ))}
    </div>
  )
}
