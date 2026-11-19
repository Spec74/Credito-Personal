import { useCallback, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Modal,
  Segmented,
  Space,
  Table,
  Tabs,
  Tag,
  Upload,
  message,
} from 'antd'
import {
  CheckOutlined,
  CloudUploadOutlined,
  FileExcelOutlined,
  FilePdfOutlined,
  PictureOutlined,
} from '@ant-design/icons'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import type { UploadFile } from 'antd/es/upload/interface'
import {
  conciliarPagosPlin,
  conciliarPagosYape,
  downloadPagosNoVerificadosCsv,
  downloadPagosNoVerificadosPdf,
  fetchPagosNoVerificados,
  verificarPagoTransferencia,
} from '../../api/cajaDiario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import {
  CredixCrudPage,
  CredixDataTable,
  CredixQueue,
  confirmMoney,
  type CredixStatItem,
} from '../../components/credix'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import type {
  MedioDigitalConciliacionResultado,
  MedioDigitalMatchPair,
  PagosNoVerificadosRow,
} from '../../types/api'
import { filterTableRows } from '../../utils/tableClientFilter'
import { formatMoney } from '../../utils/formatMoney'
import { sumMoney } from '../../utils/money'
import {
  esCreditoAdministrador,
  esCreditoEncargado,
} from '../../utils/creditoOperacionPermisos'
import { CajaListToolbar } from './components/CajaListToolbar'
import { VerificarPagosTableEmpty } from './components/VerificarPagosTableEmpty'
import { TipoPagoCell } from '../tesoreria/components/TipoPagoBrand'
import '../../styles/caja-verificar-pagos-module.css'

const PAGE_SIZES = ['45', '60', '75'] as const

function errMsg(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Error en la operación'
}

function pagoRowText(row: PagosNoVerificadosRow): string {
  return [
    row.movimientoCajaId,
    row.cliente,
    row.movimiento,
    row.importePago,
    row.tipoPago,
    row.fechaTransferencia,
    row.registro,
  ].join(' ')
}

function confirmarVerificacion(row: PagosNoVerificadosRow, onOk: () => void): void {
  confirmMoney({
    title: 'Verificar depósito',
    amount: row.importePago,
    officeLabel: [row.cliente, row.movimiento, row.tipoPago].filter(Boolean).join(' · '),
    consequence: 'El depósito quedará marcado como verificado.',
    okText: 'Sí, verificar',
    onOk,
  })
}

function estadoTag(estado: string) {
  switch (estado) {
    case 'Auto':
      return <Tag color="success">Auto</Tag>
    case 'Sugerido':
      return <Tag color="processing">Sugerido</Tag>
    case 'Ambiguo':
      return <Tag color="warning">Ambiguo</Tag>
    default:
      return <Tag>Sin match</Tag>
  }
}

type ResultFilter = 'Todos' | 'Auto' | 'Sugerido' | 'Ambiguo' | 'SinMatch'

export function VerificarPagosPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const roles = session?.roles ?? []
  const puedeConciliar = esCreditoEncargado(roles) || esCreditoAdministrador(roles)

  const [buscar, setBuscar] = useState('')
  const buscarDebounced = useDebouncedValue(buscar.trim(), 300)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(45)

  const [conciliarOpen, setConciliarOpen] = useState(false)
  const [tab, setTab] = useState<'yape' | 'plin'>('yape')
  const [yapeFile, setYapeFile] = useState<File | null>(null)
  const [plinFiles, setPlinFiles] = useState<UploadFile[]>([])
  const [resultado, setResultado] = useState<MedioDigitalConciliacionResultado | null>(null)
  const [resultFilter, setResultFilter] = useState<ResultFilter>('Todos')

  const listQuery = useQuery({
    queryKey: ['pagos-no-verificados', oficinaId],
    queryFn: () => fetchPagosNoVerificados(oficinaId),
    enabled: oficinaId > 0,
  })

  const pendientes = useMemo(() => listQuery.data ?? [], [listQuery.data])

  const filtrados = useMemo(
    () => filterTableRows(pendientes, buscarDebounced, pagoRowText),
    [pendientes, buscarDebounced],
  )

  const totalImporte = useMemo(
    () => sumMoney(filtrados.map((r) => r.importePago ?? 0)),
    [filtrados],
  )

  const verificar = useMutation({
    mutationFn: (movimientoCajaId: number) =>
      verificarPagoTransferencia({ oficinaId, movimientoCajaId }),
    onSuccess: (r) => {
      message.success(
        r.yaVerificado
          ? 'El pago ya estaba verificado'
          : `Pago verificado (mov. ${r.movimientoCajaId})`,
      )
      setSelectedId(null)
      setResultado((prev) => {
        if (!prev) return prev
        const pares = prev.pares.filter((p) => p.movimientoCajaId !== r.movimientoCajaId)
        return {
          ...prev,
          pares,
          auto: pares.filter((p) => p.estado === 'Auto').length,
          sugeridos: pares.filter((p) => p.estado === 'Sugerido').length,
          ambiguos: pares.filter((p) => p.estado === 'Ambiguo').length,
          sinMatch: pares.filter((p) => p.estado === 'SinMatch').length,
          pendientesSistema: Math.max(0, prev.pendientesSistema - 1),
        }
      })
      void listQuery.refetch()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const pedirVerificar = useCallback((row: PagosNoVerificadosRow) => {
    confirmarVerificacion(row, () => verificar.mutate(row.movimientoCajaId))
  }, [verificar])

  const pedirVerificarPar = useCallback(
    (par: MedioDigitalMatchPair) => {
      confirmMoney({
        title: 'Verificar depósito',
        amount: par.importePago,
        officeLabel: [par.cliente, par.tipoPago, par.estado].filter(Boolean).join(' · '),
        consequence:
          'Confirmación manual del encargado. El depósito quedará marcado como verificado.',
        okText: 'Sí, verificar',
        onOk: () => verificar.mutate(par.movimientoCajaId),
      })
    },
    [verificar],
  )

  const csv = useMutation({
    mutationFn: () => downloadPagosNoVerificadosCsv(oficinaId),
    onSuccess: () => message.success('CSV descargado'),
    onError: (e) => message.error(errMsg(e)),
  })

  const pdf = useMutation({
    mutationFn: () => downloadPagosNoVerificadosPdf(oficinaId),
    onSuccess: () => message.success('PDF descargado'),
    onError: (e) => message.error(errMsg(e)),
  })

  const previewYape = useMutation({
    mutationFn: (archivo: File) =>
      conciliarPagosYape({ oficinaId, archivo, aplicarAutos: false }),
    onSuccess: (r) => {
      setResultado(r)
      message.success(`Yape: ${r.filasIngreso} TE PAGÓ · ${r.auto} únicos`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const applyYape = useMutation({
    mutationFn: (archivo: File) =>
      conciliarPagosYape({ oficinaId, archivo, aplicarAutos: true }),
    onSuccess: (r) => {
      const restantes = r.pares.filter((p) => p.estado !== 'Auto')
      setResultado({
        ...r,
        pares: restantes,
        auto: 0,
        sugeridos: restantes.filter((p) => p.estado === 'Sugerido').length,
        ambiguos: restantes.filter((p) => p.estado === 'Ambiguo').length,
        sinMatch: restantes.filter((p) => p.estado === 'SinMatch').length,
      })
      setResultFilter('Todos')
      void listQuery.refetch()
      message.success(
        `Yape auto: ${r.verificadosAplicados} verificados (ya OK: ${r.yaVerificados})`,
      )
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const plinFileList = useMemo(() => {
    const out: File[] = []
    for (const f of plinFiles) {
      const raw = f.originFileObj
      if (raw) out.push(raw as File)
    }
    return out
  }, [plinFiles])

  const previewPlin = useMutation({
    mutationFn: (capturas: File[]) =>
      conciliarPagosPlin({ oficinaId, capturas, aplicarAutos: false }),
    onSuccess: (r) => {
      setResultado(r)
      message.success(`Plin: ${r.filasIngreso} ingresos · ${r.auto} únicos`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const applyPlin = useMutation({
    mutationFn: (capturas: File[]) =>
      conciliarPagosPlin({ oficinaId, capturas, aplicarAutos: true }),
    onSuccess: (r) => {
      const restantes = r.pares.filter((p) => p.estado !== 'Auto')
      setResultado({
        ...r,
        pares: restantes,
        auto: 0,
        sugeridos: restantes.filter((p) => p.estado === 'Sugerido').length,
        ambiguos: restantes.filter((p) => p.estado === 'Ambiguo').length,
        sinMatch: restantes.filter((p) => p.estado === 'SinMatch').length,
      })
      setResultFilter('Todos')
      void listQuery.refetch()
      message.success(
        `Plin auto: ${r.verificadosAplicados} verificados (ya OK: ${r.yaVerificados})`,
      )
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const analyzing = previewYape.isPending || previewPlin.isPending
  const applying = applyYape.isPending || applyPlin.isPending

  const columns: ColumnsType<PagosNoVerificadosRow> = useMemo(
    () => [
      { title: 'Mov. ID', dataIndex: 'movimientoCajaId', width: 88 },
      { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
      { title: 'Movimiento', dataIndex: 'movimiento', ellipsis: true },
      {
        title: 'Pago',
        dataIndex: 'importePago',
        align: 'right',
        render: (v: number) => (
          <span className="caja-verificar-pagos-table__importe">{formatMoney(v)}</span>
        ),
      },
      {
        title: 'Tipo',
        dataIndex: 'tipoPago',
        render: (v: string | null) => <TipoPagoCell tipoPago={v} />,
      },
      { title: 'F. transferencia', dataIndex: 'fechaTransferencia', width: 140 },
      { title: 'Registro', dataIndex: 'registro', ellipsis: true },
      {
        title: 'Acciones',
        key: 'acciones',
        width: 120,
        render: (_, row) =>
          puedeConciliar ? (
            <div className="caja-verificar-pagos-actions">
              <Button
                type="primary"
                size="small"
                icon={<CheckOutlined />}
                loading={verificar.isPending && selectedId === row.movimientoCajaId}
                onClick={(e) => {
                  e.stopPropagation()
                  setSelectedId(row.movimientoCajaId)
                  pedirVerificar(row)
                }}
              >
                Verificar
              </Button>
            </div>
          ) : (
            <Tag>Solo lectura</Tag>
          ),
      },
    ],
    [pedirVerificar, puedeConciliar, selectedId, verificar.isPending],
  )

  const resultColumns: ColumnsType<MedioDigitalMatchPair> = useMemo(
    () => [
      {
        title: 'Estado',
        dataIndex: 'estado',
        width: 100,
        render: (v: string) => estadoTag(v),
      },
      { title: 'Cliente (sistema)', dataIndex: 'cliente', ellipsis: true },
      {
        title: 'Importe',
        dataIndex: 'importePago',
        align: 'right',
        width: 100,
        render: (v: number) => (
          <span className="caja-verificar-pagos-table__importe">{formatMoney(v)}</span>
        ),
      },
      { title: 'F. tipada', dataIndex: 'fechaTransferencia', width: 130 },
      {
        title: 'Origen reporte',
        key: 'origen',
        ellipsis: true,
        render: (_, row) =>
          row.reporteOrigen ? (
            <span>
              {row.reporteCanal ? `${row.reporteCanal}-` : ''}
              {row.reporteOrigen}
              {row.reporteMensaje ? (
                <span className="caja-verificar-medios__msg"> · {row.reporteMensaje}</span>
              ) : null}
            </span>
          ) : (
            '—'
          ),
      },
      { title: 'F. reporte', dataIndex: 'reporteFechaOperacion', width: 150 },
      { title: 'Motivo', dataIndex: 'motivo', ellipsis: true },
      {
        title: 'Acción',
        key: 'accion',
        width: 110,
        fixed: 'right',
        render: (_, row) =>
          row.estado === 'Auto' ? (
            <Tag color="success">Listo auto</Tag>
          ) : (
            <Button
              type="link"
              size="small"
              icon={<CheckOutlined />}
              disabled={!puedeConciliar}
              loading={verificar.isPending && selectedId === row.movimientoCajaId}
              onClick={() => {
                setSelectedId(row.movimientoCajaId)
                pedirVerificarPar(row)
              }}
            >
              Manual
            </Button>
          ),
      },
    ],
    [pedirVerificarPar, puedeConciliar, selectedId, verificar.isPending],
  )

  const paresFiltrados = useMemo(() => {
    if (!resultado) return []
    if (resultFilter === 'Todos') return resultado.pares
    return resultado.pares.filter((p) => p.estado === resultFilter)
  }, [resultado, resultFilter])

  const stats: CredixStatItem[] = useMemo(
    () => [
      { label: 'Pendientes', value: String(pendientes.length), tone: 'default' },
      {
        label: buscarDebounced ? 'Coinciden filtro' : 'En vista',
        value: String(filtrados.length),
        tone: 'default',
      },
      {
        label: 'Importe vista',
        value: `S/ ${formatMoney(totalImporte)}`,
        tone: 'red',
      },
    ],
    [pendientes.length, filtrados.length, buscarDebounced, totalImporte],
  )

  const onTableChange = (pagination: TablePaginationConfig) => {
    setPage(pagination.current ?? 1)
    setPageSize(pagination.pageSize ?? 45)
  }

  const buscando = buscarDebounced.length > 0
  const selected = filtrados.find((r) => r.movimientoCajaId === selectedId) ?? null

  const cerrarConciliar = () => {
    setConciliarOpen(false)
    setYapeFile(null)
    setPlinFiles([])
    setResultado(null)
    setResultFilter('Todos')
  }

  const canAnalyze =
    tab === 'yape' ? Boolean(yapeFile) : plinFileList.length > 0
  const canApply =
    Boolean(resultado) &&
    (resultado?.auto ?? 0) > 0 &&
    (tab === 'yape' ? Boolean(yapeFile) : plinFileList.length > 0) &&
    resultado?.fuente.toLowerCase() === tab

  return (
    <CredixCrudPage
      className="caja-verificar-pagos-page credix-page--stats-3"
      title="Verificar pagos por transferencia"
      subtitle={undefined}
      panelTitle="VERIFICADOR DE PAGOS"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Verificar pagos' },
      ]}
      stats={stats}
      toolbar={
        <CajaListToolbar
          className="credix-list-toolbar"
          value={buscar}
          onChange={(v) => {
            setBuscar(v)
            setPage(1)
          }}
          placeholder="Cliente, movimiento, tipo, nº"
          hint="Doble clic verifica. Conciliar Yape (Excel) o Plin (capturas)."
          hintShort="Doble clic = verificar."
          filteredCount={filtrados.length}
          totalCount={pendientes.length}
          loading={listQuery.isFetching}
          onRefresh={() => void listQuery.refetch()}
          extra={
            <Space wrap size="small" className="caja-verificar-pagos-toolbar__actions">
              {buscando ? <Tag color="blue">Filtrado</Tag> : null}
              {puedeConciliar ? (
                <Button
                  type="primary"
                  icon={<CloudUploadOutlined />}
                  onClick={() => setConciliarOpen(true)}
                >
                  Conciliar medios
                </Button>
              ) : null}
              <Button
                icon={<FileExcelOutlined />}
                loading={csv.isPending}
                onClick={() => csv.mutate()}
              >
                CSV
              </Button>
              <Button
                icon={<FilePdfOutlined />}
                loading={pdf.isPending}
                onClick={() => pdf.mutate()}
              >
                PDF
              </Button>
            </Space>
          }
        />
      }
    >
      {oficinaId < 1 ? (
        <Alert type="warning" message="Sesión sin oficina activa." showIcon />
      ) : null}

      {listQuery.isError ? (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 12 }}
          message={errMsg(listQuery.error)}
        />
      ) : null}

      <CredixQueue
        emptyDetail="Seleccione un pago"
        detail={
          selected ? (
            <div>
              <p style={{ margin: '0 0 8px', fontWeight: 700 }}>
                {selected.cliente ?? selected.movimiento}
              </p>
              <p style={{ margin: '0 0 12px', fontSize: 22, fontWeight: 700 }}>
                S/ {formatMoney(selected.importePago)}
              </p>
              {puedeConciliar ? (
                <Button type="primary" onClick={() => pedirVerificar(selected)}>
                  Verificar
                </Button>
              ) : (
                <Tag>Solo Encargado/Admin verifica</Tag>
              )}
            </div>
          ) : undefined
        }
        list={(
          <CredixDataTable<PagosNoVerificadosRow>
            mode="operacion"
            className="caja-verificar-pagos-table"
            rowKey="movimientoCajaId"
            columns={columns}
            dataSource={filtrados}
            loading={listQuery.isLoading}
            scroll={{ x: 'max-content' }}
            pagination={{
              current: page,
              pageSize,
              total: filtrados.length,
              showSizeChanger: true,
              pageSizeOptions: [...PAGE_SIZES],
              showTotal: (t) => `${t} pago(s) pendiente(s) de verificar`,
            }}
            onChange={onTableChange}
            onRow={(row) => ({
              className:
                selectedId === row.movimientoCajaId
                  ? 'caja-verificar-pagos-row--selected'
                  : undefined,
              onClick: () => setSelectedId(row.movimientoCajaId),
          onDoubleClick: () => {
            setSelectedId(row.movimientoCajaId)
            if (puedeConciliar) pedirVerificar(row)
          },
            })}
            locale={{
              emptyText: listQuery.isLoading ? (
                'Cargando pagos…'
              ) : (
                <VerificarPagosTableEmpty buscando={buscando} />
              ),
            }}
          />
        )}
      />

      <Modal
        title="Conciliar medios digitales"
        open={conciliarOpen && puedeConciliar}
        onCancel={cerrarConciliar}
        width={980}
        centered
        rootClassName="caja-modal-root"
        className="caja-verificar-medios-modal"
        destroyOnClose
        footer={[
          <Button key="close" onClick={cerrarConciliar}>
            Cerrar
          </Button>,
          <Button
            key="preview"
            disabled={!canAnalyze}
            loading={analyzing}
            onClick={() => {
              setResultado(null)
              if (tab === 'yape' && yapeFile) previewYape.mutate(yapeFile)
              if (tab === 'plin' && plinFileList.length) previewPlin.mutate(plinFileList)
            }}
          >
            Analizar
          </Button>,
          <Button
            key="apply"
            type="primary"
            disabled={!canApply}
            loading={applying}
            onClick={() => {
              if (!resultado) return
              Modal.confirm({
                title: `Auto-verificar ${resultado.auto} coincidencia(s) única(s)`,
                content:
                  'Solo se marcan pares con monto y fecha/hora (minuto) únicos. Sugeridos y ambiguos quedan manuales.',
                okText: 'Sí, verificar únicos',
                cancelText: 'Cancelar',
                onOk: () => {
                  if (tab === 'yape' && yapeFile) return applyYape.mutateAsync(yapeFile)
                  if (tab === 'plin' && plinFileList.length) {
                    return applyPlin.mutateAsync(plinFileList)
                  }
                  return Promise.resolve()
                },
              })
            }}
          >
            Verificar únicos{resultado && resultado.auto > 0 ? ` (${resultado.auto})` : ''}
          </Button>,
        ]}
      >
        <Tabs
          activeKey={tab}
          onChange={(k) => {
            setTab(k as 'yape' | 'plin')
            setResultado(null)
            setResultFilter('Todos')
          }}
          items={[
            {
              key: 'yape',
              label: 'Yape (Excel)',
              children: (
                <>
                  <Alert
                    type="info"
                    showIcon
                    style={{ marginBottom: 12 }}
                    message="Reporte Yape .xlsx / .csv"
                    description="Se filtra solo TE PAGÓ (se ignora PAGASTE). Auto exige monto + minuto únicos. Familiar que paga no bloquea el auto si el match es único."
                  />
                  <Upload
                    accept=".xlsx,.xlsm,.csv,.txt"
                    maxCount={1}
                    beforeUpload={(file) => {
                      setYapeFile(file)
                      setResultado(null)
                      setResultFilter('Todos')
                      return false
                    }}
                    onRemove={() => {
                      setYapeFile(null)
                      setResultado(null)
                    }}
                    fileList={
                      yapeFile
                        ? [{ uid: 'yape', name: yapeFile.name, status: 'done' }]
                        : []
                    }
                  >
                    <Button icon={<FileExcelOutlined />}>Elegir Excel Yape</Button>
                  </Upload>
                </>
              ),
            },
            {
              key: 'plin',
              label: 'Plin (capturas)',
              children: (
                <>
                  <Alert
                    type="info"
                    showIcon
                    style={{ marginBottom: 12 }}
                    message="Capturas Interbank / Plin en bloque"
                    description="Hasta 30 fotos. OCR extrae Plin-nombre; Yape- de la misma pantalla se ignora. Dedupe si hay scroll solapado."
                  />
                  <Upload
                    accept="image/*,.jpg,.jpeg,.png,.webp"
                    multiple
                    listType="picture"
                    fileList={plinFiles}
                    beforeUpload={(file) => {
                      setPlinFiles((prev) => {
                        if (prev.length >= 30) {
                          message.warning('Máximo 30 capturas por lote.')
                          return prev
                        }
                        return [
                          ...prev,
                          {
                            uid: `${file.name}-${file.size}-${Date.now()}`,
                            name: file.name,
                            status: 'done',
                            originFileObj: file as UploadFile['originFileObj'],
                          },
                        ]
                      })
                      setResultado(null)
                      setResultFilter('Todos')
                      return false
                    }}
                    onRemove={(file) => {
                      setPlinFiles((prev) => prev.filter((f) => f.uid !== file.uid))
                      setResultado(null)
                    }}
                  >
                    <Button icon={<PictureOutlined />}>Agregar capturas</Button>
                  </Upload>
                </>
              ),
            },
          ]}
        />

        {resultado ? (
          <div className="caja-verificar-medios__result">
            <Space wrap size={[8, 8]} style={{ margin: '12px 0' }}>
              <Tag color="blue">{resultado.fuente}</Tag>
              <Tag>Detectadas: {resultado.filasArchivo}</Tag>
              <Tag color="blue">Ingresos: {resultado.filasIngreso}</Tag>
              <Tag>Descartadas: {resultado.filasDescartadas}</Tag>
              <Tag color="success">Auto: {resultado.auto}</Tag>
              <Tag color="processing">Sugeridos: {resultado.sugeridos}</Tag>
              <Tag color="warning">Ambiguos: {resultado.ambiguos}</Tag>
              <Tag>Sin match: {resultado.sinMatch}</Tag>
              <Tag>Total: S/ {formatMoney(resultado.totalIngresos)}</Tag>
            </Space>

            {resultado.advertencias?.length ? (
              <Alert
                type="warning"
                showIcon
                style={{ marginBottom: 12 }}
                message="Avisos del análisis"
                description={
                  <ul className="caja-verificar-medios__warns">
                    {resultado.advertencias.slice(0, 10).map((a) => (
                      <li key={a}>{a}</li>
                    ))}
                  </ul>
                }
              />
            ) : null}

            <div className="caja-verificar-medios__filters">
              <Segmented<ResultFilter>
                value={resultFilter}
                onChange={(v) => setResultFilter(v)}
                options={[
                  { label: `Todos (${resultado.pares.length})`, value: 'Todos' },
                  { label: `Auto (${resultado.auto})`, value: 'Auto' },
                  { label: `Sugeridos (${resultado.sugeridos})`, value: 'Sugerido' },
                  { label: `Ambiguos (${resultado.ambiguos})`, value: 'Ambiguo' },
                  { label: `Sin match (${resultado.sinMatch})`, value: 'SinMatch' },
                ]}
              />
            </div>

            <Table<MedioDigitalMatchPair>
              size="small"
              rowKey={(r) =>
                `${r.movimientoCajaId}-${r.estado}-${r.reporteIndice ?? 'x'}`
              }
              columns={resultColumns}
              dataSource={paresFiltrados}
              scroll={{ x: 980, y: 360 }}
              pagination={{ pageSize: 20, showSizeChanger: false }}
              locale={{ emptyText: 'Sin filas en este filtro.' }}
            />
          </div>
        ) : null}
      </Modal>
    </CredixCrudPage>
  )
}
