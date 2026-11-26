import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Drawer,
  Form,
  Grid,
  Input,
  InputNumber,
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
import { CredixCrudPage, CredixDataTable, CredixModal } from '../../components/credix'
import { QrEmvDisplay } from '../../components/caja/QrEmvDisplay'
import { useCrudListStats } from '../../hooks/useCrudListStats'
import '../../styles/medios-pago.css'

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

function MedioFormFields({
  form,
  editing,
  tiposDisponibles,
  previewQr,
  isMobile,
}: {
  form: ReturnType<typeof Form.useForm<FormValues>>[0]
  editing: OficinaMedioPago | null
  tiposDisponibles: { value: number; label: string }[]
  previewQr?: string
  isMobile: boolean
}) {
  return (
    <>
      <div className="medios-pago-form__section">
        <p className="medios-pago-form__section-title">Identidad</p>
        <Form.Item
          name="nombre"
          label="Nombre"
          rules={[{ required: true, message: 'Ingrese el nombre' }]}
        >
          <Input maxLength={80} placeholder="Ej. Yape sucursal" size={isMobile ? 'large' : 'middle'} />
        </Form.Item>
        <Form.Item
          name="marcaCodigo"
          label="Marca visual"
          rules={[{ required: true, message: 'Elija marca' }]}
        >
          <Select options={MARCAS} size={isMobile ? 'large' : 'middle'} />
        </Form.Item>
        <div className="medios-pago-form__flags">
          <Form.Item name="orden" label="Orden" rules={[{ required: true }]}>
            <InputNumber min={1} max={9999} style={{ width: 110 }} size={isMobile ? 'large' : 'middle'} />
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
        </div>
      </div>

      <div className="medios-pago-form__section">
        <p className="medios-pago-form__section-title">Celular (Yape / Plin)</p>
        <Form.Item name="usaCelular" valuePropName="checked">
          <Checkbox>Mostrar número de celular en cobro</Checkbox>
        </Form.Item>
        <Form.Item noStyle shouldUpdate={(a, b) => a.usaCelular !== b.usaCelular}>
          {() =>
            form.getFieldValue('usaCelular') ? (
              <Form.Item
                name="telefono"
                label="Celular"
                extra="9 dígitos. Opcional hasta configurarlo."
                rules={[
                  {
                    validator: async (_, value) => {
                      if (!value?.trim()) return
                      const digits = String(value).replace(/\D/g, '')
                      const local =
                        digits.startsWith('51') && digits.length === 11
                          ? digits.slice(2)
                          : digits
                      if (local.length !== 9) {
                        throw new Error('Use 9 dígitos (ej. 9XXXXXXXX)')
                      }
                    },
                  },
                ]}
              >
                <Input
                  placeholder="9XXXXXXXX"
                  maxLength={15}
                  inputMode="numeric"
                  size={isMobile ? 'large' : 'middle'}
                />
              </Form.Item>
            ) : null
          }
        </Form.Item>
      </div>

      <div className="medios-pago-form__section">
        <p className="medios-pago-form__section-title">Cuenta / CCI (banca)</p>
        <Form.Item name="usaCuenta" valuePropName="checked">
          <Checkbox>Mostrar cuenta o CCI en cobro</Checkbox>
        </Form.Item>
        <Form.Item noStyle shouldUpdate={(a, b) => a.usaCuenta !== b.usaCuenta}>
          {() =>
            form.getFieldValue('usaCuenta') ? (
              <>
                <Form.Item name="numeroCuenta" label="N° cuenta">
                  <Input maxLength={40} size={isMobile ? 'large' : 'middle'} />
                </Form.Item>
                <Form.Item
                  name="cci"
                  label="CCI"
                  rules={[
                    {
                      validator: async (_, value) => {
                        if (!value?.trim()) return
                        const digits = String(value).replace(/\D/g, '')
                        if (digits.length !== 20) {
                          throw new Error('CCI debe tener 20 dígitos')
                        }
                      },
                    },
                  ]}
                >
                  <Input
                    maxLength={30}
                    inputMode="numeric"
                    placeholder="20 dígitos"
                    size={isMobile ? 'large' : 'middle'}
                  />
                </Form.Item>
              </>
            ) : null
          }
        </Form.Item>
      </div>

      <div className="medios-pago-form__section">
        <p className="medios-pago-form__section-title">QR oficial (EMV / BCRP)</p>
        <Form.Item name="usaQr" valuePropName="checked">
          <Checkbox>Mostrar QR en cobro</Checkbox>
        </Form.Item>
        <Form.Item noStyle shouldUpdate={(a, b) => a.usaQr !== b.usaQr}>
          {() =>
            form.getFieldValue('usaQr') ? (
              <>
                <Form.Item
                  name="qrEmvPayload"
                  label="Payload EMV"
                  extra="Desde «Mi QR» en Yape/Plin o banca. No invente el texto."
                >
                  <Input.TextArea
                    rows={isMobile ? 4 : 3}
                    placeholder="00020101…"
                    autoSize={{ minRows: 3, maxRows: 6 }}
                  />
                </Form.Item>
                <Form.Item name="qrNota" label="Nota bajo el QR">
                  <Input maxLength={200} size={isMobile ? 'large' : 'middle'} />
                </Form.Item>
                {previewQr ? (
                  <div className="medios-pago-form__qr-preview">
                    <QrEmvDisplay payload={previewQr} size={isMobile ? 180 : 200} compact />
                  </div>
                ) : null}
              </>
            ) : null
          }
        </Form.Item>
      </div>

      <div className="medios-pago-form__section">
        <p className="medios-pago-form__section-title">Ledger / bóveda</p>
        <Form.Item name="familiaCodigo" label="Familia (rollup)">
          <Select
            allowClear
            size={isMobile ? 'large' : 'middle'}
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
              extra="Crea un ItemId nuevo en ValorTabla (13) ligado a este medio."
            >
              <Checkbox>Crear tipo de pago nuevo en catálogo</Checkbox>
            </Form.Item>
            <Form.Item
              noStyle
              shouldUpdate={(a, b) => a.crearTipoPagoCatalogo !== b.crearTipoPagoCatalogo}
            >
              {() =>
                form.getFieldValue('crearTipoPagoCatalogo') ? null : (
                  <Form.Item
                    name="tipoPagoId"
                    label="Tipo de pago existente"
                    rules={[{ required: true, message: 'Elija un tipo libre o cree uno nuevo' }]}
                  >
                    <Select
                      options={tiposDisponibles}
                      size={isMobile ? 'large' : 'middle'}
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
      </div>
    </>
  )
}

export function MediosPagoPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true
  const [filtro, setFiltro] = useState('')
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

  const filtradas = useMemo(() => {
    const q = filtro.trim().toLowerCase()
    if (!q) return flat
    return flat.filter(
      (m) =>
        m.nombre.toLowerCase().includes(q) ||
        m.marcaCodigo.toLowerCase().includes(q) ||
        String(m.tipoPagoId).includes(q) ||
        (m.telefono ?? '').includes(q),
    )
  }, [flat, filtro])

  const stats = useCrudListStats(filtradas, 'medios')

  const tiposDisponibles = useMemo(() => {
    const usados = new Set(flat.map((m) => m.tipoPagoId))
    return (catalogoQuery.data ?? [])
      .filter((v) => v.itemId > 0 && !usados.has(v.itemId))
      .map((v) => ({ value: v.itemId, label: `${v.itemId} — ${v.denominacion}` }))
  }, [catalogoQuery.data, flat])

  const closeEditor = () => {
    setModalOpen(false)
    setEditing(null)
    setAsVariantOf(null)
  }

  const guardar = useMutation({
    mutationFn: guardarOficinaMedioPago,
    onSuccess: () => {
      message.success('Medio guardado')
      closeEditor()
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
      qrNota: 'Escanee con Yape, Plin o banca móvil',
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
      nombre: values.nombre.trim(),
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

  const editorTitle = editing
    ? `Editar · ${editing.nombre}`
    : asVariantOf
      ? `Variante de ${asVariantOf.nombre}`
      : 'Nuevo medio de pago'

  const columns: ColumnsType<OficinaMedioPago> = [
    {
      title: 'Medio',
      dataIndex: 'nombre',
      render: (v, row) => (
        <div className="medios-pago-row-title">
          <span className="medios-pago-row-title__name">
            {row.parentOficinaMedioPagoId ? <Tag style={{ marginRight: 6 }}>Variante</Tag> : null}
            {v}
          </span>
          <span className="medios-pago-row-title__meta">
            Tipo {row.tipoPagoId} · {row.marcaCodigo}
            {row.telefono ? ` · ${row.telefono}` : ''}
          </span>
        </div>
      ),
    },
    {
      title: 'Cobro',
      width: 168,
      responsive: ['md'],
      render: (_, row) => (
        <Space size={4} wrap>
          {row.usaQr ? <Tag color="blue">QR</Tag> : null}
          {row.usaCelular ? <Tag color="cyan">Celular</Tag> : null}
          {row.usaCuenta ? <Tag>Cuenta</Tag> : null}
          {!row.esDigital ? <Tag color="green">Efectivo</Tag> : null}
          {!row.usaQr && !row.usaCelular && !row.usaCuenta && row.esDigital ? (
            <Tag>Sin datos</Tag>
          ) : null}
        </Space>
      ),
    },
    {
      title: 'Activo',
      dataIndex: 'activo',
      width: 88,
      render: (v: boolean, row) => (
        <Switch
          checked={v}
          checkedChildren="Sí"
          unCheckedChildren="No"
          onChange={(checked) =>
            activar.mutate({ id: row.oficinaMedioPagoId, activo: checked })
          }
        />
      ),
    },
    {
      title: 'Acciones',
      key: 'acciones',
      width: isMobile ? 120 : 200,
      render: (_, row) => (
        <Space wrap size="small">
          <Button
            size={isMobile ? 'middle' : 'small'}
            icon={<EditOutlined />}
            onClick={() => openEdit(row)}
          >
            Editar
          </Button>
          {!row.parentOficinaMedioPagoId ? (
            <Button size={isMobile ? 'middle' : 'small'} onClick={() => openCreate(row)}>
              Variante
            </Button>
          ) : null}
        </Space>
      ),
    },
  ]

  const editorForm = (
    <Form
      form={form}
      layout="vertical"
      onFinish={onFinish}
      className="medios-pago-form"
      requiredMark="optional"
    >
      <MedioFormFields
        form={form}
        editing={editing}
        tiposDisponibles={tiposDisponibles}
        previewQr={previewQr}
        isMobile={isMobile}
      />
    </Form>
  )

  return (
    <CredixCrudPage
      title="Medios de pago"
      subtitle="Configuración por sucursal: celular, cuenta/CCI y QR EMV oficial (BCRP)."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Mantenimiento' },
        { title: 'Medios de pago' },
      ]}
      toolbar={
        <Space wrap style={{ width: '100%' }}>
          <Input.Search
            allowClear
            placeholder="Buscar nombre, marca o celular"
            style={{ width: '100%', maxWidth: isMobile ? '100%' : 280 }}
            value={filtro}
            onChange={(e) => setFiltro(e.target.value)}
            size={isMobile ? 'large' : 'middle'}
          />
          <Checkbox
            checked={incluirInactivos}
            onChange={(e) => setIncluirInactivos(e.target.checked)}
          >
            Incluir inactivos
          </Checkbox>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void listQuery.refetch()}
            loading={listQuery.isFetching}
            size={isMobile ? 'large' : 'middle'}
          >
            Actualizar
          </Button>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => openCreate()}
            size={isMobile ? 'large' : 'middle'}
            block={isMobile}
          >
            Nuevo medio
          </Button>
        </Space>
      }
      extra={
        isMobile ? (
          <Drawer
            title={editorTitle}
            open={modalOpen}
            onClose={closeEditor}
            width="100%"
            destroyOnClose
            extra={
              <Button type="primary" loading={guardar.isPending} onClick={() => form.submit()}>
                Guardar
              </Button>
            }
          >
            {editorForm}
          </Drawer>
        ) : (
          <CredixModal
            title={editorTitle}
            open={modalOpen}
            onCancel={closeEditor}
            onOk={() => form.submit()}
            confirmLoading={guardar.isPending}
            width={720}
            okText="Guardar"
          >
            {editorForm}
          </CredixModal>
        )
      }
    >
      <CredixDataTable
        rowKey="oficinaMedioPagoId"
        loading={listQuery.isLoading}
        columns={columns}
        dataSource={filtradas}
        mobileCards
        pagination={{
          pageSize: isMobile ? 10 : 20,
          showTotal: (total) => `${total} medio(s)`,
        }}
        locale={{ emptyText: 'No hay medios configurados para esta sucursal' }}
      />
    </CredixCrudPage>
  )
}
