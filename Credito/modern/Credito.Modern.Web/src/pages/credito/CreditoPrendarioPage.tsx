import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusOutlined, SearchOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Alert, Badge, Button, Input, Modal, Space, Table, Tag, Tooltip, Typography, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  enviarAvisosVencimientoPrendario,
  fetchPrendarioAvisosEstado,
  fetchPrendarioAvisosVencimiento,
  fetchPrendarioCreditos,
  fetchPrendarioResumen,
  type PrendarioAvisoEnvioResumen,
  type PrendarioAvisoVencimiento,
  type PrendarioCreditoRow,
  type PrendarioWhatsAppEstado,
  type PrendarioWhatsAppPasada,
} from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, type CredixStatItem } from '../../components/credix'
import { formatFecha, formatFechaHora } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { celularPrendarioEsValido } from '../../utils/prendarioWhatsapp'
import { situacionPrendario } from '../../utils/prendarioSituacion'

const { Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

function etiquetaOrigen(origen: string): string {
  if (origen === 'programada') {
    return 'automática'
  }
  if (origen === 'arranque') {
    return 'al arrancar el servidor'
  }
  if (origen === 'manual') {
    return 'manual'
  }
  return origen
}

function textoPasada(pasada: PrendarioWhatsAppPasada): string {
  const conteo = `${pasada.enviados} enviados, ${pasada.fallidos} fallidos, ${pasada.omitidos} omitidos`
  const aviso = pasada.advertencia ? ` ${pasada.advertencia}` : ''
  return `Última pasada (${etiquetaOrigen(pasada.origen)}): ${formatFechaHora(pasada.fecha)} — ${conteo}.${aviso}`
}

function AvisosCanalAlert({
  estado,
  pendientes,
}: {
  estado: PrendarioWhatsAppEstado | undefined
  pendientes: number
}) {
  if (!estado) {
    return null
  }

  const hora = `${String(estado.dailyHourLocal).padStart(2, '0')}:00`
  const pasada = estado.ultimaPasada ? textoPasada(estado.ultimaPasada) : 'Aún no hay una pasada registrada en este servidor.'

  if (!estado.configurado) {
    return (
      <Alert
        type="warning"
        showIcon
        style={{ marginTop: 12 }}
        message="WhatsApp Business no está configurado"
        description="Falta el token o el número de envío en el servidor. El aviso automático y el envío de la plantilla quedan en pausa hasta completar user-secrets."
      />
    )
  }

  if (!estado.automaticoActivo) {
    return (
      <Alert
        type="warning"
        showIcon
        style={{ marginTop: 12 }}
        message="El envío automático está desactivado"
        description={`Puede enviar la plantilla a mano desde esta ventana. ${pasada}`}
      />
    )
  }

  return (
    <Alert
      type="info"
      showIcon
      style={{ marginTop: 12 }}
      message={`Envío automático todos los días a las ${hora} (hora de Lima).`}
      description={`Próxima corrida ${formatFechaHora(estado.proximaCorrida)}. ${pendientes} pendiente${pendientes === 1 ? '' : 's'} en esta oficina. ${pasada}`}
    />
  )
}

export function CreditoPrendarioPage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [buscar, setBuscar] = useState('')
  const [termino, setTermino] = useState('')
  const [page, setPage] = useState(1)
  const [avisosOpen, setAvisosOpen] = useState(false)
  const [ultimoEnvio, setUltimoEnvio] = useState<PrendarioAvisoEnvioResumen | null>(null)
  const queryClient = useQueryClient()

  const resumen = useQuery({
    queryKey: ['prendario-resumen', oficinaId],
    queryFn: () => fetchPrendarioResumen(oficinaId),
    enabled: oficinaId > 0,
  })

  const listado = useQuery({
    queryKey: ['prendario-creditos', oficinaId, termino, page],
    queryFn: () => fetchPrendarioCreditos({ oficinaId, buscar: termino, page, pageSize: 20 }),
    enabled: oficinaId > 0,
  })

  const avisos = useQuery({
    queryKey: ['prendario-avisos', oficinaId],
    queryFn: () => fetchPrendarioAvisosVencimiento(oficinaId, 3),
    enabled: oficinaId > 0,
  })

  const estadoAvisos = useQuery({
    queryKey: ['prendario-avisos-estado'],
    queryFn: fetchPrendarioAvisosEstado,
    enabled: oficinaId > 0,
    staleTime: 30_000,
  })

  const resultadoPorCredito = useMemo(() => {
    const map = new Map<number, { exito: boolean; mensaje: string }>()
    for (const item of ultimoEnvio?.detalle ?? []) {
      if (item.creditoId > 0) {
        map.set(item.creditoId, { exito: item.exito, mensaje: item.mensaje })
      }
    }
    return map
  }, [ultimoEnvio])

  const enviarAvisos = useMutation({
    mutationFn: (creditoId?: number) =>
      enviarAvisosVencimientoPrendario({
        oficinaId,
        diasAntes: 3,
        creditoId,
      }),
    onSuccess: (r) => {
      setUltimoEnvio(r)
      void queryClient.invalidateQueries({ queryKey: ['prendario-avisos', oficinaId] })
      void queryClient.invalidateQueries({ queryKey: ['prendario-avisos-estado'] })
      if (r.advertencia) {
        message.warning(r.advertencia)
        return
      }
      if (r.enviados === 0 && r.fallidos === 0 && r.omitidos === 0) {
        message.info('No hay avisos pendientes')
        return
      }
      message.success(`Enviados ${r.enviados}. Fallidos ${r.fallidos}. Omitidos ${r.omitidos}.`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const stats: CredixStatItem[] = useMemo(() => {
    const r = resumen.data
    if (!r) {
      return []
    }
    return [
      { label: 'Desembolsados', value: r.total },
      { label: 'Por vencer (3 días)', value: r.porVencer },
      { label: 'Vencidos', value: r.vencidos, tone: 'red' },
      { label: 'Rematados', value: r.rematados },
    ]
  }, [resumen.data])

  const columns: ColumnsType<PrendarioCreditoRow> = useMemo(
    () => [
    { title: 'Crédito', dataIndex: 'creditoId', width: 90 },
    {
      title: 'Contrato',
      dataIndex: 'numeroContratoPrendario',
      width: 110,
      render: (v: string | null) => v || '(pendiente)',
    },
    { title: 'DNI', dataIndex: 'numeroDocumento', width: 100 },
    { title: 'Cliente', dataIndex: 'nombreCompleto', ellipsis: true },
    {
      title: 'Tasación',
      dataIndex: 'montoTasacion',
      width: 110,
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: 'Préstamo',
      dataIndex: 'montoCredito',
      width: 110,
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: 'Vence',
      dataIndex: 'fechaVencimiento',
      width: 110,
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Remate',
      dataIndex: 'fechaRemate',
      width: 110,
      render: (v: string | null) => formatFecha(v),
    },
    {
      title: 'Días',
      dataIndex: 'diasParaVencer',
      width: 70,
      align: 'right',
      render: (v: number) => (
        <Text type={v < 0 ? 'danger' : v <= 3 ? 'warning' : undefined}>{v}</Text>
      ),
    },
    {
      title: 'Situación',
      key: 'situacion',
      width: 130,
      render: (_, row) => {
        const s = situacionPrendario(row)
        return (
          <Tooltip title={s.hint}>
            <Tag color={s.color}>{s.label}</Tag>
          </Tooltip>
        )
      },
    },
    {
      title: '',
      key: 'acciones',
      width: 110,
      render: (_, row) => (
        <Button
          type="link"
          size="small"
          onClick={() =>
            navigate(`/credito/prendario/gestionar/${row.personaId}?creditoId=${row.creditoId}`)
          }
        >
          Gestionar
        </Button>
      ),
    },
  ],
    [navigate],
  )

  const pendientes = avisos.data?.length ?? 0
  const canalListo = estadoAvisos.data?.configurado !== false
  const puedeEnviar = canalListo && pendientes > 0

  return (
    <CredixPage
      title="Crédito prendario"
      subtitle="Cartera de créditos con bienes en custodia. Las tarjetas miden solo desembolsos."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Prendario' },
      ]}
      stats={stats}
      actions={
        <Space wrap>
          <Badge count={pendientes} overflowCount={99} size="small">
            <Button onClick={() => setAvisosOpen(true)}>Avisos a 3 días</Button>
          </Badge>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/credito/prendario/nuevo')}>
            Nuevo
          </Button>
        </Space>
      }
    >
      <Space.Compact style={{ width: 'min(480px, 100%)', marginBottom: 16 }}>
        <Input
          placeholder="Cliente, DNI o contrato"
          value={buscar}
          onChange={(e) => setBuscar(e.target.value)}
          onPressEnter={() => {
            setPage(1)
            setTermino(buscar.trim())
          }}
        />
        <Button
          icon={<SearchOutlined />}
          loading={listado.isFetching}
          onClick={() => {
            setPage(1)
            setTermino(buscar.trim())
          }}
        >
          Buscar
        </Button>
      </Space.Compact>
      {listado.error ? (
        <Text type="danger">{errMsg(listado.error)}</Text>
      ) : null}
      <CredixDataTable<PrendarioCreditoRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={listado.data?.items ?? []}
        loading={listado.isFetching}
        pagination={{
          current: page,
          pageSize: 20,
          total: listado.data?.total ?? 0,
          showSizeChanger: false,
          onChange: (p) => setPage(p),
        }}
        locale={{ emptyText: 'No hay créditos prendarios en esta oficina' }}
        scroll={{ x: 'max-content' }}
      />
      <Modal
        title="Avisos de vencimiento (3 días)"
        open={avisosOpen}
        onCancel={() => {
          setAvisosOpen(false)
          setUltimoEnvio(null)
        }}
        width="min(800px, 96vw)"
        footer={
          <Button
            type="primary"
            icon={<WhatsAppOutlined />}
            loading={enviarAvisos.isPending}
            disabled={!puedeEnviar}
            onClick={() => enviarAvisos.mutate(undefined)}
          >
            Enviar plantilla WhatsApp
          </Button>
        }
      >
        <Text type="secondary">
          Se envía la plantilla aprobada aviso_vencimiento_prendario por WhatsApp Business a quienes
          vencen exactamente en 3 días y aún no fueron avisados hoy. Un crédito avisado no se vuelve
          a notificar hasta mañana.
        </Text>
        <AvisosCanalAlert estado={estadoAvisos.data} pendientes={pendientes} />
        {ultimoEnvio && (ultimoEnvio.enviados > 0 || ultimoEnvio.fallidos > 0 || ultimoEnvio.omitidos > 0) ? (
          <Alert
            type={ultimoEnvio.fallidos > 0 ? 'warning' : 'success'}
            showIcon
            style={{ marginTop: 12 }}
            message={`Resultado: ${ultimoEnvio.enviados} enviados, ${ultimoEnvio.fallidos} fallidos, ${ultimoEnvio.omitidos} omitidos.`}
          />
        ) : null}
        {estadoAvisos.error ? (
          <Text type="danger" style={{ display: 'block', marginTop: 8 }}>
            {errMsg(estadoAvisos.error)}
          </Text>
        ) : null}
        <Table<PrendarioAvisoVencimiento>
          rowKey="creditoId"
          size="small"
          style={{ marginTop: 12 }}
          loading={avisos.isFetching}
          pagination={false}
          scroll={{ x: 'max-content' }}
          dataSource={avisos.data ?? []}
          locale={{
            emptyText: canalListo
              ? 'Nadie vence en 3 días o el aviso de hoy ya se envió. El automático cubre el resto a las 08:00.'
              : 'No hay pendientes. Configure WhatsApp Business para enviar la plantilla.',
          }}
          columns={[
            { title: 'Crédito', dataIndex: 'creditoId', width: 90 },
            { title: 'Cliente', dataIndex: 'nombreCliente', ellipsis: true },
            {
              title: 'Celular',
              dataIndex: 'celular',
              width: 130,
              render: (v: string | null) =>
                celularPrendarioEsValido(v) ? (
                  v
                ) : (
                  <Tooltip title="Sin celular válido; se omite en el envío de plantilla">
                    <Tag color="orange">{v?.trim() ? v : 'Sin celular'}</Tag>
                  </Tooltip>
                ),
            },
            {
              title: 'A cancelar',
              dataIndex: 'montoCancelar',
              width: 110,
              align: 'right',
              render: (v: number) => formatMoney(v),
            },
            {
              title: 'Estado',
              key: 'estado',
              width: 110,
              render: (_, row) => {
                const r = resultadoPorCredito.get(row.creditoId)
                if (r?.exito) {
                  return <Tag color="green">Enviado</Tag>
                }
                if (r && !r.exito) {
                  return (
                    <Tooltip title={r.mensaje}>
                      <Tag color={r.mensaje.toLowerCase().includes('celular') ? 'orange' : 'red'}>
                        {r.mensaje.toLowerCase().includes('celular') ? 'Omitido' : 'Falló'}
                      </Tag>
                    </Tooltip>
                  )
                }
                return <Tag>Pendiente</Tag>
              },
            },
            {
              title: '',
              key: 'wa',
              width: 90,
              render: (_, row) => (
                <Button
                  type="link"
                  size="small"
                  icon={<WhatsAppOutlined />}
                  loading={enviarAvisos.isPending}
                  disabled={!canalListo || !celularPrendarioEsValido(row.celular)}
                  onClick={() => enviarAvisos.mutate(row.creditoId)}
                >
                  Enviar
                </Button>
              ),
            },
          ]}
        />
      </Modal>
    </CredixPage>
  )
}
