import { useState } from 'react'
import { UserAddOutlined } from '@ant-design/icons'
import { Button, Modal, Space, Typography } from 'antd'
import { ClienteBuscarAutoComplete } from '../caja/ClienteBuscarAutoComplete'
import { dniFromSearchTerm } from '../caja/clienteBuscarResolve'
import { CrearPersonaRapidaModal } from '../../pages/clientes/components/CrearPersonaRapidaModal'

const { Text } = Typography

type Props = {
  personaId: number | null
  clienteLabel: string
  termino: string
  onTerminoChange: (value: string) => void
  onSelect: (personaId: number, label: string) => void
  onClear: () => void
}

/** Cliente del ticket: padrón o alta rápida de comprador. */
export function CompradorVentaField({
  personaId,
  clienteLabel,
  termino,
  onTerminoChange,
  onSelect,
  onClear,
}: Props) {
  const [altaOpen, setAltaOpen] = useState(false)

  if (personaId) {
    return (
      <Space direction="vertical" size={4} style={{ width: '100%' }}>
        <Text>{clienteLabel}</Text>
        <Button type="link" style={{ paddingInline: 0 }} onClick={onClear}>
          Cambiar cliente
        </Button>
      </Space>
    )
  }

  return (
    <>
      <ClienteBuscarAutoComplete
        value={termino}
        onChange={onTerminoChange}
        onSelectPersona={(id, label) => {
          onSelect(id, label)
          onTerminoChange('')
        }}
        placeholder="Documento o nombre"
        fullWidth
      />
      <Button
        type="link"
        icon={<UserAddOutlined />}
        style={{ paddingInline: 0, marginTop: 8 }}
        onClick={() => setAltaOpen(true)}
      >
        Registrar comprador
      </Button>
      <Modal
        title="Registrar comprador"
        open={altaOpen}
        onCancel={() => setAltaOpen(false)}
        footer={null}
        destroyOnHidden
      >
        <CrearPersonaRapidaModal
          initialDni={dniFromSearchTerm(termino)}
          submitLabel="Usar en esta venta"
          onCreated={(id, label) => {
            onSelect(id, label)
            onTerminoChange('')
            setAltaOpen(false)
          }}
        />
      </Modal>
    </>
  )
}
