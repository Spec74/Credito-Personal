import type { ReactNode } from 'react'
import { CredixListToolbar } from '../../../components/credix/CredixListToolbar'

type Props = {
  value: string
  onChange: (v: string) => void
  placeholder: string
  hint?: string
  hintShort?: string
  filteredCount?: number
  totalCount?: number
  loading?: boolean
  onRefresh: () => void
  extra?: ReactNode
}

/** Toolbar de Saldos — CredixListToolbar con hints por defecto. */
export function SaldosTableToolbar(props: Props) {
  return (
    <CredixListToolbar
      hint="Filtro instantáneo en la tabla cargada (sin nueva llamada al servidor)."
      hintShort="Filtro en tabla cargada."
      {...props}
    />
  )
}
