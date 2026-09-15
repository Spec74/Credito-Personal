import { useCallback, useEffect, useRef, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { AutoComplete, Button, Input, Typography, message } from 'antd'
import { LoadingOutlined, SearchOutlined } from '@ant-design/icons'
import {
  buscarCreditosTarea,
  type CreditoTareaBuscar,
} from '../../api/tareas'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import {
  extractSearchTermsFromInput,
  pickBestClienteMatch,
} from '../caja/clienteBuscarResolve'
import { CredixDataTable } from '../credix'

const { Text } = Typography

type Props = {
  onSelect: (credito: CreditoTareaBuscar) => void
}

export function TareasBuscarCredito({ onSelect }: Props) {
  const [term, setTerm] = useState('')
  const [options, setOptions] = useState<CreditoTareaBuscar[]>([])
  const [open, setOpen] = useState(false)
  const debounced = useDebouncedValue(term.trim(), 300)
  const lastFetchRef = useRef('')

  const buscar = useMutation({
    mutationFn: (t: string) => buscarCreditosTarea(t),
    onSuccess: (hits, searchedTerm) => {
      setOptions(hits)
      lastFetchRef.current = searchedTerm
      if (hits.length > 0) {
        setOpen(true)
      }
    },
    onError: () => message.error('No se pudo buscar créditos'),
  })

  useEffect(() => {
    if (debounced.length < 2) {
      setOptions([])
      setOpen(false)
      return
    }
    if (debounced === lastFetchRef.current) {
      return
    }
    buscar.mutate(debounced)
  }, [debounced, buscar])

  const ejecutarBusqueda = useCallback(() => {
    const t = term.trim()
    if (t.length < 2) {
      message.warning('Escriba al menos 2 caracteres (DNI, nombre o N° crédito)')
      return
    }
    buscar.mutate(t, {
      onSuccess: (hits) => {
        if (hits.length === 1) {
          onSelect(hits[0])
          setTerm(hits[0].label)
          setOpen(false)
          message.success('Crédito seleccionado')
          return
        }
        const best = pickBestClienteMatch(
          t,
          hits.map((h) => ({ personaId: h.personaId, label: h.label })),
        )
        if (best) {
          const hit = hits.find((h) => h.personaId === best.personaId && h.label === best.label)
          if (hit) {
            onSelect(hit)
            setTerm(hit.label)
            setOpen(false)
            message.success('Crédito seleccionado')
          }
        }
      },
    })
  }, [term, buscar, onSelect])

  const autoOptions = options.map((h) => ({
    value: h.label,
    label: (
      <div className="credito-tareas-buscar-option">
        <span className="credito-tareas-buscar-option__name">{h.nombre}</span>
        <span className="credito-tareas-buscar-option__meta">
          DNI {h.dni} · Crédito #{h.creditoId} · {h.montoCredito.toFixed(2)}
        </span>
      </div>
    ),
    hit: h,
  }))

  const busy = buscar.isPending

  return (
    <div className="credito-tareas-buscar">
      <div className="credito-tareas-buscar__row">
        <AutoComplete
          className="credito-tareas-buscar__input"
          value={term}
          options={autoOptions}
          open={open && options.length > 0 && !busy}
          onOpenChange={setOpen}
          onSearch={setTerm}
          onSelect={(_, opt) => {
            const hit = (opt as { hit?: CreditoTareaBuscar }).hit
            if (hit) {
              setTerm(hit.label)
              setOpen(false)
              onSelect(hit)
            }
          }}
          notFoundContent={
            busy
              ? 'Buscando…'
              : term.trim().length >= 2
                ? 'Sin créditos desembolsados'
                : 'Mínimo 2 caracteres'
          }
          popupClassName="credito-tareas-buscar-dropdown"
        >
          <Input
            size="large"
            allowClear
            placeholder="DNI, nombre, N° crédito o etiqueta [código]"
            prefix={<SearchOutlined className="credito-tareas-buscar__icon" />}
            suffix={busy ? <LoadingOutlined spin /> : null}
            onPressEnter={(e) => {
              e.preventDefault()
              ejecutarBusqueda()
            }}
          />
        </AutoComplete>
        <Button
          type="primary"
          size="large"
          className="credito-tareas-buscar__btn"
          icon={<SearchOutlined />}
          loading={busy}
          disabled={term.trim().length < 2}
          onClick={ejecutarBusqueda}
        >
          Buscar crédito
        </Button>
      </div>

      {options.length > 0 && term.trim().length >= 2 ? (
        <div className="credito-tareas-buscar__tabla">
          <Text type="secondary" className="credito-tareas-buscar__hint">
            {options.length} crédito(s)
            {extractSearchTermsFromInput(term).length > 1
              ? ` · búsqueda: ${extractSearchTermsFromInput(term).join(', ')}`
              : ''}
            . Elija uno, Enter con coincidencia única o use el desplegable.
          </Text>
          <CredixDataTable<CreditoTareaBuscar>
            mode="operacion"
            size="small"
            rowKey="creditoId"
            pagination={false}
            dataSource={options}
            columns={[
              { title: 'Crédito', dataIndex: 'creditoId', align: 'center' },
              { title: 'DNI', dataIndex: 'dni' },
              { title: 'Cliente', dataIndex: 'nombre', ellipsis: true, minWidth: 140 },
              {
                title: 'Monto',
                dataIndex: 'montoCredito',
                align: 'right',
                render: (v: number) => `S/. ${v.toFixed(2)}`,
              },
              {
                title: '',
                key: 'sel',
                render: (_, row) => (
                  <Button type="primary" size="small" onClick={() => onSelect(row)}>
                    Elegir
                  </Button>
                ),
              },
            ]}
          />
        </div>
      ) : null}
    </div>
  )
}
