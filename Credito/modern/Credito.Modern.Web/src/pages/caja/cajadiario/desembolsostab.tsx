import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Button, message } from 'antd'
import { BankOutlined, ReloadOutlined } from '@ant-design/icons'
import { CajaSection } from '../../../components/caja/CajaSection'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchDesembolsosPendientes,
  realizarDesembolso,
  validarDesembolso,
} from '../../../api/cajaDiario'
import { ClienteBuscarAutoComplete } from '../../../components/caja/ClienteBuscarAutoComplete'
import { CredixDataTable } from '../../../components/credix'
import type { DesembolsoPendienteRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { maybeDownloadCajaTicket } from './cajaPagoHelpers'
import type { CajaSession } from './types'
import { errMsg } from './types'

export function DesembolsosTab({
  ctx,
  active,
  onChanged,
}: {
  ctx: CajaSession
  active: boolean
  onChanged: () => void
}) {
  const [clienteLabel, setClienteLabel] = useState('')
  const [personaId, setPersonaId] = useState(0)

  const query = useQuery({
    queryKey: ['caja-desembolsos', ctx.oficinaId, personaId],
    queryFn: () => fetchDesembolsosPendientes(ctx.oficinaId, personaId),
    enabled: active,
  })

  const desembolsar = useMutation({
    mutationFn: (creditoId: number) =>
      realizarDesembolso({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId,
      }),
    onSuccess: async (r) => {
      message.success(
        r.yaRegistrado
          ? 'Desembolso ya estaba registrado'
          : `Desembolso OK (mov. ${r.movimientoCajaId})`,
      )
      if (!r.yaRegistrado) {
        await maybeDownloadCajaTicket(ctx.oficinaId, r.movimientoCajaId)
      }
      onChanged()
      void query.refetch()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const solicitarDesembolso = async (row: DesembolsoPendienteRow) => {
    try {
      const v = await validarDesembolso(
        ctx.oficinaId,
        ctx.cajaDiarioId,
        row.creditoId,
      )
      if (!v.puedeDesembolsar) {
        message.error(v.mensaje ?? 'No se puede desembolsar')
        return
      }
      const detalle = v.mensaje?.trim()
        ? `${v.mensaje}\n\n`
        : ''
      cajaConfirm({
        title: 'Confirmar desembolso',
        content: (
          <>
            {detalle}
            ¿Confirmar desembolso de {formatMoney(row.montoDesembolso)} al crédito{' '}
            {row.creditoId} ({row.personaNombre})?
          </>
        ),
        okText: 'Desembolsar',
        onOk: () => desembolsar.mutateAsync(row.creditoId),
      })
    } catch (e) {
      message.error(errMsg(e))
    }
  }

  const columns: ColumnsType<DesembolsoPendienteRow> = [
    { title: 'Nro crédito', dataIndex: 'creditoId', width: 88 },
    { title: 'Código', dataIndex: 'personaCodigo', width: 90 },
    { title: 'Cliente', dataIndex: 'personaNombre', ellipsis: true },
    {
      title: 'Crédito',
      dataIndex: 'montoCredito',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Gasto adm.',
      dataIndex: 'montoGastosAdm',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Desembolso',
      dataIndex: 'montoDesembolso',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 100 },
    {
      title: '',
      width: 110,
      render: (_, row) => (
        <Button
          size="small"
          type="primary"
          loading={desembolsar.isPending}
          onClick={() => void solicitarDesembolso(row)}
        >
          Desembolsar
        </Button>
      ),
    },
  ]

  return (
    <div className="caja-diario-desembolsos">
      <div className="caja-diario-cobranzas-top">
        <div className="caja-diario-cobranzas-top__search">
          <span className="caja-diario-cobranzas-top__label">Buscar cliente</span>
          <ClienteBuscarAutoComplete
            value={clienteLabel}
            onChange={setClienteLabel}
            onSelectPersona={(pid, label) => {
              setPersonaId(pid)
              setClienteLabel(label)
            }}
            fullWidth
            showSearchButton
            searchButtonLabel="Cargar"
            placeholder="Buscar cliente por DNI y nombres"
          />
        </div>
        <div className="caja-diario-actions-row">
          <Button
            icon={<ReloadOutlined />}
            onClick={() => {
              setPersonaId(0)
              setClienteLabel('')
            }}
          >
            Pendientes
          </Button>
        </div>
      </div>

      <CajaSection
        tone="brand"
        kicker="Desembolso"
        title={clienteLabel || 'Pendientes del día'}
        icon={<BankOutlined />}
      >
        <CredixDataTable<DesembolsoPendienteRow>
          rowKey="creditoId"
          columns={columns}
          dataSource={query.data ?? []}
          loading={query.isLoading}
          pagination={{ defaultPageSize: 20 }}
          size="small"
          scroll={{ x: 960 }}
        />
      </CajaSection>
    </div>
  )
}
