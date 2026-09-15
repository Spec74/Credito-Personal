import { useMemo, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Button,
  Form,
  Input,
  InputNumber,
  Select,
  Typography,
  message,
} from 'antd'
import {
  confirmarClaveCajaDiario,
  entradaSalidaCajaDiario,
  fetchTipoOperaciones,
} from '../../../api/cajaDiario'
import { fetchValoresTabla } from '../../../api/maestros'
import { ClienteBuscarAutoComplete } from '../../../components/caja/ClienteBuscarAutoComplete'
import { CajaModal } from '../../../components/caja/CajaModal'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import type { TipoOperacionListItem } from '../../../types/api'
import type { CajaSession } from './types'
import { errMsg } from './types'

const { Paragraph } = Typography

export function EntradaSalidaTab({
  ctx,
  onChanged,
}: {
  ctx: CajaSession
  onChanged: () => void
}) {
  const [form] = Form.useForm()
  const [clienteLabel, setClienteLabel] = useState('')
  const [claveModalOpen, setClaveModalOpen] = useState(false)
  const [claveAdmin, setClaveAdmin] = useState('')
  const [pendingValues, setPendingValues] = useState<{
    personaId: number
    tipoOperacionId: number
    importe: number
    descripcion?: string
    tipoPagoId: number
  } | null>(null)
  const tiposQuery = useQuery({
    queryKey: ['tipo-operaciones'],
    queryFn: fetchTipoOperaciones,
  })
  const tiposPagoQuery = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    staleTime: 5 * 60_000,
  })

  const tipoPagoOptions = useMemo(
    () =>
      (tiposPagoQuery.data ?? []).map((t) => ({
        value: t.itemId,
        label: t.denominacion,
      })),
    [tiposPagoQuery.data],
  )

  const tiposCaja = (tiposQuery.data ?? []).filter((t) => t.indCajaDiario)

  const guardar = useMutation({
    mutationFn: (values: {
      personaId: number
      tipoOperacionId: number
      importe: number
      descripcion?: string
      tipoPagoId: number
    }) =>
      entradaSalidaCajaDiario({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        personaId: values.personaId,
        tipoOperacionId: values.tipoOperacionId,
        importe: values.importe,
        descripcion: values.descripcion ?? null,
        tipoPagoId: values.tipoPagoId,
      }),
    onSuccess: (r) => {
      message.success(`Registrado (código ${r.resultCode})`)
      form.resetFields()
      setClienteLabel('')
      onChanged()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const tipoSel = Form.useWatch('tipoOperacionId', form)
  const tipoOp = tiposCaja.find((t) => t.tipoOperacionId === tipoSel)
  const esEgreso = tipoOp != null && !tipoOp.indEntrada

  return (
    <div className="caja-diario-entrada-salida">
      <div className="caja-diario-entrada-salida-form">
        <Form
          form={form}
          layout="vertical"
          initialValues={{ tipoPagoId: 1 }}
          onFinish={(v) => {
            const ejecutar = () => {
              if (esEgreso) {
                setPendingValues(v)
                setClaveModalOpen(true)
                return
              }
              guardar.mutate(v)
            }
            cajaConfirm({
              title: 'Confirmar operación',
              content: '¿Confirmar para realizar esta operación en caja?',
              onOk: ejecutar,
            })
          }}
        >
          <Form.Item label="Persona" required>
            <ClienteBuscarAutoComplete
              value={clienteLabel}
              onChange={setClienteLabel}
              onSelectPersona={(pid, label) => {
                setClienteLabel(label)
                form.setFieldValue('personaId', pid)
              }}
              placeholder="Buscar por DNI, nombre o código"
            />
          </Form.Item>
          <Form.Item
            name="personaId"
            hidden
            rules={[{ required: true, type: 'number', min: 1 }]}
          >
            <InputNumber />
          </Form.Item>
          <Form.Item
            name="tipoOperacionId"
            label="Tipo operación"
            rules={[{ required: true }]}
          >
            <Select
              loading={tiposQuery.isLoading}
              options={tiposCaja.map((t: TipoOperacionListItem) => ({
                value: t.tipoOperacionId,
                label: `${t.codigo} — ${t.denominacion} (${t.indEntrada ? 'Ingreso' : 'Egreso'})`,
              }))}
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
          <Form.Item
            name="importe"
            label="Importe"
            rules={[{ required: true, type: 'number', min: 0.01 }]}
          >
            <InputNumber style={{ width: '100%' }} min={0.01} step={0.01} />
          </Form.Item>
          <Form.Item
            name="tipoPagoId"
            label="Tipo pago"
            rules={[{ required: true }]}
          >
            <Select
              loading={tiposPagoQuery.isLoading}
              options={tipoPagoOptions}
              showSearch
              optionFilterProp="label"
              placeholder="Tipo de pago"
            />
          </Form.Item>
          <Form.Item name="descripcion" label="Descripción">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Button type="primary" htmlType="submit" loading={guardar.isPending}>
            Realizar operación
          </Button>
        </Form>
      </div>
      <aside>
        {esEgreso ? (
          <Paragraph type="warning">
            Los egresos requieren clave de administrador (paridad MVC).
          </Paragraph>
        ) : (
          <Paragraph type="secondary">
            Registre ingresos o egresos de caja con la misma lógica que el
            formulario legacy. Busque la persona por nombre o documento en lugar
            de escribir el ID manualmente.
          </Paragraph>
        )}
      </aside>

      <CajaModal
        title="Autorización de egreso"
        open={claveModalOpen}
        onCancel={() => {
          setClaveModalOpen(false)
          setClaveAdmin('')
          setPendingValues(null)
        }}
        onOk={async () => {
          try {
            const r = await confirmarClaveCajaDiario(claveAdmin)
            if (!r.autorizado) {
              message.error(r.mensaje ?? 'NO AUTORIZADO!!!!')
              return
            }
            if (pendingValues) {
              guardar.mutate(pendingValues)
            }
            setClaveModalOpen(false)
            setClaveAdmin('')
            setPendingValues(null)
          } catch (e) {
            message.error(errMsg(e))
          }
        }}
        okText="Confirmar"
        confirmLoading={guardar.isPending}
      >
        <Paragraph type="secondary">
          Los egresos requieren clave de administrador (paridad MVC).
        </Paragraph>
        <Input.Password
          placeholder="Clave ADMVENDIX"
          value={claveAdmin}
          onChange={(e) => setClaveAdmin(e.target.value)}
          autoFocus
        />
      </CajaModal>
    </div>
  )
}
