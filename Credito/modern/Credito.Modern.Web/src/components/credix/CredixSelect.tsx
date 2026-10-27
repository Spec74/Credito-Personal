import { Select, type SelectProps } from 'antd'

/** Select operativo: busca, no se recorta en drawers y explica el vacío. */
export function CredixSelect<ValueType = string | number>(props: SelectProps<ValueType>) {
  return (
    <Select<ValueType>
      showSearch
      optionFilterProp="label"
      getPopupContainer={() => document.body}
      notFoundContent="Sin opciones. Cree el dato o cambie la búsqueda."
      {...props}
    />
  )
}
