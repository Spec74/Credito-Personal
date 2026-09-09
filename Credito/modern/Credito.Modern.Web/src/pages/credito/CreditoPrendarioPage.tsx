import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusOutlined, SearchOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Button, Input, Modal, Space, Table, Tag, Tooltip, Typography, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  enviarAvisosVencimientoPrendario,
  fetchPrendarioAvisosVencimiento,
  fetchPrendarioCreditos,
  fetchPrendarioResumen,
  type PrendarioAvisoVencimiento,
  type PrendarioCreditoRow,
} from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, type CredixStatItem } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { abrirWhatsAppPrendario } from '../../utils/prendarioWhatsapp'
import { situacionPrendario } from '../../utils/prendarioSituacion'

const { Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

export function CreditoPrendarioPage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [buscar, setBuscar] = useState('')
  const [termino, setTermino] = useState('')
  const [page, setPage] = useState(1)
  const [avisosOpen, setAvisosOpen] = useState(false)
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
    enabled: oficinaId > 0 && avisosOpen,
  })

  const enviarAvisos = useMutation({
    mutationFn: (creditoId?: number) =>
      enviarAvisosVencimientoPrendario({
        oficinaId,
        diasAntes: 3,
        creditoId,
      }),
    onSuccess: (r) => {
      void queryClient.invalidateQueries({ queryKey: ['prendario-avisos', oficinaId] })
      if (r.enviados === 0 && r.fallidos === 0 && r.omitidos === 0) {
        message.info(r.detalle[0]?.mensaje ?? 'No hay avisos pendientes')
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
      width: 170,
      render: (_, row) => (
        <Space size={0}>
          <Button
            type="link"
            size="small"
            onClick={() =>
              navigate(`/credito/prendario/gestionar/${row.personaId}?creditoId=${row.creditoId}`)
            }
          >
            Gestionar
          </Button>
          <Button
            type="link"
            size="small"
            icon={<WhatsAppOutlined />}
            onClick={() => {
              const ok = abrirWhatsAppPrendario(row.celular, row.nombreCompleto ?? '', row.creditoId)
              if (!ok) {
                message.warning('Este cliente no tiene celular registrado')
              }
            }}
          />
        </Space>
      ),
    },
  ],
    [navigate],
  )

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
          <Button onClick={() => setAvisosOpen(true)}>Avisos a 3 días</Button>
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
        onCancel={() => setAvisosOpen(false)}
        width={720}
        footer={
          <Button
            type="primary"
            icon={<WhatsAppOutlined />}
            loading={enviarAvisos.isPending}
            disabled={(avisos.data?.length ?? 0) === 0}
            onClick={() => enviarAvisos.mutate(undefined)}
          >
            Enviar plantilla WhatsApp
          </Button>
        }
      >
        <Text type="secondary">
          Se envía la plantilla aviso_vencimiento_prendario por WhatsApp Business a quienes vencen
          exactamente en 3 días y aún no fueron avisados hoy.
        </Text>
        <Table<PrendarioAvisoVencimiento>
          rowKey="creditoId"
          size="small"
          style={{ marginTop: 12 }}
          loading={avisos.isFetching}
          pagination={false}
          dataSource={avisos.data ?? []}
          locale={{ emptyText: 'Nadie vence en 3 días o ya fueron avisados hoy' }}
          columns={[
            { title: 'Crédito', dataIndex: 'creditoId', width: 90 },
            { title: 'Cliente', dataIndex: 'nombreCliente', ellipsis: true },
            { title: 'Celular', dataIndex: 'celular', width: 120 },
            {
              title: 'A cancelar',
              dataIndex: 'montoCancelar',
              width: 110,
              align: 'right',
              render: (v: number) => formatMoney(v),
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
