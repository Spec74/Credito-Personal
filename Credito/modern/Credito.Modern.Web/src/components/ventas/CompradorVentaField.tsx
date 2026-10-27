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

/**
 * Cliente de ticket: busca en el padrón o registra comprador de mostrador
 * (DNI + nombre). No origina crédito ni aprueba nada.
 */
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
      <Text type="secondary" style={{ display: 'block' }}>
        Si no está en el padrón, regístrelo con DNI. No se aprueba crédito.
      </Text>
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
          hint="El comprador de mostrador (prenda rematada u otro contado) no tiene que ser el titular del crédito."
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
