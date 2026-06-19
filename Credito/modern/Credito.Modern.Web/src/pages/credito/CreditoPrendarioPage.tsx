import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Row,
  Space,
  Steps,
  Typography,
  message,
} from 'antd'
import { FileAddOutlined, SearchOutlined, UserAddOutlined } from '@ant-design/icons'
import { buscarClientes, crearPersonaRapida } from '../../api/clientes'
import { crearSolicitudCredito } from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { ClienteBuscarItem } from '../../types/api'
import { CredixDataTable, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { buildCreditoPrendarioObservacion } from '../../utils/creditoPrendario'
import { formatMoney } from '../../utils/formatMoney'

const { Paragraph, Text } = Typography

type ClienteRapidoForm = {
  dni: string
  nombre: string
  apePaterno: string
  apeMaterno: string
  celular?: string
}

type PrendaForm = {
  descripcion: string
  montoTasacion: number
  fechaRemate: string
  observacion?: string
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

function defaultFechaRemate(): string {
  const d = new Date()
  d.setDate(d.getDate() + 30)
  return d.toISOString().slice(0, 10)
}

export function CreditoPrendarioPage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [clienteForm] = Form.useForm<ClienteRapidoForm>()
  const [prendaForm] = Form.useForm<PrendaForm>()
  const [terminoCliente, setTerminoCliente] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')

  const buscar = useMutation({
    mutationFn: (term: string) => buscarClientes(term),
  })

  const crearCliente = useMutation({
    mutationFn: (v: ClienteRapidoForm) =>
      crearPersonaRapida({
        dni: v.dni.trim(),
        nombre: v.nombre.trim().toUpperCase(),
        apePaterno: v.apePaterno.trim().toUpperCase(),
        apeMaterno: v.apeMaterno.trim().toUpperCase(),
        celular: v.celular?.trim() || null,
      }),
    onSuccess: (r) => {
      setPersonaId(r.personaId)
      setClienteLabel(r.label)
      clienteForm.resetFields()
      message.success('Cliente creado para crédito prendario')
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const crearSolicitudPrendaria = useMutation({
    mutationFn: async () => {
      const prenda = await prendaForm.validateFields()
      if (!personaId) throw new Error('Seleccione o cree un cliente')
      const solicitud = await crearSolicitudCredito({ oficinaId, personaId })
      return { solicitudCreditoId: solicitud.solicitudCreditoId, prenda }
    },
    onSuccess: ({ solicitudCreditoId, prenda }) => {
      const obs = buildCreditoPrendarioObservacion(prenda)
      const q = new URLSearchParams({
        personaId: String(personaId),
        solicitudCreditoId: String(solicitudCreditoId),
        productoId: '2',
        observacion: obs,
        prendaDescripcion: prenda.descripcion,
        prendaMontoTasacion: String(prenda.montoTasacion),
        prendaFechaRemate: prenda.fechaRemate,
      })
      if (prenda.observacion?.trim()) {
        q.set('prendaObservacion', prenda.observacion.trim())
      }
      message.success(`Solicitud prendaria #${solicitudCreditoId} creada`)
      navigate(`/credito/simulador?${q}`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const montoTasacion = Form.useWatch('montoTasacion', prendaForm) as number | undefined
  const stats: CredixStatItem[] = []
  if (personaId) {
    stats.push({ label: 'Cliente', value: clienteLabel || `Persona #${personaId}` })
  }
  if (montoTasacion && montoTasacion > 0) {
    stats.push({ label: 'Tasación', value: formatMoney(montoTasacion), tone: 'green' })
  }

  return (
    <CredixPage
      title="Registro de crédito prendario"
      subtitle="Acceso directo para registrar cliente, prenda y continuar al simulador de crédito."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Crédito prendario' },
      ]}
      stats={stats}
    >
      <Steps
        size="small"
        current={personaId ? 1 : 0}
        style={{ marginBottom: 24, maxWidth: 640 }}
        items={[{ title: 'Cliente' }, { title: 'Prenda' }, { title: 'Simulador' }]}
      />

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <CredixPanel title="Buscar cliente existente">
            <Space.Compact style={{ width: '100%', marginBottom: 12 }}>
              <Input
                placeholder="DNI, código o nombre"
                value={terminoCliente}
                onChange={(e) => setTerminoCliente(e.target.value)}
                onPressEnter={() => {
                  if (terminoCliente.trim().length >= 2) buscar.mutate(terminoCliente.trim())
                }}
              />
              <Button
                icon={<SearchOutlined />}
                loading={buscar.isPending}
                onClick={() => {
                  if (terminoCliente.trim().length < 2) {
                    message.warning('Ingrese al menos 2 caracteres')
                    return
                  }
                  buscar.mutate(terminoCliente.trim())
                }}
              >
                Buscar
              </Button>
            </Space.Compact>
            <CredixDataTable<ClienteBuscarItem>
              size="small"
              rowKey="personaId"
              dataSource={buscar.data ?? []}
              loading={buscar.isPending}
              pagination={false}
              locale={{ emptyText: 'Busque y elija un cliente' }}
              columns={[
                { title: 'Cliente', dataIndex: 'label', ellipsis: true },
                {
                  title: '',
                  width: 90,
                  render: (_, row) => (
                    <Button
                      size="small"
                      type="link"
                      onClick={() => {
                        setPersonaId(row.personaId)
                        setClienteLabel(row.label)
                        buscar.reset()
                      }}
                    >
                      Elegir
                    </Button>
                  ),
                },
              ]}
            />
          </CredixPanel>
        </Col>

        <Col xs={24} lg={12}>
          <CredixPanel title="Nuevo cliente rápido">
            <Form form={clienteForm} layout="vertical" onFinish={(v) => crearCliente.mutate(v)}>
              <Row gutter={12}>
                <Col span={12}>
                  <Form.Item
                    name="dni"
                    label="DNI"
                    rules={[
                      { required: true, message: 'Ingrese DNI' },
                      { len: 8, message: 'DNI debe tener 8 dígitos' },
                    ]}
                  >
                    <Input maxLength={8} />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item name="celular" label="Celular">
                    <Input maxLength={15} />
                  </Form.Item>
                </Col>
              </Row>
              <Form.Item name="nombre" label="Nombres" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Row gutter={12}>
                <Col span={12}>
                  <Form.Item name="apePaterno" label="Apellido paterno" rules={[{ required: true }]}>
                    <Input />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item name="apeMaterno" label="Apellido materno" rules={[{ required: true }]}>
                    <Input />
                  </Form.Item>
                </Col>
              </Row>
              <Button type="primary" htmlType="submit" icon={<UserAddOutlined />} loading={crearCliente.isPending}>
                Crear cliente
              </Button>
            </Form>
          </CredixPanel>
        </Col>
      </Row>

      <CredixPanel title="Datos de la prenda">
        {personaId ? (
          <Alert
            type="success"
            showIcon
            style={{ marginBottom: 12 }}
            message={`Cliente seleccionado: ${clienteLabel || `Persona #${personaId}`}`}
            action={
              <Button
                size="small"
                onClick={() => {
                  setPersonaId(null)
                  setClienteLabel('')
                }}
              >
                Cambiar
              </Button>
            }
          />
        ) : (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message="Seleccione o cree el cliente antes de registrar la prenda."
          />
        )}
        <Form
          form={prendaForm}
          layout="vertical"
          initialValues={{ montoTasacion: 0, fechaRemate: defaultFechaRemate() }}
        >
          <Form.Item
            name="descripcion"
            label="Descripción prenda"
            rules={[{ required: true, message: 'Describa la prenda' }]}
          >
            <Input placeholder="Ej. joyas de oro, herramienta, celular, electrodoméstico" />
          </Form.Item>
          <Row gutter={16}>
            <Col xs={24} md={8}>
              <Form.Item
                name="montoTasacion"
                label="Monto tasación"
                rules={[{ required: true, type: 'number', min: 0.01 }]}
              >
                <InputNumber min={0.01} precision={2} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item
                name="fechaRemate"
                label="Fecha remate"
                rules={[{ required: true, message: 'Indique fecha de remate' }]}
              >
                <Input type="date" />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="observacion" label="Observación">
                <Input />
              </Form.Item>
            </Col>
          </Row>
        </Form>
        <Card size="small" style={{ marginBottom: 12 }}>
          <Paragraph style={{ marginBottom: 4 }}>
            <Text strong>Resultado:</Text> se creará una solicitud de crédito en estado CRE y se abrirá el simulador con la ficha prendaria precargada.
          </Paragraph>
          <Text type="secondary">El monto final del préstamo se define en el simulador.</Text>
        </Card>
        <Button
          type="primary"
          icon={<FileAddOutlined />}
          disabled={!personaId || oficinaId < 1}
          loading={crearSolicitudPrendaria.isPending}
          onClick={() => crearSolicitudPrendaria.mutate()}
        >
          Crear solicitud prendaria y simular
        </Button>
      </CredixPanel>
    </CredixPage>
  )
}
