import type { ReactNode } from 'react'
import { ReloadOutlined, SearchOutlined } from '@ant-design/icons'
import { Button, Input, Tag } from 'antd'

export type CredixListToolbarProps = {
  className?: string
  value: string
  onChange: (v: string) => void
  onSubmit?: () => void
  placeholder: string
  hint?: string
  hintShort?: string
  filteredCount?: number
  totalCount?: number
  loading?: boolean
  onRefresh: () => void
  extra?: ReactNode
  /** Input.Search con botón Buscar (p. ej. catálogo de clientes). */
  withSearchButton?: boolean
}

const DEFAULT_CLASS = 'credix-list-toolbar'

/**
 * Barra de búsqueda + acciones reutilizable en CRUD y bandejas.
 * Layout responsive vía `credix-list-toolbar.css`.
 */
export function CredixListToolbar({
  className = DEFAULT_CLASS,
  value,
  onChange,
  onSubmit,
  placeholder,
  hint,
  hintShort,
  filteredCount,
  totalCount,
  loading,
  onRefresh,
  extra,
  withSearchButton = false,
}: CredixListToolbarProps) {
  const filtrado =
    filteredCount != null && totalCount != null && value.trim().length > 0

  const handleSubmit = () => {
    onSubmit?.()
  }

  return (
    <div className={className} role="search">
      <div className={`${className}__buscar`}>
        {withSearchButton ? (
          <Input.Search
            allowClear
            placeholder={placeholder}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onSearch={handleSubmit}
            enterButton={
              <span>
                <SearchOutlined aria-hidden /> Buscar
              </span>
            }
            loading={loading}
            aria-label="Buscar en la lista"
          />
        ) : (
          <Input
            allowClear
            prefix={<SearchOutlined aria-hidden />}
            placeholder={placeholder}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onPressEnter={handleSubmit}
            aria-label="Buscar en la lista"
          />
        )}
      </div>
      {hint ? (
        <p className={`${className}__hint`}>
          <span className={`${className}__hint-long`}>{hint}</span>
          <span className={`${className}__hint-short`}>
            {hintShort ?? hint}
          </span>
        </p>
      ) : null}
      <div className={`${className}__actions`}>
        {filtrado ? (
          <Tag color="blue">
            {filteredCount} de {totalCount}
          </Tag>
        ) : null}
        {extra}
        <Button
          className={`${className}__refresh`}
          icon={<ReloadOutlined />}
          onClick={onRefresh}
          loading={loading}
        >
          Actualizar
        </Button>
      </div>
    </div>
  )
}
