import { useQuery } from '@tanstack/react-query'
import { Form, Select } from 'antd'
import type { DefaultOptionType } from 'antd/es/select'
import { fetchOficinas } from '../../api/oficinas'
import { normalizeSearchText } from '../../utils/normalizeSearch'
import {
  fetchUsuariosGestion,
  fetchUsuariosReporteGestores,
} from '../../api/usuariosAdmin'

export function OficinaSelect({
  value,
  onChange,
  allowAll,
  size = 'small',
}: {
  value?: number
  onChange?: (v: number | undefined) => void
  allowAll?: boolean
  size?: 'small' | 'middle' | 'large'
}) {
  const oficinas = useQuery({ queryKey: ['oficinas-list'], queryFn: fetchOficinas })
  const options = [
    ...(allowAll ? [{ value: 0, label: 'TODOS' }] : []),
    ...(oficinas.data?.map((o) => ({
      value: o.oficinaId,
      label: o.denominacion,
    })) ?? []),
  ]
  return (
    <Select
      size={size}
      loading={oficinas.isLoading}
      options={options}
      value={value ?? (allowAll ? 0 : undefined)}
      onChange={(v) => onChange?.(v === 0 ? undefined : v)}
      style={{ width: '100%' }}
    />
  )
}

export function GestorSelect({
  value,
  onChange,
  allowAll,
  legacyList,
  size = 'small',
  onEnter,
  onGestorResolved,
}: {
  value?: number
  onChange?: (v: number | undefined) => void
  allowAll?: boolean
  /** Lista completa de usuarios activos (paridad CobranzaPagos.cshtml / UsuarioBL.Listar). */
  legacyList?: boolean
  size?: 'small' | 'middle' | 'large'
  /** Enter en el buscador del combo (confirmar búsqueda). */
  onEnter?: () => void
  /** Tras elegir un gestor concreto (id > 0). */
  onGestorResolved?: (usuarioId: number) => void
}) {
  const usuariosGestion = useQuery({
    queryKey: ['usuarios-gestores-reporte', 'gestion'],
    queryFn: () => fetchUsuariosGestion({ page: 1, pageSize: 500, incluirInactivos: false }),
    enabled: !legacyList,
  })
  const usuariosLegacy = useQuery({
    queryKey: ['usuarios-gestores-reporte', 'legacy'],
    queryFn: fetchUsuariosReporteGestores,
    enabled: legacyList === true,
  })
  const usuarios = legacyList ? usuariosLegacy : usuariosGestion
  const rows = legacyList
    ? (usuariosLegacy.data ?? [])
    : (usuariosGestion.data?.rows ?? [])
  const options = [
    ...(allowAll ? [{ value: 0, label: 'TODOS' }] : []),
    ...rows.map((u) => ({
      value: u.usuarioId,
      label: u.nombreCompleto || u.nombreUsuario,
    })),
  ]
  const filterGestor = (input: string, option?: DefaultOptionType) => {
    const label = String(option?.label ?? '')
    const q = normalizeSearchText(input)
    if (!q) return true
    const h = normalizeSearchText(label)
    return q.split(/\s+/).every((t) => h.includes(t))
  }

  const selectValue = value != null && value > 0 ? value : allowAll ? 0 : undefined

  const commit = (v: number | string | null | undefined) => {
    const id = v == null || v === 0 ? undefined : Number(v)
    onChange?.(id)
    if (id != null && id > 0) {
      onGestorResolved?.(id)
    }
  }

  return (
    <Select
      size={size}
      showSearch
      allowClear={allowAll}
      autoClearSearchValue
      optionFilterProp="label"
      filterOption={filterGestor}
      loading={usuarios.isLoading}
      options={options}
      value={selectValue}
      onChange={commit}
      onSelect={commit}
      onInputKeyDown={(e) => {
        if (e.key === 'Enter') {
          e.preventDefault()
          onEnter?.()
        }
      }}
      style={{ width: '100%' }}
      placeholder={allowAll ? 'Buscar o elegir gestor…' : 'Buscar gestor…'}
    />
  )
}

export function ReporteField({
  label,
  children,
}: {
  label: string
  children: React.ReactNode
}) {
  return (
    <Form.Item label={label} style={{ marginBottom: 8 }} labelCol={{ span: 24 }}>
      {children}
    </Form.Item>
  )
}
