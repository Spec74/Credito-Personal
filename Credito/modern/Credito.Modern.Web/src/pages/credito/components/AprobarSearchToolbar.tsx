import {
  FilterOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import { Alert, Button, Input, Spin, Tag } from 'antd'

const CAMPOS_BUSQUEDA = [
  'Cliente',
  'DNI',
  'Código',
  'Nº crédito',
  'Gestor',
] as const

type Props = {
  value: string
  onChange: (v: string) => void
  onSearch: () => void
  loading?: boolean
  terminos: string[]
  terminoCorto: boolean
  filtrado: boolean
  totalFiltrado?: number
  onRefresh: () => void
}

export function AprobarSearchToolbar({
  value,
  onChange,
  onSearch,
  loading,
  terminos,
  terminoCorto,
  filtrado,
  totalFiltrado,
  onRefresh,
}: Props) {
  return (
    <section className="credito-aprobacion-search-card" aria-label="Búsqueda de créditos pendientes">
      <div className="credito-aprobacion-search-card__header">
        <div className="credito-aprobacion-search-card__title">
          <span className="credito-aprobacion-search-card__title-icon" aria-hidden>
            <SearchOutlined />
          </span>
          <div>
            <h3 className="credito-aprobacion-search-card__heading">Buscar en la bandeja</h3>
            <p className="credito-aprobacion-search-card__subheading">
              Filtre por cualquier dato del crédito o del cliente
            </p>
          </div>
        </div>
        <div className="credito-aprobacion-search-card__header-actions">
          <Tag
            className="credito-aprobacion-search-card__estado"
            color={filtrado ? 'processing' : 'default'}
            icon={filtrado ? <FilterOutlined /> : undefined}
          >
            {filtrado ? 'Filtrado' : 'Todos PEN'}
          </Tag>
          <Button
            icon={<ReloadOutlined />}
            onClick={onRefresh}
            loading={loading}
            className="credito-aprobacion-search-card__refresh"
          >
            Actualizar
          </Button>
        </div>
      </div>

      <div className="credito-aprobacion-search-card__row">
        <Input
          size="large"
          allowClear
          className="credito-aprobacion-search-card__input"
          placeholder="Cliente, DNI, código, Nº crédito o gestor"
          value={value}
          prefix={<SearchOutlined className="credito-aprobacion-search-card__input-icon" />}
          suffix={loading ? <Spin size="small" /> : null}
          onChange={(e) => onChange(e.target.value)}
          onPressEnter={onSearch}
          aria-label="Texto de búsqueda"
        />
        <Button
          type="primary"
          size="large"
          className="credito-aprobacion-search-card__submit"
          icon={<SearchOutlined />}
          loading={loading}
          onClick={onSearch}
        >
          Buscar
        </Button>
      </div>

      <div className="credito-aprobacion-search-card__chips" role="list" aria-label="Campos incluidos en la búsqueda">
        {CAMPOS_BUSQUEDA.map((campo) => (
          <span key={campo} className="credito-aprobacion-search-card__chip" role="listitem">
            {campo}
          </span>
        ))}
        <span className="credito-aprobacion-search-card__chip credito-aprobacion-search-card__chip--hint">
          <span className="credito-aprobacion-search-card__chip-hint-long">
            Varias palabras = todas deben coincidir
          </span>
          <span className="credito-aprobacion-search-card__chip-hint-short">Multi-palabra</span>
        </span>
      </div>

      <div className="credito-aprobacion-search-card__footer">
        {terminoCorto ? (
          <Alert
            type="info"
            showIcon
            className="credito-aprobacion-search-card__alert"
            message="Escriba al menos 2 letras, o solo números si busca DNI o Nº de crédito."
          />
        ) : filtrado ? (
          <p className="credito-aprobacion-search-card__status credito-aprobacion-search-card__status--active">
            <FilterOutlined aria-hidden />
            <span>
              Búsqueda activa
              {terminos.length > 1 ? (
                <>
                  {' '}
                  — términos: <strong>{terminos.join(' · ')}</strong>
                </>
              ) : null}
              {totalFiltrado != null ? (
                <>
                  {' '}
                  — <strong>{totalFiltrado}</strong> coincidencia
                  {totalFiltrado === 1 ? '' : 's'}
                </>
              ) : null}
            </span>
          </p>
        ) : (
          <div className="credito-aprobacion-search-card__status">
            <p className="credito-aprobacion-search-card__status-line">
              La búsqueda se aplica al escribir (pausa breve) o al pulsar{' '}
              <strong>Buscar</strong> / Enter.
            </p>
            <p className="credito-aprobacion-search-card__status-line credito-aprobacion-search-card__status-line--muted">
              Doble clic en una fila abre la consulta del crédito.
            </p>
          </div>
        )}
      </div>
    </section>
  )
}
