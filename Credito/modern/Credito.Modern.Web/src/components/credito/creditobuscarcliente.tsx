import { ClienteBuscarAutoComplete } from '../caja/ClienteBuscarAutoComplete'

type Props = {
  value: string
  onChange: (label: string) => void
  onSelectPersona: (personaId: number, label: string) => void
  ariaLabelledBy?: string
}

/**
 * Buscador de cliente para consulta de crédito: botón visible, diseño responsivo y variant crédito.
 */
export function CreditoBuscarCliente({
  value,
  onChange,
  onSelectPersona,
  ariaLabelledBy,
}: Props) {
  return (
    <ClienteBuscarAutoComplete
      variant="credito"
      value={value}
      onChange={onChange}
      onSelectPersona={onSelectPersona}
      fullWidth
      showSearchButton
      searchButtonLabel="Buscar cliente"
      debounceMs={280}
      minChars={2}
      placeholder="DNI, nombre o código del cliente"
      ariaLabelledBy={ariaLabelledBy}
    />
  )
}
