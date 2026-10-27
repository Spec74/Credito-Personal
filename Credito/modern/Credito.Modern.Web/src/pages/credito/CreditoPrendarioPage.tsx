import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusOutlined, SearchOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Alert, Badge, Button, Input, Modal, Space, Tag, Tooltip, Typography, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  enviarAvisosVencimientoPrendario,
  fetchPrendarioAvisosEstado,
  fetchPrendarioAvisosVencimiento,
  fetchPrendarioCreditos,
  fetchPrendarioResumen,
  type PrendarioAvisoEnvioResumen,
  type PrendarioAvisoVencimiento,
  type PrendarioCategoria,
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
import '../../styles/credito-prendario-gestion.css'
import { situacionPrendario } from '../../utils/prendarioSituacion'

const { Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

function etiquetaAccionListado(row: PrendarioCreditoRow): string {
  if (row.categoria === 'SinBienes' || row.bienes < 1) {
    return 'Completar bienes'
  }
  const estado = (row.estado ?? '').toUpperCase()
  if (estado === 'CRE' || estado === 'PEN' || estado === 'AP1' || estado === 'APR') {
    return 'Continuar'
  }
  return 'Gestionar'
}

function textoVacioListado(categoria: PrendarioCategoria | undefined): string {
  switch (categoria) {
    case 'Otro':
      return 'No hay solicitudes en trámite en esta oficina'
    case 'SinBienes':
      return 'No hay créditos prendarios sin bienes'
    case 'PorVencer':
      return 'Nadie vence en los próximos 3 días'
    case 'Vencido':
      return 'No hay créditos vencidos'
    case 'Rematado':
      return 'No hay créditos en cola de remate ni rematados'
    default:
      return 'No hay créditos prendarios en esta oficina. Pulse Nuevo para originar.'
  }
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
        description="Falta el token o el Phone Number ID en el servidor (WhatsApp__Token / WhatsApp__PhoneNumberId)."
      />
    )
  }

  if (
    estado.webhookVerifyTokenConfigurado === false
    || estado.webhookAppSecretConfigurado === false
  ) {
    return (
      <Alert
        type="warning"
        showIcon
        style={{ marginTop: 12 }}
        message="Webhook Meta incompleto"
        description={`Configure WhatsApp__VerifyToken y WhatsApp__AppSecret en Azure para Verificar y guardar en Meta. ${pasada}`}
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
  const [categoria, setCategoria] = useState<PrendarioCategoria | undefined>()
  const [avisosOpen, setAvisosOpen] = useState(false)
  const [ultimoEnvio, setUltimoEnvio] = useState<PrendarioAvisoEnvioResumen | null>(null)
  const queryClient = useQueryClient()

  const resumen = useQuery({
    queryKey: ['prendario-resumen', oficinaId],
    queryFn: () => fetchPrendarioResumen(oficinaId),
    enabled: oficinaId > 0,
  })

  const listado = useQuery({
    queryKey: ['prendario-creditos', oficinaId, termino, page, categoria],
    queryFn: () =>
      fetchPrendarioCreditos({ oficinaId, buscar: termino, categoria, page, pageSize: 20 }),
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

  const elegirCola = (cola: PrendarioCategoria) => {
    setPage(1)
    setCategoria((actual) => (actual === cola ? undefined : cola))
  }

  const stats: CredixStatItem[] = useMemo(() => {
    const r = resumen.data
    if (!r) {
      return []
    }
    const colas: CredixStatItem[] = [
      {
        label: 'Solicitudes',
        value: r.solicitudes ?? 0,
        active: categoria === 'Otro',
        onClick: () => elegirCola('Otro'),
      },
      {
        label: 'Cartera',
        value: `S/ ${formatMoney(r.montoCartera ?? 0)}`,
        detail: r.total ? `${r.total} desembolsados` : undefined,
        active: categoria == null,
        onClick: () => {
          setPage(1)
          setCategoria(undefined)
        },
      },
      {
        label: 'Por vencer',
        value: r.porVencer,
        active: categoria === 'PorVencer',
        onClick: () => elegirCola('PorVencer'),
      },
      {
        label: 'Vencidos',
        value: r.vencidos,
        tone: 'red',
        active: categoria === 'Vencido',
        onClick: () => elegirCola('Vencido'),
      },
      {
        label: 'Remate',
        value: r.rematados,
        active: categoria === 'Rematado',
        onClick: () => elegirCola('Rematado'),
      },
    ]
    return colas
  }, [resumen.data, categoria])

  const columns: ColumnsType<PrendarioCreditoRow> = useMemo(
    () => [
      {
        title: 'Cliente',
        key: 'cliente',
        width: '24%',
        render: (_, row) => (
          <div className="prendario-lista-cliente">
            <strong title={row.nombreCompleto ?? undefined}>
              {row.nombreCompleto || 'Sin nombre'}
            </strong>
            <span>{row.numeroDocumento || 'Sin DNI'}</span>
          </div>
        ),
      },
      {
        title: 'Préstamo',
        dataIndex: 'montoCredito',
        width: '18%',
        align: 'right',
        render: (v: number) => <strong className="prendario-lista-num">{formatMoney(v)}</strong>,
      },
      {
        title: 'Tasación',
        dataIndex: 'montoTasacion',
        width: '18%',
        align: 'right',
        render: (v: number) =>
          v > 0 ? (
            <span className="prendario-lista-num">{formatMoney(v)}</span>
          ) : (
            <Text type="secondary">—</Text>
          ),
      },
      {
        title: 'Vence',
        key: 'vence',
        width: '18%',
        align: 'right',
        render: (_, row) => {
          const dias = row.diasParaVencer
          const urgente = dias < 0 ? 'danger' : dias <= 3 ? 'warning' : undefined
          return (
            <div className="prendario-lista-plazo">
              <strong>{formatFecha(row.fechaVencimiento)}</strong>
              <Text type={urgente}>
                {row.categoria === 'Rematado' && row.fechaRemate
                  ? `Remate ${formatFecha(row.fechaRemate)}`
                  : dias < 0
                    ? `${Math.abs(dias)} día${Math.abs(dias) === 1 ? '' : 's'} vencido`
                    : dias === 0
                      ? 'Vence hoy'
                      : `${dias} día${dias === 1 ? '' : 's'}`}
              </Text>
            </div>
          )
        },
      },
      {
        title: 'Estado',
        key: 'acciones',
        width: '22%',
        render: (_, row) => {
          const s = situacionPrendario(row)
          return (
            <div className="prendario-lista-estado">
              <Tooltip title={s.hint}>
                <Tag color={s.color}>{s.label}</Tag>
              </Tooltip>
              <Button
                type="link"
                size="small"
                onClick={(e) => {
                  e.stopPropagation()
                  navigate(`/credito/prendario/gestionar/${row.personaId}?creditoId=${row.creditoId}`)
                }}
              >
                {etiquetaAccionListado(row)}
              </Button>
            </div>
          )
        },
      },
    ],
    [navigate],
  )

  const pendientes = avisos.data?.length ?? 0
  const canalListo = estadoAvisos.data?.configurado !== false
  const puedeEnviar = canalListo && pendientes > 0

  return (
    <CredixPage
      className="credito-prendario-page"
      title="Crédito prendario"
      subtitle="Bandeja de esta oficina. Pulse una cifra para filtrar."
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
      <div className="credito-prendario-toolbar">
        {categoria ? (
          <Button type="link" onClick={() => { setPage(1); setCategoria(undefined) }}>
            Ver todas
          </Button>
        ) : (
          <span />
        )}
        <Space.Compact className="credito-prendario-toolbar__buscar">
          <Input
            allowClear
            placeholder="Cliente o DNI"
            value={buscar}
            onChange={(e) => {
              setBuscar(e.target.value)
              if (e.target.value === '') {
                setPage(1)
                setTermino('')
              }
            }}
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
      </div>
      {listado.error ? (
        <Text type="danger">{errMsg(listado.error)}</Text>
      ) : null}
      <CredixDataTable<PrendarioCreditoRow>
        mode="operacion"
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
        locale={{ emptyText: textoVacioListado(categoria) }}
        tableLayout="fixed"
        onRow={(row) => ({
          style: { cursor: 'pointer' },
          onClick: () =>
            navigate(`/credito/prendario/gestionar/${row.personaId}?creditoId=${row.creditoId}`),
        })}
      />
      <Modal
        title="Avisos de vencimiento (3 días)"
        open={avisosOpen}
        onCancel={() => {
          setAvisosOpen(false)
          setUltimoEnvio(null)
        }}
        wrapClassName="credix-modal-operativo"
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
          vencen exactamente en 3 días (según listado actual). El envío no marca créditos en BD: puede
          reenviarse si vuelve a aparecer en la lista.
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
        <CredixDataTable<PrendarioAvisoVencimiento>
          mode="operacion"
          rowKey="creditoId"
          style={{ marginTop: 12 }}
          loading={avisos.isFetching}
          pagination={false}
          scroll={{ x: 720 }}
          dataSource={avisos.data ?? []}
          locale={{
            emptyText: canalListo
              ? 'Nadie vence en 3 días en esta oficina. El automático (si está activo) corre a las 08:00 Lima.'
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
              title: 'Acciones',
              key: 'acciones',
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
