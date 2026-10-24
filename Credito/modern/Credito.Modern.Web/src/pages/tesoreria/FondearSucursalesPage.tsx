import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, InputNumber, Select, message } from 'antd'
import {
  fetchBovedaAbierta,
  fetchBovedasDestinoTransferencia,
  transferirBoveda,
} from '../../api/boveda'
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage, CredixDataTable, confirmMoney } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import { resolverBovedaDestinoPorSede } from '../../utils/sucursalRed'
import { buildTipoPagoOptions, tipoPagoOptionRender } from './components/TipoPagoBrand'
import type { BovedaDestinoTransferencia } from '../../api/boveda'

export function FondearSucursalesPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const queryClient = useQueryClient()
  const [montos, setMontos] = useState<Record<number, number>>({})
  const [tipoPagoId, setTipoPagoId] = useState(1)

  const origen = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const destinos = useQuery({
    queryKey: ['bovedas-destino-transferencia', oficinaId],
    queryFn: () => fetchBovedasDestinoTransferencia(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const tiposPago = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    enabled: oficinaId > 0,
    staleTime: 5 * 60_000,
  })
  const tipoPagoOptions = useMemo(
    () => buildTipoPagoOptions(tiposPago.data ?? []),
    [tiposPago.data],
  )

  const filas = useMemo(() => destinos.data ?? [], [destinos.data])

  const enviar = useMutation({
    mutationFn: (row: BovedaDestinoTransferencia) => {
      const monto = montos[row.oficinaId] ?? 0
      if (!(monto > 0)) throw new Error('Indique un monto')
      if (!origen.data?.bovedaId) throw new Error('No hay bóveda abierta en esta oficina')
      const destino = resolverBovedaDestinoPorSede(filas, row.oficinaId)
      if (!destino) throw new Error('No hay bóveda abierta en la sede destino')
      return transferirBoveda({
        oficinaId,
        bovedaInicioId: origen.data.bovedaId,
        bovedaDestinoId: destino.bovedaId,
        monto,
        glosa: `Fondeo ${row.oficina}`,
        tipoPagoId: tipoPagoId > 0 ? tipoPagoId : 1,
      })
    },
    onSuccess: (_, row) => {
      message.success(`Fondeo enviado a ${row.oficina}`)
      setMontos((prev) => ({ ...prev, [row.oficinaId]: 0 }))
      void queryClient.invalidateQueries({ queryKey: ['bovedas-destino-transferencia'] })
      void queryClient.invalidateQueries({ queryKey: ['boveda-abierta'] })
      void queryClient.invalidateQueries({ queryKey: ['boveda-red-posicion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error'),
  })

  const pedirFondeo = (row: BovedaDestinoTransferencia) => {
    const monto = montos[row.oficinaId] ?? 0
    confirmMoney({
      title: `Fondear ${row.oficina}`,
      amount: monto,
      officeLabel: session?.oficinaDenominacion
        ? `${session.oficinaDenominacion} → ${row.oficina}`
        : row.oficina,
      consequence: `Viaja ${
        tipoPagoOptions.find((o) => o.value === tipoPagoId)?.label ?? 'el medio seleccionado'
      }. Se acredita de inmediato en origen y destino.`,
      okText: 'Fondear',
      onOk: async () => {
        await enviar.mutateAsync(row)
      },
    })
  }

  return (
    <CredixPage
      title="Fondear sucursales"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/tesoreria">Tesorería</Link> },
        { title: <Link to="/tesoreria/boveda">Bóveda</Link> },
        { title: 'Fondear' },
      ]}
    >
      {oficinaId < 1 ? (
        <Alert type="warning" showIcon message="Sesión sin oficina." />
      ) : null}
      {origen.data ? (
        <p style={{ marginTop: 0 }}>
          Origen: bóveda #{origen.data.bovedaId} · saldo {formatMoney(origen.data.saldoFinal)}
        </p>
      ) : origen.isError ? (
        <Alert type="warning" showIcon message="No hay bóveda abierta en esta sede." />
      ) : null}

      <div style={{ maxWidth: 320, marginBottom: 12 }}>
        <label htmlFor="fondeo-medio">Medio que viaja</label>
        <Select
          id="fondeo-medio"
          style={{ width: '100%', marginTop: 4 }}
          value={tipoPagoId}
          loading={tiposPago.isLoading}
          options={tipoPagoOptions}
          optionFilterProp="label"
          showSearch
          optionRender={tipoPagoOptionRender}
          onChange={(v) => setTipoPagoId(Number(v) || 1)}
        />
      </div>

      <CredixDataTable<BovedaDestinoTransferencia>
        rowKey="oficinaId"
        loading={destinos.isLoading}
        dataSource={filas}
        locale={{ emptyText: 'No hay bóvedas destino (¿está en la oficina central?)' }}
        columns={[
          { title: 'Sede', dataIndex: 'oficina', ellipsis: true },
          {
            title: 'Saldo destino',
            dataIndex: 'saldoFinal',
            align: 'right',
            width: 140,
            render: formatMoney,
          },
          {
            title: 'Monto',
            key: 'monto',
            width: 140,
            render: (_, row) => (
              <InputNumber
                min={0}
                step={10}
                prefix="S/"
                style={{ width: '100%' }}
                value={montos[row.oficinaId]}
                onChange={(v) =>
                  setMontos((prev) => ({ ...prev, [row.oficinaId]: Number(v ?? 0) }))
                }
              />
            ),
          },
          {
            title: '',
            key: 'acc',
            width: 120,
            render: (_, row) => (
              <Button
                type="primary"
                disabled={!(montos[row.oficinaId] > 0) || !origen.data}
                loading={enviar.isPending && enviar.variables?.oficinaId === row.oficinaId}
                onClick={() => pedirFondeo(row)}
              >
                Fondear
              </Button>
            ),
          },
        ]}
      />
    </CredixPage>
  )
}
