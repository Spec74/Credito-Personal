import { Button, InputNumber, Typography } from 'antd'
import { SearchOutlined, NumberOutlined } from '@ant-design/icons'

const { Text } = Typography

type Props = {
  creditoId: number | null
  onCreditoIdChange: (id: number | null) => void
  onSearch: () => void
  loading?: boolean
}

/** Acceso directo por número de crédito (paridad búsqueda rápida MVC). */
export function CreditoBuscarPorCredito({
  creditoId,
  onCreditoIdChange,
  onSearch,
  loading = false,
}: Props) {
  return (
    <section
      className="credito-buscar-credito"
      aria-labelledby="credito-buscar-credito-label"
    >
      <div className="credito-buscar-credito__head">
        <NumberOutlined className="credito-buscar-credito__head-icon" aria-hidden />
        <span className="credito-buscar-credito__label" id="credito-buscar-credito-label">
          Por número de crédito
        </span>
      </div>
      <div className="credito-buscar-credito__row">
        <InputNumber
          className="credito-buscar-credito__input"
          size="large"
          min={1}
          placeholder="Ej. 125430"
          value={creditoId}
          controls={false}
          onChange={(v) => onCreditoIdChange(v ?? null)}
          onPressEnter={onSearch}
          aria-label="Número de crédito"
        />
        <Button
          type="primary"
          size="large"
          className="credito-buscar-credito__btn"
          icon={<SearchOutlined />}
          loading={loading}
          disabled={creditoId == null || creditoId < 1}
          onClick={onSearch}
        >
          <span className="credito-buscar-credito__btn-text">Consultar crédito</span>
        </Button>
      </div>
      <Text type="secondary" className="credito-buscar-credito__hint">
        Si conoce el ID del crédito, consúltelo aquí sin buscar al cliente.
      </Text>
    </section>
  )
}
