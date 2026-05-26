import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckOutlined, PlusOutlined, ReloadOutlined, UndoOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Col,
  Form,
  Input,
  Modal,
  Row,
  Select,
  Space,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { fetchAlmacenes } from '../../api/almacenes'
import {
  confirmarTransferencia,
  crearTransferencia,
  desconfirmarTransferencia,
  eliminarSerieTransferencia,
  fetchTransferenciaCabecera,
  fetchTransferenciaDetalle,
  fetchTransferencias,
  validarSerieTransferencia,
  type TransferenciaDetalleLinea,
  type TransferenciaListRow,
} from '../../api/transferenciaAlmacen'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

function estadoTag(estado: string) {
  if (estado === 'C') {
    return <Tag color="success">Confirmada</Tag>
  }
  if (estado === 'P') {
    return <Tag color="processing">Pendiente</Tag>
  }
  return <Tag>{estado}</Tag>
}

export function TransferenciaAlmacenPage() {
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const oficinaId = session?.oficinaId ?? 0

  const [buscar, setBuscar] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [selId, setSelId] = useState(0)
  const [nuevaOpen, setNuevaOpen] = useState(false)
  const [destinoId, setDestinoId] = useState<number | null>(null)
  const [serieTerm, setSerieTerm] = useState('')

  const todosAlmacenes = useQuery({
    queryKey: ['almacenes-todos'],
    queryFn: () => fetchAlmacenes(),
    enabled: oficinaId > 0,
  })

  const destinos = (todosAlmacenes.data ?? []).filter((a) => a.oficinaId !== oficinaId)

  const listQuery = useQuery({
    queryKey: ['transferencias', oficinaId, buscar, page, pageSize],
    queryFn: () =>
      fetchTransferencias({
        oficinaId,
        buscar,
        page,
        pageSize,
      }),
    enabled: oficinaId > 0,
  })

  const cabeceraQuery = useQuery({
    queryKey: ['transferencia-cab', selId, oficinaId],
    queryFn: () => fetchTransferenciaCabecera(selId, oficinaId),
    enabled: selId > 0 && oficinaId > 0,
  })

  const detalleQuery = useQuery({
    queryKey: ['transferencia-det', selId, oficinaId],
    queryFn: () => fetchTransferenciaDetalle(selId, oficinaId),
    enabled: selId > 0 && oficinaId > 0,
  })

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: ['transferencias'] })
    if (selId > 0) {
      void queryClient.invalidateQueries({ queryKey: ['transferencia-cab', selId] })
      void queryClient.invalidateQueries({ queryKey: ['transferencia-det', selId] })
    }
  }

  const crear = useMutation({
    mutationFn: () => crearTransferencia({ oficinaId, almacenDestinoId: destinoId! }),
    onSuccess: (r) => {
      message.success(`Transferencia #${r.transferenciaId} creada`)
      setNuevaOpen(false)
      setDestinoId(null)
      setSelId(r.transferenciaId)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const validarSerie = useMutation({
    mutationFn: (numeroSerie: string) =>
      validarSerieTransferencia({
        oficinaId,
        transferenciaId: selId,
        numeroSerie,
      }),
    onSuccess: (r) => {
      if (r.error) {
        message.warning(r.mensaje ?? 'Serie no válida')
        return
      }
      message.success(`Serie ${r.numeroSerie} agregada`)
      setSerieTerm('')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const eliminarLinea = useMutation({
    mutationFn: (articuloId: number) =>
      eliminarSerieTransferencia({ oficinaId, transferenciaId: selId, articuloId }),
    onSuccess: () => {
      message.success('Series del artículo eliminadas')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const confirmar = useMutation({
    mutationFn: () => confirmarTransferencia({ oficinaId, transferenciaId: selId }),
    onSuccess: () => {
      message.success('Transferencia confirmada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const desconfirmar = useMutation({
    mutationFn: () => desconfirmarTransferencia({ oficinaId, transferenciaId: selId }),
    onSuccess: () => {
      message.success('Transferencia desconfirmada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cab = cabeceraQuery.data
  const editable = cab?.editable ?? false

  const listColumns: ColumnsType<TransferenciaListRow> = [
    { title: 'ID', dataIndex: 'transferenciaId', width: 70 },
    { title: 'Origen', dataIndex: 'almacenOrigen', ellipsis: true },
    { title: 'Destino', dataIndex: 'almacenDestino', ellipsis: true },
    {
      title: 'Fecha',
      dataIndex: 'fecha',
      width: 110,
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 110,
      render: (e: string) => estadoTag(e),
    },
  ]

  const detColumns: ColumnsType<TransferenciaDetalleLinea> = [
    { title: 'Artículo', dataIndex: 'articulo', ellipsis: true },
    { title: 'Cant.', dataIndex: 'cantidad', width: 60, align: 'center' },
    { title: 'Series', dataIndex: 'series', ellipsis: true },
    {
      title: '',
      key: 'act',
      width: 80,
      render: (_, r) =>
        editable ? (
          <Button
            type="link"
            danger
            size="small"
            onClick={() => eliminarLinea.mutate(r.articuloId)}
          >
            Quitar
          </Button>
        ) : null,
    },
  ]

  const stats = useMemo((): CredixStatItem[] => {
    if (selId > 0) {
      return [
        { value: selId, label: 'Transferencia' },
        { value: detalleQuery.data?.length ?? 0, label: 'Series' },
        { value: cabeceraQuery.data?.estado ?? '—', label: 'Estado' },
      ]
    }
    return [
      {
        value: listQuery.data?.totalCount ?? listQuery.data?.items?.length ?? 0,
        label: 'Transferencias',
      },
    ]
  }, [selId, detalleQuery.data, cabeceraQuery.data, listQuery.data])

  return (
    <CredixPage
      title="Transferencias de almacén"
      subtitle="Traslado de series entre oficinas: alta, escaneo y confirmación."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/almacen">Almacén</Link> },
        { title: 'Transferencia' },
      ]}
    >
      <Row gutter={[16, 16]}>
        <Col xs={24} lg={selId > 0 ? 10 : 24}>
          <CredixPanel
            title="Buscar transferencias"
            extra={
              <Space>
                <Button icon={<ReloadOutlined />} onClick={refrescar} loading={listQuery.isFetching}>
                  Actualizar
                </Button>
                <Button type="primary" icon={<PlusOutlined />} onClick={() => setNuevaOpen(true)}>
                  Nueva
                </Button>
              </Space>
            }
          >
            <Input.Search
              placeholder="Nro. TRA o fecha (dd/mm/yyyy)"
              allowClear
              value={buscar}
              onChange={(e) => setBuscar(e.target.value)}
              onSearch={() => {
                setPage(1)
                void listQuery.refetch()
              }}
              style={{ marginBottom: 12 }}
            />
            {listQuery.isError ? (
              <Alert type="error" showIcon message={errMsg(listQuery.error)} style={{ marginBottom: 12 }} />
            ) : null}
            <CredixDataTable<TransferenciaListRow>
              rowKey="transferenciaId"
              size="small"
              columns={listColumns}
              dataSource={listQuery.data?.items ?? []}
              loading={listQuery.isLoading}
              pagination={{
                current: page,
                pageSize,
                total: listQuery.data?.totalCount ?? 0,
                showSizeChanger: true,
                onChange: (p, ps) => {
                  setPage(p)
                  setPageSize(ps)
                },
              }}
              rowClassName={(r) => (r.transferenciaId === selId ? 'ant-table-row-selected' : '')}
              onRow={(r) => ({
                onClick: () => setSelId(r.transferenciaId),
                style: { cursor: 'pointer' },
              })}
            />
          </CredixPanel>
        </Col>

        {selId > 0 ? (
          <Col xs={24} lg={14}>
            <CredixPanel
              title={`Transferencia #${selId}`}
              extra={
                cab ? (
                  <Space>
                    {cab.estado === 'C' ? (
                      <Button
                        icon={<UndoOutlined />}
                        onClick={() => desconfirmar.mutate()}
                        loading={desconfirmar.isPending}
                      >
                        Desconfirmar
                      </Button>
                    ) : (
                      <Button
                        type="primary"
                        icon={<CheckOutlined />}
                        onClick={() => confirmar.mutate()}
                        loading={confirmar.isPending}
                        disabled={!editable}
                      >
                        Confirmar
                      </Button>
                    )}
                  </Space>
                ) : null
              }
            >
              {cab ? (
                <>
                  <Paragraph>
                    <Text strong>Origen:</Text> {cab.almacenOrigen}
                    <br />
                    <Text strong>Destino:</Text> {cab.almacenDestino}
                    <br />
                    <Text strong>Fecha:</Text> {formatFecha(cab.fecha)} — {estadoTag(cab.estado)}
                  </Paragraph>

                  {editable ? (
                    <Form layout="inline" style={{ marginBottom: 12 }}>
                      <Form.Item label="Serie" style={{ flex: 1, minWidth: 200 }}>
                        <Input.Search
                          placeholder="Escanear o escribir número de serie"
                          value={serieTerm}
                          onChange={(e) => setSerieTerm(e.target.value)}
                          onSearch={(v) => validarSerie.mutate(v)}
                          loading={validarSerie.isPending}
                        />
                      </Form.Item>
                    </Form>
                  ) : (
                    <Alert
                      type="info"
                      showIcon
                      message="Transferencia confirmada. Desconfirme para editar series."
                      style={{ marginBottom: 12 }}
                    />
                  )}

                  <CredixDataTable<TransferenciaDetalleLinea>
                    rowKey="articuloId"
                    size="small"
                    columns={detColumns}
                    dataSource={detalleQuery.data ?? []}
                    loading={detalleQuery.isLoading}
                    pagination={false}
                    locale={{ emptyText: 'Sin series registradas' }}
                  />
                </>
              ) : (
                <Alert type="warning" showIcon message="No se pudo cargar la transferencia" />
              )}
            </CredixPanel>
          </Col>
        ) : null}
      </Row>

      <Modal
        title="Nueva transferencia"
        open={nuevaOpen}
        onCancel={() => {
          setNuevaOpen(false)
          setDestinoId(null)
        }}
        onOk={() => {
          if (!destinoId) {
            message.warning('Seleccione almacén destino')
            return
          }
          crear.mutate()
        }}
        confirmLoading={crear.isPending}
        okText="Crear"
      >
        <Paragraph type="secondary">
          El almacén origen es el de su oficina. Elija destino en otra oficina.
        </Paragraph>
        <Select
          style={{ width: '100%' }}
          placeholder="Almacén destino (otra oficina)"
          options={destinos.map((a) => ({
            value: a.almacenId,
            label: `${a.denominacion} (oficina ${a.oficinaId})`,
          }))}
          value={destinoId ?? undefined}
          onChange={setDestinoId}
          showSearch
          optionFilterProp="label"
        />
      </Modal>
    </CredixPage>
  )
}
