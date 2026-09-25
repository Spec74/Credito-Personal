import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { AutoComplete, Button, Input, message, type InputRef } from 'antd'
import { LoadingOutlined, SearchOutlined, UserOutlined } from '@ant-design/icons'
import { buscarClientes } from '../../api/cajaDiario'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import {
  codigoEnLabel,
  extractSearchTermsFromInput,
  findHitInList,
  pickBestClienteMatch,
  type ClienteHit,
} from './clienteBuscarResolve'

export type ClienteBuscarOption = {
  personaId: number
  value: string
  /** Etiqueta API (texto) para coincidencias y confirmación. */
  hitLabel: string
  /** Contenido visible en el desplegable Ant Design. */
  label: ReactNode
}

export type ClienteBuscarVariant = 'default' | 'credito'

type Props = {
  value: string
  onChange: (label: string) => void
  onSelectPersona: (personaId: number, label: string) => void
  placeholder?: string
  minWidth?: number
  minChars?: number
  debounceMs?: number
  autoFocus?: boolean
  fullWidth?: boolean
  showSearchButton?: boolean
  searchButtonLabel?: string
  ariaLabelledBy?: string
  /** `credito`: buscador destacado en consulta de crédito (botón siempre visible). */
  variant?: ClienteBuscarVariant
}

const norm = (s: string) => s.trim().toLowerCase()

function optionLabel(h: ClienteHit, variant: ClienteBuscarVariant): ReactNode {
  if (variant !== 'credito') {
    return h.label
  }
  const code = codigoEnLabel(h.label)
  const name = h.label.replace(/\s*\[[^\]]+\]\s*$/, '').trim()
  return (
    <div className="credito-buscar-cliente-option">
      <span className="credito-buscar-cliente-option__name">{name}</span>
      {code ? (
        <span className="credito-buscar-cliente-option__code">{code}</span>
      ) : null}
    </div>
  )
}

function toOptions(hits: ClienteHit[], variant: ClienteBuscarVariant): ClienteBuscarOption[] {
  return hits.map((h) => ({
    personaId: h.personaId,
    value: h.label,
    hitLabel: h.label,
    label: optionLabel(h, variant),
  }))
}

function toHits(options: ClienteBuscarOption[]): ClienteHit[] {
  return options.map((o) => ({
    personaId: o.personaId,
    label: o.hitLabel,
  }))
}

export function ClienteBuscarAutoComplete({
  value,
  onChange,
  onSelectPersona,
  placeholder = 'DNI, nombre o código — al elegir en la lista se carga solo',
  minWidth = 320,
  minChars = 2,
  debounceMs = 350,
  autoFocus = false,
  fullWidth = false,
  showSearchButton = false,
  searchButtonLabel = 'Buscar',
  ariaLabelledBy,
  variant = 'default',
}: Props) {
  const [options, setOptions] = useState<ClienteBuscarOption[]>([])
  const [loading, setLoading] = useState(false)
  const [resolving, setResolving] = useState(false)
  const [open, setOpen] = useState(false)
  const [fetchedTerm, setFetchedTerm] = useState('')
  const selectedRef = useRef<ClienteHit | null>(null)
  const inputRef = useRef<InputRef>(null)
  const debounced = useDebouncedValue(value.trim(), debounceMs)

  useEffect(() => {
    if (!value.trim()) {
      selectedRef.current = null
      return
    }
    if (
      selectedRef.current &&
      norm(value) !== norm(selectedRef.current.label)
    ) {
      selectedRef.current = null
    }
  }, [value])

  const confirmPersona = useCallback(
    (hit: ClienteHit, opts?: { fromList?: boolean }) => {
      selectedRef.current = hit
      onChange(hit.label)
      onSelectPersona(hit.personaId, hit.label)
      setOpen(false)
      if (!opts?.fromList) {
        // El flujo de cobranzas ya muestra el cliente seleccionado; sin toast.
      }
    },
    [onChange, onSelectPersona],
  )

  const runSearch = useCallback(
    async (term: string): Promise<ClienteBuscarOption[]> => {
      const hits = await buscarClientes(term)
      const opts = toOptions(hits, variant)
      setOptions(opts)
      setFetchedTerm(term)
      return opts
    },
    [variant],
  )

  const searchWithFallbacks = useCallback(
    async (term: string): Promise<ClienteBuscarOption[]> => {
      const candidates = extractSearchTermsFromInput(term)
      let last: ClienteBuscarOption[] = []

      for (const candidate of candidates) {
        const opts = await runSearch(candidate)
        last = opts
        if (opts.length > 0) {
          return opts
        }
      }

      return last
    },
    [runSearch],
  )

  useEffect(() => {
    if (debounced.length < minChars) {
      setOptions([])
      setFetchedTerm('')
      setLoading(false)
      return
    }

    if (
      selectedRef.current &&
      norm(debounced) === norm(selectedRef.current.label)
    ) {
      return
    }

    let cancelled = false
    setLoading(true)
    void searchWithFallbacks(debounced)
      .then((opts) => {
        if (cancelled) {
          return
        }
        if (opts.length > 0 && norm(debounced) !== norm(selectedRef.current?.label ?? '')) {
          setOpen(true)
        }
      })
      .catch(() => {
        if (!cancelled) {
          setOptions([])
          setFetchedTerm('')
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [debounced, minChars, searchWithFallbacks])

  const resolveCliente = useCallback(
    async (rawTerm?: string): Promise<boolean> => {
      const term = (rawTerm ?? value).trim()
      if (term.length < minChars) {
        message.warning(`Escriba al menos ${minChars} caracteres para buscar`)
        inputRef.current?.focus()
        return false
      }

      if (
        selectedRef.current &&
        norm(term) === norm(selectedRef.current.label)
      ) {
        onSelectPersona(
          selectedRef.current.personaId,
          selectedRef.current.label,
        )
        return true
      }

      const inCache = findHitInList(term, toHits(options))
      if (inCache) {
        confirmPersona(inCache)
        return true
      }

      setResolving(true)
      try {
        let opts = options
        const cacheHit = findHitInList(term, toHits(opts))
        if (cacheHit) {
          confirmPersona(cacheHit)
          return true
        }

        if (fetchedTerm !== term || opts.length === 0) {
          opts = await searchWithFallbacks(term)
        }

        if (opts.length === 0) {
          message.warning(
            'No se encontró el cliente. Busque por DNI, nombre o código (no use solo la etiqueta completa).',
          )
          setOpen(false)
          return false
        }

        const hits = toHits(opts)
        const picked =
          findHitInList(term, hits) ?? pickBestClienteMatch(term, hits)
        if (picked) {
          confirmPersona(picked)
          return true
        }

        setOpen(true)
        message.info(
          `${opts.length} clientes encontrados. Elija uno de la lista.`,
        )
        return false
      } catch {
        message.error('No se pudo buscar el cliente. Intente de nuevo.')
        return false
      } finally {
        setResolving(false)
      }
    },
    [
      value,
      minChars,
      options,
      fetchedTerm,
      searchWithFallbacks,
      confirmPersona,
      onSelectPersona,
    ],
  )

  const busy = loading || resolving

  const input = (
    <Input
      ref={inputRef}
      size="large"
      allowClear
      autoFocus={autoFocus}
      prefix={<SearchOutlined className="caja-cliente-buscar__icon" />}
      suffix={
        busy ? (
          <LoadingOutlined className="caja-cliente-buscar__icon-muted" spin />
        ) : options.length > 0 && value.trim().length >= minChars ? (
          <span className="credito-buscar-cliente__hits" aria-live="polite">
            {options.length}
          </span>
        ) : value.trim().length > 0 ? (
          <UserOutlined className="caja-cliente-buscar__icon-muted" />
        ) : null
      }
      placeholder={placeholder}
      aria-label={ariaLabelledBy ? undefined : 'Buscar cliente'}
      aria-labelledby={ariaLabelledBy}
      onPressEnter={(e) => {
        e.preventDefault()
        void resolveCliente()
      }}
      onKeyDown={(e) => {
        if (e.key === 'ArrowDown' && options.length > 0) {
          setOpen(true)
        }
        if (e.key === 'Escape') {
          setOpen(false)
        }
      }}
    />
  )

  const autoClass =
    variant === 'credito' ? 'credito-buscar-cliente-input' : 'caja-cliente-buscar'

  const autoComplete = (
    <AutoComplete
      className={autoClass}
      popupClassName={
        variant === 'credito' ? 'credito-buscar-cliente-dropdown' : undefined
      }
      style={{ minWidth: showSearchButton ? undefined : minWidth, width: '100%' }}
      options={options}
      value={value}
      open={open && options.length > 0 && !busy}
      onOpenChange={(next) => {
        if (!busy) {
          setOpen(next)
        }
      }}
      onSearch={(v) => {
        onChange(v)
        if (v.trim().length < minChars) {
          setOpen(false)
          setFetchedTerm('')
        }
      }}
      onSelect={(selectedValue, option) => {
        const fromOption = option as ClienteBuscarOption
        const row =
          fromOption?.personaId != null
            ? fromOption
            : options.find((o) => o.value === selectedValue)
        if (row) {
          confirmPersona(
            { personaId: row.personaId, label: row.hitLabel },
            { fromList: true },
          )
        }
      }}
      onBlur={() => {
        window.setTimeout(() => setOpen(false), 150)
      }}
      notFoundContent={
        busy
          ? 'Buscando…'
          : value.trim().length >= minChars
            ? 'Sin resultados'
            : `Mínimo ${minChars} caracteres`
      }
    >
      {input}
    </AutoComplete>
  )

  if (!showSearchButton) {
    return autoComplete
  }

  const wrapClass = [
    variant === 'credito'
      ? 'credito-buscar-cliente-wrap'
      : 'caja-cliente-buscar-wrap',
    fullWidth ? 'caja-cliente-buscar-wrap--full credito-buscar-cliente-wrap--full' : '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div
      className={wrapClass}
      style={fullWidth ? undefined : { minWidth }}
    >
      {autoComplete}
      <Button
        type="primary"
        size="large"
        className={
          variant === 'credito'
            ? 'credito-buscar-cliente__submit'
            : 'caja-cliente-buscar__submit'
        }
        icon={<SearchOutlined />}
        loading={resolving}
        disabled={value.trim().length < minChars}
        onClick={() => void resolveCliente()}
      >
        <span
          className={
            variant === 'credito'
              ? 'credito-buscar-cliente__submit-text'
              : 'caja-cliente-buscar__submit-text'
          }
        >
          {searchButtonLabel}
        </span>
      </Button>
    </div>
  )
}
