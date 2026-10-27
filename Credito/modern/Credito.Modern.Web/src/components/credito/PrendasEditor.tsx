import { useCallback, useMemo } from 'react'
import { Button, Input, InputNumber, Typography } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import type { PrendaItem } from '../../api/creditoGestion'
import { formatMoney } from '../../utils/formatMoney'
import {
  errorCampoPrenda,
  PRENDA_MAX,
  prendaVacia,
  totalTasacion,
  type PrendaCampoError,
} from '../../utils/prendas'
import '../../styles/prendas-editor.css'

const { Text } = Typography

type Props = {
  value: PrendaItem[]
  onChange: (prendas: PrendaItem[]) => void
  disabled?: boolean
  /** Solo lectura: sin inputs, sin agregar/quitar. */
  readOnly?: boolean
  /** Errores de campo desde `validarPrendasForm` (índice + campo). */
  errores?: PrendaCampoError[]
}

const DETALLE_CAMPOS = [
  { campo: 'marca', label: 'Marca', max: PRENDA_MAX.marca },
  { campo: 'modelo', label: 'Modelo', max: PRENDA_MAX.modelo },
  { campo: 'serie', label: 'Serie', max: PRENDA_MAX.serie, placeholder: 'N/T' },
  { campo: 'color', label: 'Color', max: PRENDA_MAX.color },
  { campo: 'codigoInterno', label: 'Código interno', max: PRENDA_MAX.codigoInterno },
] as const

function celda(texto: string | null | undefined, vacio = '—') {
  const t = (texto ?? '').trim()
  return t.length > 0 ? t : vacio
}

function statusCampo(
  errores: PrendaCampoError[] | undefined,
  indice: number,
  campo: keyof PrendaItem,
): { status?: 'error'; help?: string } {
  if (!errores?.length) return {}
  const mensaje = errorCampoPrenda(errores, indice, campo)
  return mensaje ? { status: 'error', help: mensaje } : {}
}

function extrasLeidos(prenda: PrendaItem): { key: string; label: string; value: string }[] {
  const rows: { key: string; label: string; value: string }[] = []
  if (prenda.marca?.trim()) rows.push({ key: 'marca', label: 'Marca', value: prenda.marca.trim() })
  if (prenda.modelo?.trim()) rows.push({ key: 'modelo', label: 'Modelo', value: prenda.modelo.trim() })
  if (prenda.serie?.trim()) rows.push({ key: 'serie', label: 'Serie', value: prenda.serie.trim() })
  if (prenda.color?.trim()) rows.push({ key: 'color', label: 'Color', value: prenda.color.trim() })
  if (prenda.codigoInterno?.trim()) {
    rows.push({ key: 'codigo', label: 'Código', value: prenda.codigoInterno.trim() })
  }
  if (prenda.observaciones?.trim()) {
    rows.push({ key: 'obs', label: 'Nota', value: prenda.observaciones.trim() })
  }
  return rows
}

/**
 * Bienes en custodia: ficha por bien (no tabla de 9 columnas).
 * Obligatorio: descripción + tasación. El resto identifica el bien.
 */
export function PrendasEditor({
  value,
  onChange,
  disabled = false,
  readOnly = false,
  errores,
}: Props) {
  const bloqueado = disabled || readOnly

  const actualizar = useCallback(
    (indice: number, cambios: Partial<PrendaItem>) => {
      if (bloqueado) return
      onChange(value.map((p, i) => (i === indice ? { ...p, ...cambios } : p)))
    },
    [value, onChange, bloqueado],
  )

  const eliminar = useCallback(
    (indice: number) => {
      if (bloqueado) return
      const restantes = value.filter((_, i) => i !== indice)
      onChange(restantes.length > 0 ? restantes : [prendaVacia()])
    },
    [value, onChange, bloqueado],
  )

  const agregar = useCallback(() => {
    if (bloqueado) return
    onChange([...value, prendaVacia()])
  }, [value, onChange, bloqueado])

  const total = useMemo(() => totalTasacion(value), [value])

  return (
    <div className="prendas-editor">
      {value.map((prenda, indice) => {
        const errDesc = statusCampo(errores, indice, 'descripcion')
        const errTasa = statusCampo(errores, indice, 'valorTasacion')
        const extras = extrasLeidos(prenda)
        return (
          <article key={indice} className="prendas-editor__card">
            <header className="prendas-editor__head">
              <span className="prendas-editor__kicker">Bien {indice + 1}</span>
              {readOnly ? (
                <strong className="prendas-editor__tasacion">{formatMoney(prenda.valorTasacion)}</strong>
              ) : null}
              {!readOnly && value.length > 1 ? (
                <Button
                  type="text"
                  danger
                  size="small"
                  icon={<DeleteOutlined />}
                  disabled={bloqueado}
                  aria-label={`Quitar bien ${indice + 1}`}
                  onClick={() => eliminar(indice)}
                >
                  Quitar
                </Button>
              ) : null}
            </header>

            {readOnly ? (
              <div className="prendas-editor__lectura">
                <p className="prendas-editor__nombre">{celda(prenda.descripcion, 'Sin descripción')}</p>
                {extras.length > 0 ? (
                  <dl className="prendas-editor__dl">
                    {extras.map((x) => (
                      <div key={x.key} className="prendas-editor__dl-item">
                        <dt>{x.label}</dt>
                        <dd>{x.value}</dd>
                      </div>
                    ))}
                  </dl>
                ) : null}
              </div>
            ) : (
              <div className="prendas-editor__form">
                <label className="prendas-editor__field prendas-editor__field--hero">
                  <span>Qué deja en prenda</span>
                  <Input
                    disabled={bloqueado}
                    status={errDesc.status}
                    maxLength={PRENDA_MAX.descripcion}
                    placeholder="Ej. moto KTM Duke, anillo oro 18k"
                    value={prenda.descripcion}
                    onChange={(e) => actualizar(indice, { descripcion: e.target.value })}
                  />
                  {errDesc.help ? <Text type="danger">{errDesc.help}</Text> : null}
                </label>
                <label className="prendas-editor__field prendas-editor__field--money">
                  <span>Tasación (S/)</span>
                  <InputNumber
                    min={0}
                    precision={2}
                    disabled={bloqueado}
                    status={errTasa.status}
                    value={prenda.valorTasacion || null}
                    onChange={(v) => actualizar(indice, { valorTasacion: v ?? 0 })}
                    prefix="S/"
                  />
                  {errTasa.help ? <Text type="danger">{errTasa.help}</Text> : null}
                </label>

                <div className="prendas-editor__detalle">
                  {DETALLE_CAMPOS.map(({ campo, label, max, ...rest }) => {
                    const err = statusCampo(errores, indice, campo)
                    const placeholder = 'placeholder' in rest ? rest.placeholder : undefined
                    return (
                      <label key={campo} className="prendas-editor__field">
                        <span>{label}</span>
                        <Input
                          disabled={bloqueado}
                          status={err.status}
                          maxLength={max}
                          placeholder={placeholder}
                          value={(prenda[campo] as string | null | undefined) ?? ''}
                          onChange={(e) => actualizar(indice, { [campo]: e.target.value })}
                        />
                        {err.help ? <Text type="danger">{err.help}</Text> : null}
                      </label>
                    )
                  })}
                </div>

                <label className="prendas-editor__field prendas-editor__field--wide">
                  <span>Nota del bien</span>
                  <Input
                    disabled={bloqueado}
                    maxLength={PRENDA_MAX.observaciones}
                    placeholder="Opcional"
                    value={prenda.observaciones ?? ''}
                    onChange={(e) => actualizar(indice, { observaciones: e.target.value })}
                  />
                </label>
              </div>
            )}
          </article>
        )
      })}

      <div className="prendas-editor__footer">
        {!readOnly ? (
          <Button icon={<PlusOutlined />} disabled={bloqueado} onClick={agregar}>
            Otro bien
          </Button>
        ) : (
          <span />
        )}
        <div className="prendas-editor__total">
          <Text type="secondary">Tasación total</Text>
          <Text strong>{formatMoney(total)}</Text>
        </div>
      </div>
    </div>
  )
}
