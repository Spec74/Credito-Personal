import { ClienteBuscarAutoComplete } from '../caja/ClienteBuscarAutoComplete'

type Props = {
  value: string
  onChange: (label: string) => void
  onSelectPersona: (personaId: number, label: string) => void
  ariaLabelledBy?: string
  autoFocus?: boolean
  autoSelectSingle?: boolean
  onMiss?: (term: string) => Promise<boolean>
  placeholder?: string
  searchButtonLabel?: string
}

/**
 * Buscador de cliente para consulta de crédito: botón visible, diseño responsivo y variant crédito.
 */
export function CreditoBuscarCliente({
  value,
  onChange,
  onSelectPersona,
  ariaLabelledBy,
  autoFocus = false,
  autoSelectSingle = true,
  onMiss,
  placeholder = 'DNI, nombre, código o nro. de crédito',
  searchButtonLabel = 'Buscar',
}: Props) {
  return (
    <ClienteBuscarAutoComplete
      variant="credito"
      value={value}
      onChange={onChange}
      onSelectPersona={onSelectPersona}
      fullWidth
      showSearchButton
      searchButtonLabel={searchButtonLabel}
      debounceMs={220}
      minChars={2}
      placeholder={placeholder}
      ariaLabelledBy={ariaLabelledBy}
      autoFocus={autoFocus}
      autoSelectSingle={autoSelectSingle}
      onMiss={onMiss}
    />
  )
}
