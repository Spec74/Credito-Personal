import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Button, Input, Space, message } from 'antd'
import { FileSearchOutlined } from '@ant-design/icons'
import { consultarDniApiPeru } from '../../../api/apiperu'
import { crearPersonaRapida } from '../../../api/clientes'
import { ApiError } from '../../../api/errors'

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

function onlyDigits(value: string): string {
  return value.replace(/\D/g, '')
}

type Props = {
  onCreated: (personaId: number, label: string) => void
}

export function CrearPersonaRapidaModal({ onCreated }: Props) {
  const [dni, setDni] = useState('')
  const [nombre, setNombre] = useState('')
  const [paterno, setPaterno] = useState('')
  const [materno, setMaterno] = useState('')
  const [celular, setCelular] = useState('')

  const crear = useMutation({
    mutationFn: () =>
      crearPersonaRapida({
        dni: onlyDigits(dni),
        nombre: nombre.trim(),
        apePaterno: paterno.trim(),
        apeMaterno: materno.trim(),
        celular: onlyDigits(celular) || null,
      }),
    onSuccess: (r) => {
      if (!r.success) {
        message.error(r.label || 'No se pudo crear')
        return
      }
      onCreated(r.personaId, r.label)
      message.success('Persona creada')
    },
    onError: (e) => message.error(errMsg(e)),
  })

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="middle">
      <Input
        placeholder="DNI 8 dígitos"
        value={dni}
        onChange={(e) => setDni(onlyDigits(e.target.value))}
        inputMode="numeric"
        pattern="[0-9]*"
        maxLength={8}
      />
      <Button
        icon={<FileSearchOutlined />}
        onClick={async () => {
          const documento = onlyDigits(dni)
          if (documento.length !== 8) {
            message.warning('Ingrese un DNI de 8 dígitos')
            return
          }
          const r = await consultarDniApiPeru(documento)
          if (r.success) {
            setNombre(r.nombres ?? '')
            setPaterno(r.apellidoPaterno ?? '')
            setMaterno(r.apellidoMaterno ?? '')
          } else {
            message.warning(r.mensaje ?? 'DNI no encontrado')
          }
        }}
      >
        Buscar DNI (ApiPeru)
      </Button>
      <Input placeholder="Nombres" value={nombre} onChange={(e) => setNombre(e.target.value)} />
      <Input placeholder="Paterno" value={paterno} onChange={(e) => setPaterno(e.target.value)} />
      <Input placeholder="Materno" value={materno} onChange={(e) => setMaterno(e.target.value)} />
      <Input
        placeholder="Celular 9 dígitos"
        value={celular}
        onChange={(e) => setCelular(onlyDigits(e.target.value))}
        inputMode="numeric"
        pattern="[0-9]*"
        maxLength={9}
      />
      <Button type="primary" loading={crear.isPending} onClick={() => crear.mutate()}>
        Guardar persona
      </Button>
    </Space>
  )
}
