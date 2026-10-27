import { useEffect, useState, type ReactNode } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Button, Input, Space, Typography, message } from 'antd'
import { FileSearchOutlined } from '@ant-design/icons'
import { consultarDniApiPeru } from '../../../api/apiperu'
import { crearPersonaRapida, obtenerPersonaPorDocumento } from '../../../api/clientes'
import { ApiError } from '../../../api/errors'
import {
  etiquetaPersonaDocumento,
} from '../../../components/caja/clienteBuscarResolve'
import { FIELD_MAX } from '../../../validation/formRules'

const { Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError
    ? e.message
    : e instanceof Error
      ? e.message
      : 'Error desconocido'
}

function onlyDigits(value: string): string {
  return value.replace(/\D/g, '')
}

function validarFormulario(input: {
  dni: string
  nombre: string
  paterno: string
  materno: string
  celular: string
}): string | null {
  if (input.dni.length !== 8) return 'Ingrese un DNI de 8 dígitos'
  if (!input.nombre.trim()) return 'Ingrese los nombres'
  if (!input.paterno.trim()) return 'Ingrese el apellido paterno'
  if (!input.materno.trim()) return 'Ingrese el apellido materno'
  if (input.celular && !/^9\d{8}$/.test(input.celular)) {
    return 'Celular: 9 dígitos que empiezan con 9'
  }
  return null
}

type Props = {
  onCreated: (personaId: number, label: string) => void
  /** Prefill DNI (p. ej. desde simulador prospecto). */
  initialDni?: string
  submitLabel?: string
  hint?: ReactNode
}

export function CrearPersonaRapidaModal({
  onCreated,
  initialDni,
  submitLabel = 'Guardar persona',
  hint,
}: Props) {
  const [dni, setDni] = useState(() => onlyDigits(initialDni ?? '').slice(0, 8))
  const [nombre, setNombre] = useState('')
  const [paterno, setPaterno] = useState('')
  const [materno, setMaterno] = useState('')
  const [celular, setCelular] = useState('')

  useEffect(() => {
    const next = onlyDigits(initialDni ?? '').slice(0, 8)
    if (next) setDni(next)
  }, [initialDni])

  const crear = useMutation({
    mutationFn: async () => {
      const payload = {
        dni: onlyDigits(dni),
        nombre: nombre.trim(),
        paterno: paterno.trim(),
        materno: materno.trim(),
        celular: onlyDigits(celular),
      }
      if (payload.dni.length !== 8) {
        throw new Error('Ingrese un DNI de 8 dígitos')
      }

      const existente = await obtenerPersonaPorDocumento(payload.dni)
      if (existente?.personaId) {
        return {
          success: true,
          personaId: existente.personaId,
          label: etiquetaPersonaDocumento(existente),
          existed: true,
        }
      }

      const error = validarFormulario(payload)
      if (error) throw new Error(error)
      const created = await crearPersonaRapida({
        dni: payload.dni,
        nombre: payload.nombre,
        apePaterno: payload.paterno,
        apeMaterno: payload.materno,
        celular: payload.celular || null,
      })
      return { ...created, existed: false }
    },
    onSuccess: (r) => {
      if (!r.success) {
        message.error(r.label || 'No se pudo crear')
        return
      }
      onCreated(r.personaId, r.label)
      message.success(r.existed ? 'Ya estaba en el padrón' : 'Persona creada')
    },
    onError: (e) => message.error(errMsg(e)),
  })

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="middle">
      {hint ? <Text type="secondary">{hint}</Text> : null}
      <Input
        placeholder="DNI 8 dígitos"
        value={dni}
        onChange={(e) => setDni(onlyDigits(e.target.value))}
        inputMode="numeric"
        pattern="[0-9]*"
        maxLength={FIELD_MAX.dni}
        status={dni.length > 0 && dni.length !== 8 ? 'error' : undefined}
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
          const ok = Boolean(
            r.success && (r.nombres?.trim() || r.apellidoPaterno?.trim() || r.apellidoMaterno?.trim()),
          )
          if (ok) {
            setNombre(r.nombres ?? '')
            setPaterno(r.apellidoPaterno ?? '')
            setMaterno(r.apellidoMaterno ?? '')
            message.success('Datos validados correctamente')
          } else {
            message.info(r.mensaje ?? 'DNI no encontrado. Complete los datos manualmente.')
          }
        }}
      >
        Buscar DNI (ApiPeru)
      </Button>
      <Input
        placeholder="Nombres *"
        value={nombre}
        maxLength={FIELD_MAX.nombre}
        onChange={(e) => setNombre(e.target.value)}
      />
      <Input
        placeholder="Paterno *"
        value={paterno}
        maxLength={FIELD_MAX.nombre}
        onChange={(e) => setPaterno(e.target.value)}
      />
      <Input
        placeholder="Materno *"
        value={materno}
        maxLength={FIELD_MAX.nombre}
        onChange={(e) => setMaterno(e.target.value)}
      />
      <Input
        placeholder="Celular 9 dígitos"
        value={celular}
        onChange={(e) => setCelular(onlyDigits(e.target.value))}
        inputMode="numeric"
        pattern="[0-9]*"
        maxLength={FIELD_MAX.celular}
        status={celular.length > 0 && !/^9\d{8}$/.test(celular) ? 'error' : undefined}
      />
      <Button type="primary" loading={crear.isPending} onClick={() => crear.mutate()}>
        {submitLabel}
      </Button>
    </Space>
  )
}
