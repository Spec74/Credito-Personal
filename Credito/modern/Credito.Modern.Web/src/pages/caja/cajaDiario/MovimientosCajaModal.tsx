import { useMemo, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Button, Space } from 'antd'
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
import { formatMoney } from '../../../utils/formatMoney'
import { formatFechaHora } from '../../../utils/formatFecha'
import { cajaToastError } from './cajaFeedback'
import { errMsg } from './types'

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
    return all.filter(
      (r) =>
        (r.cliente ?? '').toLowerCase().includes(q) ||
        (r.codigo ?? '').toLowerCase().includes(q) ||
        String(r.movimientoCajaId).includes(q),
    )
  }, [query.data, filtroCliente])

  const columns: ColumnsType<RptSaldosCajaRow> = [
    { title: 'Nro', dataIndex: 'movimientoCajaId', width: 72 },
    { title: 'Op', dataIndex: 'operacion', width: 56 },
    {
      title: 'Fecha',
      dataIndex: 'fechaReg',
      width: 140,
      render: (v: string) => formatFechaHora(v),
    },
    { title: 'Descripción', dataIndex: 'glosa', ellipsis: true },
    {
      title: 'Importe',
      dataIndex: 'importePago',
      align: 'right',
      render: (v: number, r) => (
        <span className={r.indEntrada ? 'caja-monto-entrada' : 'caja-monto-salida'}>
          {formatMoney(v)}
        </span>
      ),
    },
    {
      title: '',
      width: 88,
      render: (_, r) => (
        <Button
          size="small"
          type="link"
          icon={<FilePdfOutlined />}
          loading={ticket.isPending && ticket.variables === r.movimientoCajaId}
          onClick={() => ticket.mutate(r.movimientoCajaId)}
          aria-label="Imprimir ticket"
        />
      ),
    },
  ]

  return (
    <CajaModal
      title="Movimientos de caja"
      open={open}
      onCancel={onClose}
      footer={null}
      width={820}
    >
      <Space wrap style={{ marginBottom: 12 }}>
        <ClienteBuscarAutoComplete
          value={clienteLabel}
          onChange={setClienteLabel}
          onSelectPersona={(_pid, label) => {
            setClienteLabel(label)
            setFiltroCliente(label)
          }}
          placeholder="Buscar cliente para filtrar movimientos"
          minWidth={280}
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
        rowKey="movimientoCajaId"
        columns={columns}
        dataSource={rows}
        loading={query.isLoading}
        pagination={{ defaultPageSize: 20 }}
        size="small"
        scroll={{ x: 700 }}
      />
    </CajaModal>
  )
}
