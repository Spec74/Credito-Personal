import { useCallback, useMemo } from 'react'
import { Button, Col, Grid, Input, InputNumber, Row, Space, Typography } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import type { PrendaItem } from '../../api/creditoGestion'
import { CredixDataTable } from '../credix'
import { formatMoney } from '../../utils/formatMoney'
import { prendaVacia, totalTasacion } from '../../utils/prendas'

const { Text } = Typography

type Props = {
  value: PrendaItem[]
  onChange: (prendas: PrendaItem[]) => void
  disabled?: boolean
  /** Solo lectura: sin inputs, sin agregar/quitar. */
  readOnly?: boolean
}

function celda(texto: string | null | undefined, vacio = '—') {
  const t = (texto ?? '').trim()
  return t.length > 0 ? t : vacio
}

/**
 * Detalle de bienes en custodia. El crédito admite varios, y la tasación del crédito es la
 * suma de los que tengan descripción: el servidor la recalcula desde lo guardado.
 */
export function PrendasEditor({ value, onChange, disabled = false, readOnly = false }: Props) {
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true
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

  if (isMobile) {
    return (
      <div className="prendas-editor-mobile">
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
              <>
                <div className="prendas-editor-mobile__field">
                  <span>Descripción</span>
                  <Text>{celda(prenda.descripcion)}</Text>
                </div>
                <div className="prendas-editor-mobile__field">
                  <span>Marca</span>
                  <Text>{celda(prenda.marca)}</Text>
                </div>
                <div className="prendas-editor-mobile__field">
                  <span>Modelo</span>
                  <Text>{celda(prenda.modelo)}</Text>
                </div>
                <div className="prendas-editor-mobile__field">
                  <span>Serie</span>
                  <Text>{celda(prenda.serie, 'N/T')}</Text>
                </div>
                <div className="prendas-editor-mobile__field">
                  <span>Color</span>
                  <Text>{celda(prenda.color)}</Text>
                </div>
                <div className="prendas-editor-mobile__field">
                  <span>Código interno</span>
                  <Text>{celda(prenda.codigoInterno)}</Text>
                </div>
                <div className="prendas-editor-mobile__field">
                  <span>Tasación</span>
                  <Text strong>{formatMoney(prenda.valorTasacion)}</Text>
                </div>
                <div className="prendas-editor-mobile__field">
                  <span>Observaciones</span>
                  <Text>{celda(prenda.observaciones)}</Text>
                </div>
              </>
            ) : (
              <>
                <label className="prendas-editor-mobile__field">
                  <span>Descripción</span>
                  <Input
                    disabled={bloqueado}
                    placeholder="Ej. anillo de oro 18k"
                    value={prenda.descripcion}
                    onChange={(e) => actualizar(indice, { descripcion: e.target.value })}
                  />
                </label>
                <label className="prendas-editor-mobile__field">
                  <span>Marca</span>
                  <Input
                    disabled={bloqueado}
                    value={prenda.marca ?? ''}
                    onChange={(e) => actualizar(indice, { marca: e.target.value })}
                  />
                </label>
                <label className="prendas-editor-mobile__field">
                  <span>Modelo</span>
                  <Input
                    disabled={bloqueado}
                    value={prenda.modelo ?? ''}
                    onChange={(e) => actualizar(indice, { modelo: e.target.value })}
                  />
                </label>
                <label className="prendas-editor-mobile__field">
                  <span>Serie</span>
                  <Input
                    disabled={bloqueado}
                    placeholder="N/T"
                    value={prenda.serie ?? ''}
                    onChange={(e) => actualizar(indice, { serie: e.target.value })}
                  />
                </label>
                <label className="prendas-editor-mobile__field">
                  <span>Color</span>
                  <Input
                    disabled={bloqueado}
                    value={prenda.color ?? ''}
                    onChange={(e) => actualizar(indice, { color: e.target.value })}
                  />
                </label>
                <label className="prendas-editor-mobile__field">
                  <span>Código interno</span>
                  <Input
                    disabled={bloqueado}
                    value={prenda.codigoInterno ?? ''}
                    onChange={(e) => actualizar(indice, { codigoInterno: e.target.value })}
                  />
                </label>
                <label className="prendas-editor-mobile__field">
                  <span>Tasación</span>
                  <InputNumber
                    style={{ width: '100%' }}
                    min={0}
                    precision={2}
                    disabled={bloqueado}
                    value={prenda.valorTasacion}
                    onChange={(v) => actualizar(indice, { valorTasacion: v ?? 0 })}
                  />
                </label>
                <label className="prendas-editor-mobile__field">
                  <span>Observaciones</span>
                  <Input
                    disabled={bloqueado}
                    value={prenda.observaciones ?? ''}
                    onChange={(e) => actualizar(indice, { observaciones: e.target.value })}
                  />
                </label>
              </>
            )}
          </article>
        ))}
        {footer}
      </div>
    )
  }

  return (
    <>
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
                <Input
                  disabled={bloqueado}
                  placeholder="Ej. anillo de oro 18k"
                  value={prenda.descripcion}
                  onChange={(e) => actualizar(indice, { descripcion: e.target.value })}
                />
              ),
          },
          {
            title: 'Marca',
            width: 130,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.marca)}</Text>
              ) : (
                <Input
                  disabled={bloqueado}
                  value={prenda.marca ?? ''}
                  onChange={(e) => actualizar(indice, { marca: e.target.value })}
                />
              ),
          },
          {
            title: 'Modelo',
            width: 130,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.modelo)}</Text>
              ) : (
                <Input
                  disabled={bloqueado}
                  value={prenda.modelo ?? ''}
                  onChange={(e) => actualizar(indice, { modelo: e.target.value })}
                />
              ),
          },
          {
            title: 'Serie',
            width: 130,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.serie, 'N/T')}</Text>
              ) : (
                <Input
                  disabled={bloqueado}
                  placeholder="N/T"
                  value={prenda.serie ?? ''}
                  onChange={(e) => actualizar(indice, { serie: e.target.value })}
                />
              ),
          },
          {
            title: 'Color',
            width: 110,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.color)}</Text>
              ) : (
                <Input
                  disabled={bloqueado}
                  value={prenda.color ?? ''}
                  onChange={(e) => actualizar(indice, { color: e.target.value })}
                />
              ),
          },
          {
            title: 'Código interno',
            width: 140,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.codigoInterno)}</Text>
              ) : (
                <Input
                  disabled={bloqueado}
                  value={prenda.codigoInterno ?? ''}
                  onChange={(e) => actualizar(indice, { codigoInterno: e.target.value })}
                />
              ),
          },
          {
            title: 'Tasación',
            width: 140,
            align: 'right',
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text strong>{formatMoney(prenda.valorTasacion)}</Text>
              ) : (
                <InputNumber
                  style={{ width: '100%' }}
                  min={0}
                  precision={2}
                  disabled={bloqueado}
                  value={prenda.valorTasacion}
                  onChange={(v) => actualizar(indice, { valorTasacion: v ?? 0 })}
                />
              ),
          },
          {
            title: 'Observaciones',
            width: 200,
            render: (_, { indice, prenda }) =>
              readOnly ? (
                <Text>{celda(prenda.observaciones)}</Text>
              ) : (
                <Input
                  disabled={bloqueado}
                  value={prenda.observaciones ?? ''}
                  onChange={(e) => actualizar(indice, { observaciones: e.target.value })}
                />
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
    </>
  )
}
