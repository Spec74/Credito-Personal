import { useQuery } from '@tanstack/react-query'
import { Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { fetchRptAval } from '../../api/creditoPlanes'
import { formatMoney } from '../../utils/formatMoney'
import type { RptAvalRow } from '../../types/api'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { CredixDataTable } from '../credix'

const { Text } = Typography

type Props = {
  personaId: number
}

/** Paridad bloque «Aval y Avalados» en `Creditos.cshtml`. */
export function CreditoPersonaAvalesPanel({ personaId }: Props) {
  const query = useQuery({
    queryKey: ['rpt-aval-consulta', personaId],
    queryFn: () => fetchRptAval(personaId),
    enabled: personaId > 0,
    staleTime: creditoStaleTime.ficha,
  })

  const columns: ColumnsType<RptAvalRow> = [
    { title: 'Tipo', dataIndex: 'grupo' },
    { title: 'Crédito', dataIndex: 'creditoId', align: 'center' },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado' },
    { title: 'Persona', dataIndex: 'persona', ellipsis: true, minWidth: 120 },
    { title: 'DNI', dataIndex: 'dni' },
    { title: 'Celular', dataIndex: 'celular' },
  ]

  const rows = query.data ?? []
  if (query.isLoading) {
    return (
      <section className="credito-consulta-avales">
        <Text type="secondary">Cargando avales y avalados…</Text>
      </section>
    )
  }

  if (rows.length === 0) {
    return null
  }

  return (
    <section className="credito-consulta-avales" aria-label="Avales y avalados">
      <div className="credito-consulta-avales__head">
        <Text strong>Aval y avalados</Text>
        <Text type="secondary" className="credito-consulta-avales__hint">
          Misma grilla que el MVC (`usp_RptAval`).
        </Text>
      </div>
      <CredixDataTable<RptAvalRow>
        mode="operacion"
        className="credito-consulta-avales__table"
        rowKey={(r, i) => `${r.grupo}-${r.creditoId}-${i}`}
        pagination={false}
        loading={query.isFetching}
        columns={columns}
        dataSource={rows}
      />
    </section>
  )
}
