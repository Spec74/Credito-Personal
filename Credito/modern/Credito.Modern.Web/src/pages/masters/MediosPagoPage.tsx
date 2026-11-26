import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Tag,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchOficinaMediosPago,
  guardarOficinaMedioPago,
  setOficinaMedioPagoActivo,
  type GuardarOficinaMedioPagoBody,
  type OficinaMedioPago,
} from '../../api/mediosPago'
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { QrEmvDisplay } from '../../components/caja/QrEmvDisplay'

const MARCAS = [
  { value: 'efectivo', label: 'Efectivo' },
  { value: 'yape', label: 'Yape' },
  { value: 'plin', label: 'Plin' },
  { value: 'interbank', label: 'Interbank' },
  { value: 'bcp', label: 'BCP' },
  { value: 'banco-nacion', label: 'Banco de la Nación' },
  { value: 'bbva', label: 'BBVA' },
  { value: 'scotiabank', label: 'Scotiabank' },
  { value: 'transferencia', label: 'Transferencia' },
  { value: 'otro', label: 'Otro' },
]

type FormValues = {
  nombre: string
  marcaCodigo: string
  orden: number
  activo: boolean
  esDigital: boolean
  usaCelular: boolean
  telefono?: string
  usaCuenta: boolean
  numeroCuenta?: string
  cci?: string
  usaQr: boolean
  qrEmvPayload?: string
  qrNota?: string
  familiaCodigo?: string
  soloCentral: boolean
  crearTipoPagoCatalogo: boolean
  parentOficinaMedioPagoId?: number | null
  tipoPagoId?: number | null
}

function flatten(rows: OficinaMedioPago[]): OficinaMedioPago[] {
  const out: OficinaMedioPago[] = []
  for (const r of rows) {
    out.push(r)
    for (const v of r.variantes ?? []) out.push(v)
  }
  return out
}

export function MediosPagoPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<OficinaMedioPago | null>(null)
  const [asVariantOf, setAsVariantOf] = useState<OficinaMedioPago | null>(null)
  const [form] = Form.useForm<FormValues>()
  const queryClient = useQueryClient()

  const listQuery = useQuery({
    queryKey: ['oficina-medios-pago', oficinaId, incluirInactivos],
    queryFn: () =>
      fetchOficinaMediosPago(oficinaId, {
        incluirInactivos,
        soloRaices: true,
      }),
    enabled: oficinaId > 0,
  })

  const catalogoQuery = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    staleTime: 60_000,
  })

  const flat = useMemo(() => flatten(listQuery.data ?? []), [listQuery.data])

  const tiposDisponibles = useMemo(() => {
    const usados = new Set(flat.map((m) => m.tipoPagoId))
    return (catalogoQuery.data ?? [])
      .filter((v) => v.itemId > 0 && !usados.has(v.itemId))
      .map((v) => ({ value: v.itemId, label: `${v.itemId} — ${v.denominacion}` }))
  }, [catalogoQuery.data, flat])

  const guardar = useMutation({
    mutationFn: guardarOficinaMedioPago,
    onSuccess: () => {
      message.success('Medio guardado')
      setModalOpen(false)
      setEditing(null)
      setAsVariantOf(null)
      void queryClient.invalidateQueries({ queryKey: ['oficina-medios-pago'] })
      void queryClient.invalidateQueries({ queryKey: ['valores-tabla', 13] })
    },
    onError: (e: unknown) => {
      message.error(e instanceof ApiError ? e.message : 'Error al guardar')
    },
  })

  const activar = useMutation({
    mutationFn: ({ id, activo }: { id: number; activo: boolean }) =>
      setOficinaMedioPagoActivo(id, activo, oficinaId),
    onSuccess: () => {
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['oficina-medios-pago'] })
    },
    onError: (e: unknown) => {
      message.error(e instanceof ApiError ? e.message : 'Error al cambiar estado')
    },
  })

  const openCreate = (parent?: OficinaMedioPago) => {
    setEditing(null)
    setAsVariantOf(parent ?? null)
    form.setFieldsValue({
      nombre: parent ? `${parent.nombre} — variante` : '',
      marcaCodigo: parent?.marcaCodigo ?? 'yape',
      orden: (parent?.orden ?? 100) + 1,
      activo: true,
      esDigital: parent?.esDigital ?? true,
      usaCelular: parent?.usaCelular ?? true,
      usaCuenta: parent?.usaCuenta ?? false,
      usaQr: parent?.usaQr ?? true,
      familiaCodigo: parent?.familiaCodigo ?? undefined,
      soloCentral: parent?.soloCentral ?? false,
      crearTipoPagoCatalogo: true,
      parentOficinaMedioPagoId: parent?.oficinaMedioPagoId ?? null,
      tipoPagoId: null,
      telefono: undefined,
      numeroCuenta: undefined,
      cci: undefined,
      qrEmvPayload: undefined,
      qrNota: 'Payload EMV del QR oficial (Mi QR Yape/Plin)',
    })
    setModalOpen(true)
  }

  const openEdit = (row: OficinaMedioPago) => {
    setEditing(row)
    setAsVariantOf(null)
    form.setFieldsValue({
      nombre: row.nombre,
      marcaCodigo: row.marcaCodigo,
      orden: row.orden,
      activo: row.activo,
      esDigital: row.esDigital,
      usaCelular: row.usaCelular,
      telefono: row.telefono ?? undefined,
      usaCuenta: row.usaCuenta,
      numeroCuenta: row.numeroCuenta ?? undefined,
      cci: row.cci ?? undefined,
      usaQr: row.usaQr,
      qrEmvPayload: row.qrEmvPayload ?? undefined,
      qrNota: row.qrNota ?? undefined,
      familiaCodigo: row.familiaCodigo ?? undefined,
      soloCentral: row.soloCentral,
      crearTipoPagoCatalogo: false,
      parentOficinaMedioPagoId: row.parentOficinaMedioPagoId,
      tipoPagoId: row.tipoPagoId,
    })
    setModalOpen(true)
  }

  const columns: ColumnsType<OficinaMedioPago> = [
    {
      title: 'Nombre',
      dataIndex: 'nombre',
      render: (v, row) => (
        <Space>
          {row.parentOficinaMedioPagoId ? <Tag>Variante</Tag> : null}
          <span>{v}</span>
        </Space>
      ),
    },
    { title: 'TipoPagoId', dataIndex: 'tipoPagoId', width: 100 },
    { title: 'Marca', dataIndex: 'marcaCodigo', width: 120 },
    {
      title: 'Cobro',
      width: 160,
      render: (_, row) => (
        <Space size={4} wrap>
          {row.usaQr ? <Tag color="blue">QR</Tag> : null}
          {row.usaCelular ? <Tag color="cyan">Celular</Tag> : null}
          {row.usaCuenta ? <Tag>Cuenta</Tag> : null}
          {!row.esDigital ? <Tag color="green">Efectivo</Tag> : null}
        </Space>
      ),
    },
    {
      title: 'Activo',
      dataIndex: 'activo',
      width: 90,
      render: (v: boolean, row) => (
        <Switch
          checked={v}
          onChange={(checked) =>
            activar.mutate({ id: row.oficinaMedioPagoId, activo: checked })
          }
        />
      ),
    },
    {
      title: '',
      width: 200,
      render: (_, row) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(row)}>
            Editar
          </Button>
          {!row.parentOficinaMedioPagoId ? (
            <Button size="small" onClick={() => openCreate(row)}>
              Variante
            </Button>
          ) : null}
        </Space>
      ),
    },
  ]

  const onFinish = (values: FormValues) => {
    if (oficinaId < 1) {
      message.error('Sin oficina en sesión')
      return
    }
    const body: GuardarOficinaMedioPagoBody = {
      oficinaMedioPagoId: editing?.oficinaMedioPagoId ?? null,
      oficinaId,
      parentOficinaMedioPagoId:
        values.parentOficinaMedioPagoId ?? asVariantOf?.oficinaMedioPagoId ?? null,
      tipoPagoId: values.tipoPagoId ?? editing?.tipoPagoId ?? null,
      nombre: values.nombre,
      marcaCodigo: values.marcaCodigo,
      orden: values.orden,
      activo: values.activo,
      esDigital: values.esDigital,
      usaCelular: values.usaCelular,
      telefono: values.telefono ?? null,
      usaCuenta: values.usaCuenta,
      numeroCuenta: values.numeroCuenta ?? null,
      cci: values.cci ?? null,
      usaQr: values.usaQr,
      qrEmvPayload: values.qrEmvPayload ?? null,
      qrNota: values.qrNota ?? null,
      familiaCodigo: values.familiaCodigo ?? null,
      soloCentral: values.soloCentral,
      crearTipoPagoCatalogo: values.crearTipoPagoCatalogo === true,
    }
    guardar.mutate(body)
  }

  const previewQr = Form.useWatch('qrEmvPayload', form)

  return (
    <CredixCrudPage
      title="Medios de pago"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/maestros">Maestros</Link> },
        { title: 'Medios de pago' },
      ]}
      subtitle="Por sucursal: teléfono, cuenta/CCI y QR EMV oficial (BCRP). Las variantes crean un TipoPagoId nuevo en el catálogo."
      toolbar={
        <Space wrap>
          <Checkbox
            checked={incluirInactivos}
            onChange={(e) => setIncluirInactivos(e.target.checked)}
          >
            Incluir inactivos
          </Checkbox>
          <Button icon={<ReloadOutlined />} onClick={() => void listQuery.refetch()}>
            Actualizar
          </Button>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openCreate()}>
            Nuevo medio
          </Button>
        </Space>
      }
    >
      <CredixDataTable
        rowKey="oficinaMedioPagoId"
        loading={listQuery.isLoading}
        columns={columns}
        dataSource={flat}
        pagination={{ pageSize: 20 }}
      />

      <Modal
        title={
          editing
            ? 'Editar medio'
            : asVariantOf
              ? `Variante de ${asVariantOf.nombre}`
              : 'Nuevo medio'
        }
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={guardar.isPending}
        width={720}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={onFinish}>
          <Form.Item name="nombre" label="Nombre" rules={[{ required: true }]}>
            <Input maxLength={80} />
          </Form.Item>
          <Form.Item name="marcaCodigo" label="Marca visual" rules={[{ required: true }]}>
            <Select options={MARCAS} />
          </Form.Item>
          <Space wrap style={{ width: '100%' }}>
            <Form.Item name="orden" label="Orden" rules={[{ required: true }]}>
              <InputNumber min={1} max={9999} />
            </Form.Item>
            <Form.Item name="activo" label="Activo" valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item name="esDigital" label="Digital" valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item name="soloCentral" label="Solo Central" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Space>

          <Form.Item name="usaCelular" valuePropName="checked">
            <Checkbox>Usa número de celular (Yape/Plin)</Checkbox>
          </Form.Item>
          <Form.Item noStyle shouldUpdate={(a, b) => a.usaCelular !== b.usaCelular}>
            {() =>
              form.getFieldValue('usaCelular') ? (
                <Form.Item
                  name="telefono"
                  label="Celular (9 dígitos)"
                  extra="Opcional hasta configurarlo; en cobro solo se muestra si está cargado."
                >
                  <Input placeholder="9XXXXXXXX" maxLength={15} />
                </Form.Item>
              ) : null
            }
          </Form.Item>

          <Form.Item name="usaCuenta" valuePropName="checked">
            <Checkbox>Usa número de cuenta / CCI (transferencia banca)</Checkbox>
          </Form.Item>
          <Form.Item noStyle shouldUpdate={(a, b) => a.usaCuenta !== b.usaCuenta}>
            {() =>
              form.getFieldValue('usaCuenta') ? (
                <>
                  <Form.Item name="numeroCuenta" label="N° cuenta">
                    <Input maxLength={40} />
                  </Form.Item>
                  <Form.Item name="cci" label="CCI">
                    <Input maxLength={30} />
                  </Form.Item>
                </>
              ) : null
            }
          </Form.Item>

          <Form.Item name="usaQr" valuePropName="checked">
            <Checkbox>Mostrar QR en cobro (payload EMV oficial BCRP)</Checkbox>
          </Form.Item>
          <Form.Item noStyle shouldUpdate={(a, b) => a.usaQr !== b.usaQr}>
            {() =>
              form.getFieldValue('usaQr') ? (
                <>
                  <Form.Item
                    name="qrEmvPayload"
                    label="Payload EMV del QR"
                    extra="Obténgalo de «Mi QR» en Yape/Plin o del QR del banco. No invente el texto: debe ser el EMVCo MPM oficial para que BCP/Interbank/BN puedan escanearlo."
                  >
                    <Input.TextArea rows={3} placeholder="00020101…" />
                  </Form.Item>
                  <Form.Item name="qrNota" label="Nota bajo el QR">
                    <Input maxLength={200} />
                  </Form.Item>
                  {previewQr ? <QrEmvDisplay payload={previewQr} size={180} /> : null}
                </>
              ) : null
            }
          </Form.Item>

          <Form.Item name="familiaCodigo" label="Familia (rollup bóveda)">
            <Select
              allowClear
              options={[
                { value: 'YAPE', label: 'YAPE' },
                { value: 'INTERBANK', label: 'INTERBANK' },
              ]}
            />
          </Form.Item>

          {!editing ? (
            <>
              <Form.Item
                name="crearTipoPagoCatalogo"
                valuePropName="checked"
                extra="Recomendado: crea un ItemId nuevo en ValorTabla (13) ligado a este medio."
              >
                <Checkbox>Crear ItemId nuevo en catálogo (Tabla 13)</Checkbox>
              </Form.Item>
              <Form.Item noStyle shouldUpdate={(a, b) => a.crearTipoPagoCatalogo !== b.crearTipoPagoCatalogo}>
                {() =>
                  form.getFieldValue('crearTipoPagoCatalogo') ? null : (
                    <Form.Item
                      name="tipoPagoId"
                      label="Tipo de pago (catálogo)"
                      rules={[{ required: true, message: 'Elija un tipo libre o cree uno nuevo' }]}
                    >
                      <Select
                        options={tiposDisponibles}
                        placeholder={
                          tiposDisponibles.length
                            ? 'Tipos aún no usados en esta sucursal'
                            : 'No hay tipos libres — active crear catálogo'
                        }
                        showSearch
                        optionFilterProp="label"
                      />
                    </Form.Item>
                  )
                }
              </Form.Item>
            </>
          ) : (
            <Form.Item name="tipoPagoId" hidden>
              <InputNumber />
            </Form.Item>
          )}

          <Form.Item name="parentOficinaMedioPagoId" hidden>
            <InputNumber />
          </Form.Item>
        </Form>
      </Modal>
    </CredixCrudPage>
  )
}
