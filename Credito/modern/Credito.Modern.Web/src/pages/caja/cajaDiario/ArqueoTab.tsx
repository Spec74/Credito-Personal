import { useMemo, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import {
  Button,
  Input,
  Segmented,
  Space,
  Tag,
  Typography,
} from 'antd'
import {
  FileExcelOutlined,
  FilePdfOutlined,
  ReloadOutlined,
  SwapOutlined,
} from '@ant-design/icons'
import {
  cajaToastError,
  cajaToastSuccess,
} from './cajaFeedback'
import type { ColumnsType } from 'antd/es/table'
import {
  anularMovimientoCaja,
  downloadMovimientoCajaTicketPdf,
  downloadRptSaldosCajaCsv,
  downloadRptSaldosCajaPdf,
  reconciliarCajaDiario,
  validarAnularMovimientoCaja,
} from '../../../api/cajaDiario'
import { CredixDataTable } from '../../../components/credix'
import { CajaModal } from '../../../components/caja/CajaModal'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import type { RptSaldosCajaRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { formatFechaHora } from '../../../utils/formatFecha'
import { TransferirSaldosDrawer } from './TransferirSaldosDrawer'
import { MovimientoDetalleModal } from './MovimientoDetalleModal'
import type { CajaSession } from './types'
import { errMsg } from './types'
import { useAuth } from '../../../auth/useAuth'
import { puedeAnularMovimientoCaja } from '../../../utils/cajaSaldosPermisos'

const { Paragraph, Text } = Typography

function filtrarMovimientos(rows: RptSaldosCajaRow[], q: string): RptSaldosCajaRow[] {
  const norm = q.trim().toLowerCase()
  if (!norm) {
    return rows
  }
  return rows.filter((r) => {
    const blob = [
      r.movimientoCajaId,
      r.cliente,
      r.glosa,
      r.operacion,
      r.tipoPago,
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
    return blob.includes(norm)
  })
}

export function ArqueoTab({
  ctx,
  entradas,
  salidas,
  loading,
  onRefresh,
  onChanged,
  onGoCierre,
}: {
  ctx: CajaSession
  entradas: RptSaldosCajaRow[]
  salidas: RptSaldosCajaRow[]
  loading: boolean
  onRefresh: () => void
  onChanged: () => void
  onGoCierre: () => void
}) {
  const { session } = useAuth()
  const puedeAnularMovimiento = puedeAnularMovimientoCaja(session?.roles ?? [])
  const [filtro, setFiltro] = useState('')
  const [vista, setVista] = useState<'entradas' | 'salidas' | 'ambos'>('ambos')
  const [anularId, setAnularId] = useState<number | null>(null)
  const [observacion, setObservacion] = useState('')
  const [transferirOpen, setTransferirOpen] = useState(false)
  const [detalleMov, setDetalleMov] = useState<RptSaldosCajaRow | null>(null)

  const entradasF = useMemo(
    () => filtrarMovimientos(entradas, filtro),
    [entradas, filtro],
  )
  const salidasF = useMemo(
    () => filtrarMovimientos(salidas, filtro),
    [salidas, filtro],
  )

  const conciliar = useMutation({
    mutationFn: () =>
      reconciliarCajaDiario({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
      }),
    onSuccess: () => {
      cajaToastSuccess('Caja conciliada', 'caja-conciliar')
      onChanged()
      onRefresh()
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const csv = useMutation({
    mutationFn: () => downloadRptSaldosCajaCsv(ctx.cajaDiarioId),
    onError: (e) => cajaToastError(errMsg(e)),
  })
  const pdf = useMutation({
    mutationFn: () => downloadRptSaldosCajaPdf(ctx.cajaDiarioId),
    onError: (e) => cajaToastError(errMsg(e)),
  })
  const ticket = useMutation({
    mutationFn: (movimientoCajaId: number) =>
      downloadMovimientoCajaTicketPdf(ctx.oficinaId, movimientoCajaId),
    onError: (e) => cajaToastError(errMsg(e)),
  })
  const anular = useMutation({
    mutationFn: async () => {
      if (!observacion.trim()) {
        throw new Error('Ingrese la observación para anular.')
      }
      const validacion = await validarAnularMovimientoCaja(
        ctx.oficinaId,
        anularId!,
      )
      if (validacion.bloqueadoPorPagosCuota) {
        throw new Error(
          'Tiene Pagos de cuotas, No se Puede Anular el Crédito',
        )
      }
      return anularMovimientoCaja({
        oficinaId: ctx.oficinaId,
        movimientoCajaId: anularId!,
        observacion: observacion.trim(),
      })
    },
    onSuccess: () => {
      cajaToastSuccess('Movimiento anulado', 'caja-anular')
      setAnularId(null)
      setObservacion('')
      onChanged()
      onRefresh()
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const columns: ColumnsType<RptSaldosCajaRow> = [
    { title: 'Nro', dataIndex: 'movimientoCajaId', width: 72 },
    {
      title: 'Fecha',
      dataIndex: 'fechaReg',
      width: 140,
      render: (v: string) => formatFechaHora(v),
    },
    {
      title: 'Operación',
      dataIndex: 'operacion',
      ellipsis: true,
      render: (v: string, r) => (
        <Space size={4}>
          <span>{v}</span>
          {r.estadoActivo === false ? <Tag>ANULADO</Tag> : null}
        </Space>
      ),
    },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    {
      title: 'Importe',
      dataIndex: 'importePago',
      align: 'right',
      render: (v: number, r) => (
        <Text
          type={r.estadoActivo === false ? 'secondary' : r.indEntrada ? 'success' : 'danger'}
          delete={r.estadoActivo === false}
        >
          {r.indEntrada ? '+' : '-'}
          {formatMoney(v)}
        </Text>
      ),
    },
    { title: 'Tipo pago', dataIndex: 'tipoPago', width: 100 },
    { title: 'Glosa', dataIndex: 'glosa', ellipsis: true },
    {
      title: '',
      width: 150,
      fixed: 'right',
      render: (_, r) => (
        <Space size="small">
          <Button
            size="small"
            icon={<FilePdfOutlined />}
            loading={ticket.isPending && ticket.variables === r.movimientoCajaId}
            onClick={() => ticket.mutate(r.movimientoCajaId)}
          >
            Ticket
          </Button>
          {puedeAnularMovimiento && r.estadoActivo !== false ? (
            <Button
              size="small"
              danger
              onClick={() => setAnularId(r.movimientoCajaId)}
            >
              Anular
            </Button>
          ) : null}
        </Space>
      ),
    },
  ]

  const tablePagination = {
    defaultPageSize: 20,
    showSizeChanger: true,
    pageSizeOptions: ['10', '20', '50', '100'],
  }

  const showEntradas = vista === 'entradas' || vista === 'ambos'
  const showSalidas = vista === 'salidas' || vista === 'ambos'

  return (
    <div className="caja-diario-arqueo">
      <div className="caja-diario-arqueo-toolbar">
        <Input.Search
          className="caja-diario-arqueo-search"
          allowClear
          placeholder="Filtrar por cliente, glosa, operación o nro…"
          value={filtro}
          onChange={(e) => setFiltro(e.target.value)}
        />
        <Segmented
          className="caja-diario-arqueo-segmented"
          value={vista}
          onChange={(v) => setVista(v as typeof vista)}
          options={[
            { label: `Entradas (${entradasF.length})`, value: 'entradas' },
            { label: `Salidas (${salidasF.length})`, value: 'salidas' },
            { label: 'Ambas', value: 'ambos' },
          ]}
        />
        <Button icon={<ReloadOutlined />} onClick={onRefresh} loading={loading}>
          Actualizar
        </Button>
        <Button
          icon={<FileExcelOutlined />}
          loading={csv.isPending}
          onClick={() => csv.mutate()}
        >
          CSV
        </Button>
        {filtro.trim() ? (
          <Text type="secondary">
            {entradasF.length + salidasF.length} de{' '}
            {entradas.length + salidas.length} mov.
          </Text>
        ) : null}
      </div>

      <div
        className={[
          'caja-diario-arqueo-split',
          vista === 'ambos' ? 'is-ambas' : 'is-single',
        ].join(' ')}
      >
        {showEntradas ? (
          <section className="caja-diario-arqueo-pane is-entrada">
            <header className="caja-diario-arqueo-pane__head">
              <h3>Entradas</h3>
              <Tag color="success">{entradasF.length}</Tag>
            </header>
            <div className="caja-diario-arqueo-pane__table">
              <CredixDataTable<RptSaldosCajaRow>
                rowKey="movimientoCajaId"
                columns={columns}
                dataSource={entradasF}
                loading={loading}
                pagination={tablePagination}
                scroll={{ x: 'max-content' }}
                onRow={(row) => ({
                  onDoubleClick: () => setDetalleMov(row),
                })}
              />
            </div>
          </section>
        ) : null}
        {showSalidas ? (
          <section className="caja-diario-arqueo-pane is-salida">
            <header className="caja-diario-arqueo-pane__head">
              <h3>Salidas</h3>
              <Tag color="error">{salidasF.length}</Tag>
            </header>
            <div className="caja-diario-arqueo-pane__table">
              <CredixDataTable<RptSaldosCajaRow>
                rowKey="movimientoCajaId"
                columns={columns}
                dataSource={salidasF}
                loading={loading}
                pagination={tablePagination}
                scroll={{ x: 'max-content' }}
                onRow={(row) => ({
                  onDoubleClick: () => setDetalleMov(row),
                })}
              />
            </div>
          </section>
        ) : null}
      </div>

      <footer className="caja-diario-arqueo-footer">
        <Button
          icon={<FilePdfOutlined />}
          loading={pdf.isPending}
          onClick={() => pdf.mutate()}
        >
          Previsualizar saldo caja
        </Button>
        <div className="caja-diario-arqueo-footer-actions">
          <Tag color="blue" className="caja-diario-total-caja-badge">
            TOTAL CAJA: S/. {formatMoney(ctx.saldoFinal)}
          </Tag>
          <Button
            icon={<SwapOutlined />}
            onClick={() => setTransferirOpen(true)}
          >
            Transferir saldos
          </Button>
          {ctx.esCajaCentral && (
            <Button
              type="primary"
              loading={conciliar.isPending}
              onClick={() => {
                cajaConfirm({
                  title: 'Conciliar pagos',
                  content: '¿Desea conciliar los pagos de esta caja central?',
                  onOk: () => conciliar.mutateAsync(),
                })
              }}
            >
              Conciliar pagos
            </Button>
          )}
          <Button danger onClick={onGoCierre}>
            Cerrar caja
          </Button>
        </div>
      </footer>

      <TransferirSaldosDrawer
        open={transferirOpen}
        ctx={ctx}
        onClose={() => setTransferirOpen(false)}
        onSuccess={() => {
          onChanged()
          onRefresh()
        }}
      />

      <MovimientoDetalleModal
        movement={detalleMov}
        oficinaId={ctx.oficinaId}
        onClose={() => setDetalleMov(null)}
      />

      <CajaModal
        title={`Anular movimiento ${anularId ?? ''}`}
        open={anularId != null}
        onCancel={() => setAnularId(null)}
        onOk={() => anular.mutate()}
        confirmLoading={anular.isPending}
        okText="Confirmar anulación"
        okButtonProps={{ danger: true }}
      >
        <Paragraph type="secondary">
          Paridad Caja Diario: si el movimiento INI tiene cuotas pagadas, la
          anulación se bloquea. La observación es obligatoria.
        </Paragraph>
        <Input.TextArea
          rows={2}
          placeholder="Observación (obligatoria)"
          value={observacion}
          onChange={(e) => setObservacion(e.target.value)}
        />
      </CajaModal>
    </div>
  )
}
