import { useQuery } from '@tanstack/react-query'
import { AutoComplete } from 'antd'
import { buscarPersonasConyuge } from '../../../api/clientes'
import { useDebouncedValue } from '../../../hooks/useDebouncedValue'

type Props = {
  value?: string
  onChange?: (v: string) => void
  onPick: (personaId: number, label: string) => void
}

export function ConyugueAutoComplete({ value, onChange, onPick }: Props) {
  const debounced = useDebouncedValue((value ?? '').trim(), 300)
  const q = useQuery({
    queryKey: ['personas-buscar', debounced],
    queryFn: () => buscarPersonasConyuge(debounced),
    enabled: debounced.length >= 2,
  })

  return (
    <AutoComplete
      value={value}
      onChange={onChange}
      onSearch={onChange}
      options={(q.data ?? []).map((p) => ({
        value: p.label,
        label: p.label,
        id: p.personaId,
      }))}
      onSelect={(_, opt) => onPick((opt as { id: number }).id, String(opt.value))}
      placeholder="Buscar por DNI o nombre…"
    />
  )
}
