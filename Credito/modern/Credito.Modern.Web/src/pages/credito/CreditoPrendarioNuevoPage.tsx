import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { FileAddOutlined, SearchOutlined, UserAddOutlined } from '@ant-design/icons'
import { Alert, Button, Col, Form, Input, Row, Space, Steps, Typography, message } from 'antd'
import { buscarClientes, crearPersonaRapida } from '../../api/clientes'
import { crearSolicitudPrendaria, guardarBienesPrendario } from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { ClienteBuscarItem } from '../../types/api'
import type { PrendaItem } from '../../api/creditoGestion'
import { CredixDataTable, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import { prendaVacia, prendasValidas, totalTasacion } from '../../utils/prendas'
import { formatMoney } from '../../utils/formatMoney'

const { Paragraph, Text } = Typography

type ClienteRapidoForm = {
  dni: string
  nombre: string
  apePaterno: string
  apeMaterno: string
  celular?: string
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

export function CreditoPrendarioNuevoPage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [clienteForm] = Form.useForm<ClienteRapidoForm>()
  const [terminoCliente, setTerminoCliente] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])
  const [fechaRemate, setFechaRemate] = useState('')

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

  const crear = useMutation({
    mutationFn: async () => {
      if (!personaId) {
        throw new Error('Seleccione o cree un cliente')
      }
      const bienes = prendasValidas(prendas)
      if (bienes.length === 0) {
        throw new Error('Registre al menos un bien con tasación')
      }
      const solicitud = await crearSolicitudPrendaria({ oficinaId, personaId })
      await guardarBienesPrendario({
        oficinaId,
        creditoId: solicitud.solicitudCreditoId,
        prendas: bienes,
        fechaRemate: fechaRemate || null,
      })
      return solicitud.solicitudCreditoId
    },
    onSuccess: (solicitudCreditoId) => {
      message.success(`Solicitud prendaria #${solicitudCreditoId} creada`)
      navigate(`/credito/simulador?personaId=${personaId}&solicitudCreditoId=${solicitudCreditoId}&productoId=2`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const tasacion = useMemo(() => totalTasacion(prendas), [prendas])
  const stats: CredixStatItem[] = []
  if (personaId) {
    stats.push({ label: 'Cliente', value: clienteLabel || `Persona #${personaId}` })
  }
  if (tasacion > 0) {
    stats.push({ label: 'Tasación', value: formatMoney(tasacion), tone: 'green' })
  }

  return (
    <CredixPage
      title="Nuevo crédito prendario"
      subtitle="Alta de cliente, bienes en custodia y solicitud en estado CRE. El monto se define en el simulador."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: <Link to="/credito/prendario">Prendario</Link> },
        { title: 'Nuevo' },
      ]}
      stats={stats}
    >
      <Steps
        size="small"
        current={personaId ? 1 : 0}
        style={{ marginBottom: 24, maxWidth: 640 }}
        items={[{ title: 'Cliente' }, { title: 'Bienes' }, { title: 'Simulador' }]}
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

      <CredixPanel title="Bienes en custodia">
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
            message="Seleccione o cree el cliente antes de registrar los bienes."
          />
        )}
        <Paragraph type="secondary" style={{ marginBottom: 8 }}>
          Fecha de remate (opcional). Si se deja vacía, el servidor usa el vencimiento más 30 días.
        </Paragraph>
        <Input
          type="date"
          style={{ maxWidth: 220, marginBottom: 12 }}
          value={fechaRemate}
          onChange={(e) => setFechaRemate(e.target.value)}
          disabled={!personaId}
        />
        <PrendasEditor value={prendas} onChange={setPrendas} disabled={!personaId} />
        <Paragraph style={{ marginTop: 12, marginBottom: 4 }}>
          <Text strong>Resultado:</Text> se crea una solicitud prendaria (ProductoId 2, estado CRE) y se abrirá el
          simulador. El préstamo se define ahí.
        </Paragraph>
        <Button
          type="primary"
          icon={<FileAddOutlined />}
          disabled={!personaId || oficinaId < 1 || prendasValidas(prendas).length === 0}
          loading={crear.isPending}
          onClick={() => crear.mutate()}
        >
          Crear solicitud y simular
        </Button>
      </CredixPanel>
    </CredixPage>
  )
}
