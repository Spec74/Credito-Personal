import { useEffect, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Form, Input, Modal, Select, Switch, message } from 'antd'
import { fetchCajaGestores } from '../../api/cajaMaestro'
import { guardarOficina, type OficinaGestionRow } from '../../api/maestrosCrud'
import { ApiError } from '../../api/errors'
import { GoogleMapLocationPicker } from '../../components/maps/GoogleMapLocationPicker'
import { type MapLatLng, toMapLatLng } from '../../config/googleMaps'
import { useQuery } from '@tanstack/react-query'

type FormValues = {
  denominacion: string
  descripcion?: string
  telefono?: string
  usuarioAsignadoId?: number
  indPrincipal: boolean
  estado: boolean
}

export function OficinaFormModal({
  open,
  editing,
  onClose,
  onSaved,
}: {
  open: boolean
  editing: OficinaGestionRow | null
  onClose: () => void
  onSaved: () => void
}) {
  const [form] = Form.useForm<FormValues>()
  const [mapLocation, setMapLocation] = useState<MapLatLng | null>(null)

  const gestoresQuery = useQuery({
    queryKey: ['caja-gestores'],
    queryFn: fetchCajaGestores,
    enabled: open,
  })

  useEffect(() => {
    if (!open) return
    if (editing) {
      form.setFieldsValue({
        denominacion: editing.denominacion ?? '',
        descripcion: editing.descripcion ?? '',
        telefono: editing.telefono ?? '',
        usuarioAsignadoId: editing.usuarioAsignadoId > 0 ? editing.usuarioAsignadoId : undefined,
        indPrincipal: editing.indPrincipal,
        estado: editing.estado,
      })
      setMapLocation(toMapLatLng(editing.latitud, editing.longitud))
    } else {
      form.setFieldsValue({
        denominacion: '',
        descripcion: '',
        telefono: '',
        usuarioAsignadoId: undefined,
        indPrincipal: false,
        estado: true,
      })
      setMapLocation(null)
    }
  }, [open, editing, form])

  const guardar = useMutation({
    mutationFn: guardarOficina,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Oficina guardada')
      onSaved()
      onClose()
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  return (
    <Modal
      title={editing ? `Editar oficina #${editing.oficinaId}` : 'Nueva oficina'}
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={guardar.isPending}
      width={760}
      destroyOnHidden
      styles={{ body: { maxHeight: 'calc(100vh - 200px)', overflowY: 'auto' } }}
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={(v) =>
          guardar.mutate({
            oficinaId: editing?.oficinaId ?? 0,
            denominacion: v.denominacion.trim(),
            descripcion: v.descripcion?.trim() || null,
            telefono: v.telefono?.trim() || null,
            usuarioAsignadoId: v.usuarioAsignadoId ?? 0,
            indPrincipal: v.indPrincipal,
            estado: v.estado,
            latitud: mapLocation?.lat ?? null,
            longitud: mapLocation?.lng ?? null,
          })
        }
      >
        <Form.Item name="denominacion" label="Denominación" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="descripcion" label="Descripción">
          <Input.TextArea rows={2} />
        </Form.Item>
        <Form.Item name="telefono" label="Teléfono">
          <Input />
        </Form.Item>
        <Form.Item name="usuarioAsignadoId" label="Responsable">
          <Select
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="Seleccione responsable"
            loading={gestoresQuery.isLoading}
            options={(gestoresQuery.data ?? []).map((g) => ({
              value: g.usuarioId,
              label: g.nombreCompleto,
            }))}
          />
        </Form.Item>
        <Form.Item name="indPrincipal" label="Principal" valuePropName="checked">
          <Switch />
        </Form.Item>
        <Form.Item name="estado" label="Activo" valuePropName="checked">
          <Switch />
        </Form.Item>
        <Form.Item label="Ubicación (Google Maps)">
          <GoogleMapLocationPicker
            value={mapLocation}
            layoutKey={editing?.oficinaId ?? 'nueva'}
            onChange={setMapLocation}
          />
        </Form.Item>
      </Form>
    </Modal>
  )
}
