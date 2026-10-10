import { useCallback, useMemo } from 'react'
import { Button, Col, Grid, Input, InputNumber, Row, Space, Typography } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import type { PrendaItem } from '../../api/creditoGestion'
import { CredixDataTable } from '../credix'
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

/**
 * Detalle de bienes en custodia. El crédito admite varios, y la tasación del crédito es la
 * suma de los que tengan descripción: el servidor la recalcula desde lo guardado.
 */
export function PrendasEditor({
  value,
  onChange,
  disabled = false,
  readOnly = false,
  errores,
}: Props) {
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true
  const bloqueado = disabled || readOnly
  /** Consulta: ficha etiqueta/valor en móvil y PC (evita tabla apretada / texto pegado). */
  const useDetailCards = readOnly || isMobile

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

  const filas = useMemo(
    () => value.map((prenda, indice) => ({ key: indice, indice, prenda })),
    [value],
  )

  const total = useMemo(() => totalTasacion(value), [value])

  const footer = (
    <Row justify="space-between" align="middle" gutter={[8, 8]} style={{ marginTop: 12 }}>
      {!readOnly ? (
        <Col xs={24} sm="auto">
          <Button block={isMobile} icon={<PlusOutlined />} disabled={bloqueado} onClick={agregar}>
            Agregar bien
          </Button>
        </Col>
      ) : (
        <Col />
      )}
      <Col xs={24} sm="auto">
        <Space>
          <Text type="secondary">Tasación total</Text>
          <Text strong>{formatMoney(total)}</Text>
        </Space>
      </Col>
    </Row>
  )

  const renderInput = (
    indice: number,
    campo: keyof PrendaItem,
    prenda: PrendaItem,
    opts?: { placeholder?: string; maxLength?: number },
  ) => {
    const err = statusCampo(errores, indice, campo)
    return (
      <div>
        <Input
          disabled={bloqueado}
          status={err.status}
          maxLength={opts?.maxLength}
          placeholder={opts?.placeholder}
          value={(prenda[campo] as string | null | undefined) ?? ''}
          onChange={(e) => actualizar(indice, { [campo]: e.target.value })}
        />
        {err.help ? (
          <Text type="danger" style={{ fontSize: 12 }}>
            {err.help}
          </Text>
        ) : null}
      </div>
    )
  }

  if (useDetailCards) {
    return (
      <div className={`prendas-editor-mobile${readOnly ? ' prendas-editor-mobile--readonly' : ''}`}>
        {value.map((prenda, indice) => (
          <article key={indice} className="prendas-editor-mobile__card">
            <div className="prendas-editor-mobile__head">
              <Text strong>Bien #{indice + 1}</Text>
              {!readOnly ? (
                <Button
                  type="text"
                  danger
                  size="small"
                  icon={<DeleteOutlined />}
                  disabled={bloqueado}
                  aria-label={`Quitar bien ${indice + 1}`}
                  onClick={() => eliminar(indice)}
                />
              ) : null}
            </div>
            {readOnly ? (
              <dl className="prendas-editor-mobile__dl">
                {(
                  [
                    ['Descripción', celda(prenda.descripcion)],
                    ['Marca', celda(prenda.marca)],
                    ['Modelo', celda(prenda.modelo)],
                    ['Serie', celda(prenda.serie, 'N/T')],
                    ['Color', celda(prenda.color)],
                    ['Código interno', celda(prenda.codigoInterno)],
                    ['Tasación', formatMoney(prenda.valorTasacion)],
                    ['Observaciones', celda(prenda.observaciones)],
                  ] as const
                ).map(([label, valor]) => (
                  <div key={label} className="prendas-editor-mobile__field">
                    <dt className="prendas-editor-mobile__label">{label}</dt>
                    <dd
                      className={
                        valor === '—'
                          ? 'prendas-editor-mobile__value prendas-editor-mobile__value--muted'
                          : 'prendas-editor-mobile__value'
                      }
                    >
                      {valor}
                    </dd>
                  </div>
                ))}
              </dl>
            ) : (
              <>
                {(
                  [
                    ['descripcion', 'Descripción', PRENDA_MAX.descripcion, 'Ej. anillo de oro 18k'],
                    ['marca', 'Marca', PRENDA_MAX.marca, undefined],
                    ['modelo', 'Modelo', PRENDA_MAX.modelo, undefined],
                    ['serie', 'Serie', PRENDA_MAX.serie, 'N/T'],
                    ['color', 'Color', PRENDA_MAX.color, undefined],
                    ['codigoInterno', 'Código interno', PRENDA_MAX.codigoInterno, undefined],
                    ['observaciones', 'Observaciones', PRENDA_MAX.observaciones, undefined],
                  ] as const
                ).map(([campo, label, max, placeholder]) => {
                  const err = statusCampo(errores, indice, campo)
                  return (
                    <label key={campo} className="prendas-editor-mobile__field prendas-editor-mobile__field--edit">
                      <span className="prendas-editor-mobile__label">{label}</span>
                      <Input
                        disabled={bloqueado}
                        status={err.status}
                        maxLength={max}
                        placeholder={placeholder}
                        value={(prenda[campo] as string | null | undefined) ?? ''}
                        onChange={(e) => actualizar(indice, { [campo]: e.target.value })}
                      />
                      {err.help ? (
                        <Text type="danger" style={{ fontSize: 12 }}>
                          {err.help}
                        </Text>
                      ) : null}
                    </label>
                  )
                })}
                {(() => {
                  const err = statusCampo(errores, indice, 'valorTasacion')
                  return (
                    <label className="prendas-editor-mobile__field prendas-editor-mobile__field--edit">
                      <span className="prendas-editor-mobile__label">Tasación</span>
                      <InputNumber
                        style={{ width: '100%' }}
                        min={0}
                        precision={2}
                        disabled={bloqueado}
                        status={err.status}
                        value={prenda.valorTasacion}
                        onChange={(v) => actualizar(indice, { valorTasacion: v ?? 0 })}
                      />
                      {err.help ? (
                        <Text type="danger" style={{ fontSize: 12 }}>
                          {err.help}
                        </Text>
                      ) : null}
                    </label>
                  )
                })()}
              </>
            )}
          </article>
        ))}
        {footer}
      </div>
    )
  }

  return (
    <div className="prendas-editor-desktop-wrap">
      <CredixDataTable
        mode="operacion"
        pagination={false}
        dataSource={filas}
        rowKey="key"
        scroll={{ x: readOnly ? 960 : 1100 }}
        columns={[
          {
            title: 'Descripción',
            width: 240,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.descripcion)}</Text>
              ) : (
                renderInput(indice, 'descripcion', prenda, {
                  placeholder: 'Ej. anillo de oro 18k',
                  maxLength: PRENDA_MAX.descripcion,
                })
              ),
          },
          {
            title: 'Marca',
            width: 130,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.marca)}</Text>
              ) : (
                renderInput(indice, 'marca', prenda, { maxLength: PRENDA_MAX.marca })
              ),
          },
          {
            title: 'Modelo',
            width: 130,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.modelo)}</Text>
              ) : (
                renderInput(indice, 'modelo', prenda, { maxLength: PRENDA_MAX.modelo })
              ),
          },
          {
            title: 'Serie',
            width: 130,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.serie, 'N/T')}</Text>
              ) : (
                renderInput(indice, 'serie', prenda, {
                  placeholder: 'N/T',
                  maxLength: PRENDA_MAX.serie,
                })
              ),
          },
          {
            title: 'Color',
            width: 110,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.color)}</Text>
              ) : (
                renderInput(indice, 'color', prenda, { maxLength: PRENDA_MAX.color })
              ),
          },
          {
            title: 'Código interno',
            width: 140,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.codigoInterno)}</Text>
              ) : (
                renderInput(indice, 'codigoInterno', prenda, {
                  maxLength: PRENDA_MAX.codigoInterno,
                })
              ),
          },
          {
            title: 'Tasación',
            width: 140,
            align: 'right',
            render: (_, { indice, prenda }) => {
              if (readOnly) {
                return <Text strong>{formatMoney(prenda.valorTasacion)}</Text>
              }
              const err = statusCampo(errores, indice, 'valorTasacion')
              return (
                <div>
                  <InputNumber
                    style={{ width: '100%' }}
                    min={0}
                    precision={2}
                    disabled={bloqueado}
                    status={err.status}
                    value={prenda.valorTasacion}
                    onChange={(v) => actualizar(indice, { valorTasacion: v ?? 0 })}
                  />
                  {err.help ? (
                    <Text type="danger" style={{ fontSize: 12 }}>
                      {err.help}
                    </Text>
                  ) : null}
                </div>
              )
            },
          },
          {
            title: 'Observaciones',
            width: 200,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.observaciones)}</Text>
              ) : (
                renderInput(indice, 'observaciones', prenda, {
                  maxLength: PRENDA_MAX.observaciones,
                })
              ),
          },
          ...(readOnly
            ? []
            : [
                {
                  title: 'Acciones',
                  key: 'acciones',
                  width: 48,
                  render: (_: unknown, { indice }: { indice: number }) => (
                    <Button
                      type="text"
                      danger
                      size="small"
                      icon={<DeleteOutlined />}
                      disabled={bloqueado}
                      aria-label={`Quitar bien ${indice + 1}`}
                      onClick={() => eliminar(indice)}
                    />
                  ),
                },
              ]),
        ]}
      />
      {footer}
    </div>
  )
}
