import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Button } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchCuentasPorCobrarPendientes,
  pagarCuentaPorCobrar,
} from '../../../api/cajaDiario'
import { ClienteBuscarAutoComplete } from '../../../components/caja/ClienteBuscarAutoComplete'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import { CredixDataTable } from '../../../components/credix'
import type { CuentaPorCobrarPendienteRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { maybeDownloadCajaTicket } from './cajaPagoHelpers'
import { cajaToastError, cajaToastSuccess } from './cajaFeedback'
import type { CajaSession } from './types'
import { errMsg } from './types'

export function CxcTab({
  ctx,
  active,
  onChanged,
}: {
  ctx: CajaSession
  active: boolean
  onChanged: () => void
}) {
  const [personaId, setPersonaId] = useState(0)
  const [clienteLabel, setClienteLabel] = useState('')

  const query = useQuery({
    queryKey: ['caja-cxc', ctx.cajaDiarioId, personaId],
    queryFn: () =>
      fetchCuentasPorCobrarPendientes(
        ctx.oficinaId,
        ctx.cajaDiarioId,
        personaId,
      ),
    enabled: active,
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
      cajaToastSuccess('CxC cobrada', 'caja-cxc')
      await maybeDownloadCajaTicket(ctx.oficinaId, r.resultId)
      onChanged()
      void query.refetch()
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const columns: ColumnsType<CuentaPorCobrarPendienteRow> = [
    { title: 'Orden', dataIndex: 'ordenVentaId', width: 80 },
    { title: 'CxC', dataIndex: 'cuentaxCobrarId', width: 70 },
    { title: 'Código', dataIndex: 'personaCodigo', width: 90 },
    { title: 'Cliente', dataIndex: 'personaNombre', ellipsis: true },
    { title: 'Operación', dataIndex: 'operacion' },
    {
      title: 'Monto',
      dataIndex: 'monto',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 90 },
    {
      title: '',
      width: 90,
      render: (_, row) => (
        <Button
          size="small"
          type="primary"
          loading={pagar.isPending}
          onClick={() => {
            cajaConfirm({
              title: '¿Cobrar cuenta por cobrar?',
              content: `Registrar cobro de ${formatMoney(row.monto)}.`,
              onOk: () => pagar.mutateAsync(row),
            })
          }}
        >
          Cobrar
        </Button>
      ),
    },
  ]

  return (
    <>
      <div className="caja-diario-toolbar">
        <ClienteBuscarAutoComplete
          value={clienteLabel}
          onChange={setClienteLabel}
          onSelectPersona={(pid, label) => {
            setPersonaId(pid)
            setClienteLabel(label)
          }}
          placeholder="Filtrar por cliente (vacío = todos)"
        />
        <Button
          onClick={() => {
            setPersonaId(0)
            setClienteLabel('')
          }}
        >
          Ver todos
        </Button>
      </div>
      <CredixDataTable<CuentaPorCobrarPendienteRow>
        rowKey={(r) => `${r.ordenVentaId}-${r.cuentaxCobrarId}`}
        columns={columns}
        dataSource={query.data ?? []}
        loading={query.isLoading}
        pagination={{ defaultPageSize: 20 }}
        size="small"
      />
    </>
  )
}
