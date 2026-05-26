import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Button, Table, message } from 'antd'
import { AccountBookOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchCuentasPorCobrarPendientes,
  pagarCuentaPorCobrar,
} from '../../../api/cajaDiario'
import { CajaSection } from '../../../components/caja/CajaSection'
import { ClienteBuscarAutoComplete } from '../../../components/caja/ClienteBuscarAutoComplete'
import { cajaConfirm } from '../../../components/caja/CajaModal'
import type { CuentaPorCobrarPendienteRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { formatFecha } from '../../../utils/formatFecha'
import { maybeDownloadCajaTicket } from './cajaPagoHelpers'
import type { CajaSession } from './types'
import { errMsg } from './types'

/** CxC en pestaña Cobranzas (paridad bloque «GAD pendientes» del MVC). */
export function CxcInlinePanel({
  ctx,
  personaId: personaIdInicial = 0,
  clienteLabel: clienteLabelInicial = '',
  onChanged,
}: {
  ctx: CajaSession
  personaId?: number
  clienteLabel?: string
  onChanged: () => void
}) {
  const [personaId, setPersonaId] = useState(personaIdInicial)
  const [clienteInput, setClienteInput] = useState(clienteLabelInicial)
  const [clienteLabel, setClienteLabel] = useState(clienteLabelInicial)

  const query = useQuery({
    queryKey: ['caja-cxc-inline', ctx.cajaDiarioId, personaId],
    queryFn: () =>
      fetchCuentasPorCobrarPendientes(
        ctx.oficinaId,
        ctx.cajaDiarioId,
        personaId,
      ),
  })

  const pagar = useMutation({
    mutationFn: (row: CuentaPorCobrarPendienteRow) =>
      pagarCuentaPorCobrar({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        ordenVentaId: row.ordenVentaId,
        cuentaxCobrarId: row.cuentaxCobrarId,
      }),
    onSuccess: async (r) => {
      message.success('CxC cobrada')
      await maybeDownloadCajaTicket(ctx.oficinaId, r.resultId)
      onChanged()
      void query.refetch()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const columns: ColumnsType<CuentaPorCobrarPendienteRow> = [
    { title: 'Código', dataIndex: 'personaCodigo', width: 80 },
    { title: 'Cliente', dataIndex: 'personaNombre', ellipsis: true },
    { title: 'Operación', dataIndex: 'operacion', width: 90 },
    { title: 'Origen', dataIndex: 'origen', width: 90 },
    {
      title: 'Monto',
      dataIndex: 'monto',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 72 },
    {
      title: 'Fecha',
      dataIndex: 'fechaReg',
      width: 100,
      render: (v: string) => formatFecha(v),
    },
    {
      title: '',
      width: 100,
      render: (_, row) => (
        <Button
          size="small"
          type="primary"
          loading={pagar.isPending}
          onClick={() => {
            cajaConfirm({
              title: '¿Pagar cuenta por cobrar?',
              content: `Registrar cobro de ${formatMoney(row.monto)} para ${row.personaNombre}.`,
              onOk: () => pagar.mutateAsync(row),
            })
          }}
        >
          Pagar cuenta
        </Button>
      ),
    },
  ]

  return (
    <CajaSection
      tone="cxc"
      kicker="GAD"
      title="Cuentas por cobrar"
      subtitle={clienteLabel || 'Todos los clientes'}
      icon={<AccountBookOutlined />}
    >
      <div className="caja-diario-cxc-toolbar">
        <ClienteBuscarAutoComplete
          value={clienteInput}
          onChange={setClienteInput}
          onSelectPersona={(pid, label) => {
            setPersonaId(pid)
            setClienteInput(label)
            setClienteLabel(label)
          }}
          placeholder="Filtrar por cliente"
          minWidth={260}
        />
        <Button
          onClick={() => {
            setPersonaId(0)
            setClienteInput('')
            setClienteLabel('')
          }}
        >
          Ver todos
        </Button>
      </div>
      <Table<CuentaPorCobrarPendienteRow>
        className="caja-cxc-table credix-table"
        rowKey={(r) => `${r.ordenVentaId}-${r.cuentaxCobrarId}`}
        columns={columns}
        dataSource={query.data ?? []}
        loading={query.isLoading}
        size="small"
        bordered
        pagination={{ pageSize: 10, showSizeChanger: true }}
        scroll={{ x: 860 }}
      />
    </CajaSection>
  )
}
