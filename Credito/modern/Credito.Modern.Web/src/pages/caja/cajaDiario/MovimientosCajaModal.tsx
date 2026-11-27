import { useMemo, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Button, Space, Typography } from 'antd'
import { FilePdfOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadMovimientoCajaTicketPdf,
  fetchRptSaldosCaja,
} from '../../../api/cajaDiario'
import { ClienteBuscarAutoComplete } from '../../../components/caja/ClienteBuscarAutoComplete'
import { CajaModal } from '../../../components/caja/CajaModal'
import { CredixDataTable } from '../../../components/credix'
import type { RptSaldosCajaRow } from '../../../types/api'
import { TipoPagoCell } from '../../tesoreria/components/TipoPagoBrand'
import { formatMoney } from '../../../utils/formatMoney'
import { formatFechaHora } from '../../../utils/formatFecha'
import { cajaToastError } from './cajaFeedback'
import { errMsg } from './types'

const { Text } = Typography

function creditoDesdeGlosa(glosa: string | null | undefined): string | null {
  if (!glosa) return null
  const m = glosa.match(/CREDITO\s+(\d+)/i)
  return m?.[1] ? `#${m[1]}` : null
}

export function MovimientosCajaModal({
  open,
  oficinaId,
  cajaDiarioId,
  onClose,
}: {
  open: boolean
  oficinaId: number
  cajaDiarioId: number
  onClose: () => void
}) {
  const [clienteLabel, setClienteLabel] = useState('')
  const [filtroCliente, setFiltroCliente] = useState('')

  const query = useQuery({
    queryKey: ['caja-movimientos-modal', cajaDiarioId],
    queryFn: () => fetchRptSaldosCaja(cajaDiarioId),
    enabled: open,
  })

  const ticket = useMutation({
    mutationFn: (movimientoCajaId: number) =>
      downloadMovimientoCajaTicketPdf(oficinaId, movimientoCajaId),
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const rows = useMemo(() => {
    const all = query.data ?? []
    const q = filtroCliente.trim().toLowerCase()
    if (!q) {
      return all
    }
    return all.filter((r) => {
      const cred = creditoDesdeGlosa(r.glosa) ?? ''
      return (
        (r.cliente ?? '').toLowerCase().includes(q) ||
        (r.codigo ?? '').toLowerCase().includes(q) ||
        (r.glosa ?? '').toLowerCase().includes(q) ||
        (r.tipoPago ?? '').toLowerCase().includes(q) ||
        (r.operacion ?? '').toLowerCase().includes(q) ||
        cred.toLowerCase().includes(q) ||
        String(r.movimientoCajaId).includes(q)
      )
    })
  }, [query.data, filtroCliente])

  const columns: ColumnsType<RptSaldosCajaRow> = [
    { title: 'Nro', dataIndex: 'movimientoCajaId', width: 64 },
    {
      title: 'Fecha',
      dataIndex: 'fechaReg',
      width: 132,
      render: (v: string) => formatFechaHora(v),
    },
    {
      title: 'Cliente',
      key: 'cliente',
      width: 220,
      ellipsis: true,
      render: (_, r) => {
        const nombre = (r.cliente ?? '').trim()
        const codigo = (r.codigo ?? '').trim()
        if (!nombre && !codigo) {
          return <Text type="secondary">—</Text>
        }
        return (
          <div className="caja-movimientos-modal__cliente">
            <strong>{nombre || 'Sin nombre'}</strong>
            {codigo ? (
              <span className="caja-movimientos-modal__meta">[{codigo}]</span>
            ) : null}
          </div>
        )
      },
    },
    {
      title: 'Crédito',
      key: 'credito',
      width: 80,
      render: (_, r) => creditoDesdeGlosa(r.glosa) ?? '—',
    },
    {
      title: 'Op',
      dataIndex: 'operacion',
      width: 56,
    },
    {
      title: 'Tipo pago',
      dataIndex: 'tipoPago',
      width: 150,
      render: (v: string) => <TipoPagoCell tipoPago={v} />,
    },
    {
      title: 'Importe',
      dataIndex: 'importePago',
      align: 'right',
      width: 100,
      render: (v: number, r) => (
        <span className={r.indEntrada ? 'caja-monto-entrada' : 'caja-monto-salida'}>
          {r.indEntrada ? '+' : '-'}
          {formatMoney(v)}
        </span>
      ),
    },
    {
      title: 'Descripción',
      dataIndex: 'glosa',
      ellipsis: true,
      render: (v: string | null) => v || '—',
    },
    {
      title: 'Acciones',
      key: 'acciones',
      width: 56,
      fixed: 'right',
      className: 'credix-col-acciones',
      render: (_, r) => (
        <Button
          size="small"
          type="link"
          icon={<FilePdfOutlined />}
          loading={ticket.isPending && ticket.variables === r.movimientoCajaId}
          onClick={() => ticket.mutate(r.movimientoCajaId)}
          aria-label={`Imprimir ticket movimiento ${r.movimientoCajaId}`}
          title="Imprimir ticket"
        />
      ),
    },
  ]

  return (
    <CajaModal
      title="Movimientos de caja"
      open={open}
      onCancel={() => {
        setClienteLabel('')
        setFiltroCliente('')
        onClose()
      }}
      footer={null}
      width="min(1100px, 96vw)"
    >
      <Space wrap style={{ marginBottom: 12 }}>
        <ClienteBuscarAutoComplete
          value={clienteLabel}
          onChange={setClienteLabel}
          onSelectPersona={(_pid, label) => {
            setClienteLabel(label)
            setFiltroCliente(label)
          }}
          placeholder="Buscar cliente, código, crédito u operación…"
          minWidth={300}
        />
        <Button
          onClick={() => {
            setClienteLabel('')
            setFiltroCliente('')
          }}
        >
          Ver todos
        </Button>
      </Space>
      <CredixDataTable<RptSaldosCajaRow>
        mode="operacion"
        rowKey="movimientoCajaId"
        columns={columns}
        dataSource={rows}
        loading={query.isLoading}
        pagination={{
          defaultPageSize: 20,
          showTotal: (t) => `${t} registro(s)`,
        }}
        size="small"
        scroll={{ x: 980 }}
        locale={{
          emptyText: filtroCliente.trim()
            ? 'Sin movimientos para ese filtro'
            : 'Sin movimientos en esta caja',
        }}
      />
    </CajaModal>
  )
}
