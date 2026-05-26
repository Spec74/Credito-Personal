import type { ReactNode } from 'react'
import { DownloadOutlined, PlusOutlined } from '@ant-design/icons'
import { Button, Checkbox } from 'antd'
import {
  CredixListToolbar,
  type CredixListToolbarProps,
} from './CredixListToolbar'

export type CredixCrudToolbarProps = Omit<
  CredixListToolbarProps,
  'extra'
> & {
  /** Selects, AutoComplete, etc. (paridad filtros legacy arriba de la grilla). */
  filters?: ReactNode
  incluirInactivos?: boolean
  onIncluirInactivosChange?: (checked: boolean) => void
  incluirInactivosLabel?: string
  onCreate?: () => void
  createLabel?: string
  onExportCsv?: () => void
  exportCsvDisabled?: boolean
  extra?: ReactNode
}

/** Toolbar estándar CRUD: filtros opcionales + búsqueda responsive + acciones legacy. */
export function CredixCrudToolbar({
  filters,
  incluirInactivos,
  onIncluirInactivosChange,
  incluirInactivosLabel = 'Incluir inactivos',
  onCreate,
  createLabel = 'Nuevo',
  onExportCsv,
  exportCsvDisabled,
  extra,
  ...searchProps
}: CredixCrudToolbarProps) {
  const crudActions =
    onIncluirInactivosChange != null ||
    onCreate != null ||
    onExportCsv != null ||
    extra != null ? (
      <div className="credix-crud-toolbar__actions">
        {extra}
        {onIncluirInactivosChange != null ? (
          <Checkbox
            checked={incluirInactivos ?? true}
            onChange={(e) => onIncluirInactivosChange(e.target.checked)}
          >
            {incluirInactivosLabel}
          </Checkbox>
        ) : null}
        {onCreate ? (
          <Button type="primary" icon={<PlusOutlined />} onClick={onCreate}>
            {createLabel}
          </Button>
        ) : null}
        {onExportCsv ? (
          <Button
            icon={<DownloadOutlined />}
            disabled={exportCsvDisabled}
            onClick={onExportCsv}
          >
            CSV
          </Button>
        ) : null}
      </div>
    ) : null

  return (
    <div className="credix-crud-toolbar">
      {filters ? (
        <div className="credix-crud-toolbar__filters" role="group" aria-label="Filtros">
          {filters}
        </div>
      ) : null}
      <CredixListToolbar {...searchProps} extra={crudActions} />
    </div>
  )
}
