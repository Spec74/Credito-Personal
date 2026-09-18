import { useCallback, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Modal,
  Space,
  Typography,
  message,
} from 'antd'
import {
  CheckOutlined,
  ExclamationCircleOutlined,
  FileExcelOutlined,
  FilePdfOutlined,
} from '@ant-design/icons'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import {
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
  type CredixStatItem,
} from '../../components/credix'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import type { PagosNoVerificadosRow } from '../../types/api'
import { filterTableRows } from '../../utils/tableClientFilter'
import { formatMoney } from '../../utils/formatMoney'
import { CajaListToolbar } from './components/CajaListToolbar'
import { VerificarPagosTableEmpty } from './components/VerificarPagosTableEmpty'

const { Paragraph } = Typography

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
  const detalle = [
    row.movimientoCajaId,
    row.movimiento,
    row.tipoPago,
    row.fechaTransferencia,
  ]
    .filter(Boolean)
    .join(' — ')

  Modal.confirm({
    title: 'Verificar depósito',
    icon: <ExclamationCircleOutlined />,
    content: (
      <>
        <Paragraph style={{ marginBottom: 8 }}>{detalle}</Paragraph>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          ¿Desea actualizar el pago como verificado? (paridad doble clic legacy)
        </Paragraph>
      </>
    ),
    okText: 'Sí, verificar',
    cancelText: 'No',
    onOk,
  })
}

export function VerificarPagosPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0

  const [buscar, setBuscar] = useState('')
  const buscarDebounced = useDebouncedValue(buscar.trim(), 300)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(45)

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
    () => filtrados.reduce((s, r) => s + (r.importePago ?? 0), 0),
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
      void listQuery.refetch()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const pedirVerificar = useCallback((row: PagosNoVerificadosRow) => {
    confirmarVerificacion(row, () => verificar.mutate(row.movimientoCajaId))
  }, [verificar])

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

  const columns: ColumnsType<PagosNoVerificadosRow> = useMemo(
    () => [
      { title: 'Mov. ID', dataIndex: 'movimientoCajaId', width: 88, sorter: (a, b) => a.movimientoCajaId - b.movimientoCajaId },
      {
        title: 'Cliente',
        dataIndex: 'cliente',
        ellipsis: true,
        minWidth: 140,
        sorter: (a, b) => (a.cliente ?? '').localeCompare(b.cliente ?? ''),
      },
      {
        title: 'Movimiento',
        dataIndex: 'movimiento',
        ellipsis: true,
        minWidth: 120,
      },
      {
        title: 'Pago',
        dataIndex: 'importePago',
        align: 'right',
        width: 110,
        sorter: (a, b) => a.importePago - b.importePago,
        render: (v: number) => (
          <span className="caja-verificar-pagos-table__importe">{formatMoney(v)}</span>
        ),
      },
      { title: 'Tipo', dataIndex: 'tipoPago', width: 100 },
      {
        title: 'F. transferencia',
        dataIndex: 'fechaTransferencia',
        width: 118,
      },
      {
        title: 'Registro',
        dataIndex: 'registro',
        width: 140,
        ellipsis: true,
      },
      {
        title: 'Acciones',
        key: 'acciones',
        width: 120,
        render: (_, row) => (
          <div className="caja-verificar-pagos-actions">
            <Button
              type="primary"
              size="small"
              block
              icon={<CheckOutlined />}
              title="Marcar como verificado"
              loading={
                verificar.isPending &&
                verificar.variables === row.movimientoCajaId
              }
              disabled={verificar.isPending}
              onClick={(e) => {
                e.stopPropagation()
                pedirVerificar(row)
              }}
            >
              Verificar
            </Button>
          </div>
        ),
      },
    ],
    [pedirVerificar, verificar.isPending, verificar.variables],
  )

  const stats: CredixStatItem[] = useMemo(() => {
    const n = pendientes.length
    const nf = filtrados.length
    return [
      {
        value: n,
        label: 'Pendientes (total)',
        tone: n > 0 ? 'green' : 'default',
      },
      {
        value: buscarDebounced ? nf : n,
        label: buscarDebounced ? 'Coinciden filtro' : 'En pantalla',
      },
      {
        value: formatMoney(totalImporte),
        label: 'Importe (vista)',
        tone: 'red',
      },
    ]
  }, [pendientes.length, filtrados.length, buscarDebounced, totalImporte])

  const onTableChange = (pagination: TablePaginationConfig) => {
    setPage(pagination.current ?? 1)
    setPageSize(pagination.pageSize ?? 45)
  }

  const buscando = buscarDebounced.length > 0

  return (
    <CredixCrudPage
      className="caja-verificar-pagos-page credix-page--stats-3"
      title="Verificar pagos por transferencia"
      subtitle="Confirme depósitos en cuentas — misma grilla que VerificarPagos del MVC."
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
          hint="Filtro instantáneo. Doble clic en fila = verificar (como legacy)."
          hintShort="Doble clic en fila = verificar."
          filteredCount={filtrados.length}
          totalCount={pendientes.length}
          loading={listQuery.isFetching}
          onRefresh={() => void listQuery.refetch()}
          extra={
            <Space wrap size="small">
              {buscando ? <Tag color="blue">Filtrado</Tag> : null}
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
      <p className="credix-module-banner credix-module-banner--spaced">
        <strong>Paridad MVC:</strong> columnas MOV / CLIENTE / MOVIMIENTO / PAGO / TIPO / FECHA /
        REGISTRO · doble clic confirma verificación · exportaciones CSV/PDF añadidas en modern.
      </p>

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
            pedirVerificar(row)
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
    </CredixCrudPage>
  )
}
