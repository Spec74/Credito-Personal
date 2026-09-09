import { useCallback, useMemo } from 'react'
import { Button, Col, Input, InputNumber, Row, Space, Table, Typography } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import type { PrendaItem } from '../../api/creditoGestion'
import { formatMoney } from '../../utils/formatMoney'
import { prendaVacia, totalTasacion } from '../../utils/prendas'

const { Text } = Typography

type Props = {
  value: PrendaItem[]
  onChange: (prendas: PrendaItem[]) => void
  disabled?: boolean
}

/**
 * Detalle de bienes en custodia. El crédito admite varios, y la tasación del crédito es la
 * suma de los que tengan descripción: el servidor la recalcula desde lo guardado.
 */
export function PrendasEditor({ value, onChange, disabled = false }: Props) {
  const actualizar = useCallback(
    (indice: number, cambios: Partial<PrendaItem>) => {
      onChange(value.map((p, i) => (i === indice ? { ...p, ...cambios } : p)))
    },
    [value, onChange],
  )

  const eliminar = useCallback(
    (indice: number) => {
      const restantes = value.filter((_, i) => i !== indice)
      onChange(restantes.length > 0 ? restantes : [prendaVacia()])
    },
    [value, onChange],
  )

  const agregar = useCallback(() => {
    onChange([...value, prendaVacia()])
  }, [value, onChange])

  const filas = useMemo(
    () => value.map((prenda, indice) => ({ key: indice, indice, prenda })),
    [value],
  )

  const total = useMemo(() => totalTasacion(value), [value])

  return (
    <>
      <Table
        size="small"
        pagination={false}
        dataSource={filas}
        scroll={{ x: 'max-content' }}
        columns={[
          {
            title: 'Descripción',
            width: 240,
            fixed: 'left',
            render: (_, { indice, prenda }) => (
              <Input
                disabled={disabled}
                placeholder="Ej. anillo de oro 18k"
                value={prenda.descripcion}
                onChange={(e) => actualizar(indice, { descripcion: e.target.value })}
              />
            ),
          },
          {
            title: 'Marca',
            width: 130,
            render: (_, { indice, prenda }) => (
              <Input
                disabled={disabled}
                value={prenda.marca ?? ''}
                onChange={(e) => actualizar(indice, { marca: e.target.value })}
              />
            ),
          },
          {
            title: 'Modelo',
            width: 130,
            render: (_, { indice, prenda }) => (
              <Input
                disabled={disabled}
                value={prenda.modelo ?? ''}
                onChange={(e) => actualizar(indice, { modelo: e.target.value })}
              />
            ),
          },
          {
            title: 'Serie',
            width: 130,
            render: (_, { indice, prenda }) => (
              <Input
                disabled={disabled}
                placeholder="N/T"
                value={prenda.serie ?? ''}
                onChange={(e) => actualizar(indice, { serie: e.target.value })}
              />
            ),
          },
          {
            title: 'Color',
            width: 110,
            render: (_, { indice, prenda }) => (
              <Input
                disabled={disabled}
                value={prenda.color ?? ''}
                onChange={(e) => actualizar(indice, { color: e.target.value })}
              />
            ),
          },
          {
            title: 'Código interno',
            width: 140,
            render: (_, { indice, prenda }) => (
              <Input
                disabled={disabled}
                value={prenda.codigoInterno ?? ''}
                onChange={(e) => actualizar(indice, { codigoInterno: e.target.value })}
              />
            ),
          },
          {
            title: 'Tasación',
            width: 140,
            align: 'right',
            render: (_, { indice, prenda }) => (
              <InputNumber
                style={{ width: '100%' }}
                min={0}
                precision={2}
                disabled={disabled}
                value={prenda.valorTasacion}
                onChange={(v) => actualizar(indice, { valorTasacion: v ?? 0 })}
              />
            ),
          },
          {
            title: 'Observaciones',
            width: 200,
            render: (_, { indice, prenda }) => (
              <Input
                disabled={disabled}
                value={prenda.observaciones ?? ''}
                onChange={(e) => actualizar(indice, { observaciones: e.target.value })}
              />
            ),
          },
          {
            title: '',
            width: 48,
            fixed: 'right',
            render: (_, { indice }) => (
              <Button
                type="text"
                danger
                size="small"
                icon={<DeleteOutlined />}
                disabled={disabled}
                aria-label={`Quitar bien ${indice + 1}`}
                onClick={() => eliminar(indice)}
              />
            ),
          },
        ]}
      />
      <Row justify="space-between" align="middle" style={{ marginTop: 12 }}>
        <Col>
          <Button icon={<PlusOutlined />} disabled={disabled} onClick={agregar}>
            Agregar bien
          </Button>
        </Col>
        <Col>
          <Space>
            <Text type="secondary">Tasación total</Text>
            <Text strong>{formatMoney(total)}</Text>
          </Space>
        </Col>
      </Row>
    </>
  )
}
