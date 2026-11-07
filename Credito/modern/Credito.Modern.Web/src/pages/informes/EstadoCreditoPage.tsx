import { useMemo, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Button, Descriptions, InputNumber, Table, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadRptEstadoCreditoCsv,
  downloadRptEstadoCreditoPdf,
  fetchRptEstadoCredito,
} from '../../api/creditoPlanes'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { EstadoPlanPagoCuota } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { sumMoney } from '../../utils/money'
import {
  importePagadoPlanCuota,
  totalPagadoPlanCuotas,
  totalSaldoPorPagarPlanCuotas,
} from '../../utils/estadoPlanPagoMontos'
import '../../styles/credito-consulta.css'

function estadoPlanRowClass(row: EstadoPlanPagoCuota): string {
  const estado = row.estado?.trim().toUpperCase()
  if (estado === 'PAG') return 'credito-plan-row credito-plan-row--pagado'
  if ((row.diasAtrazo ?? 0) > 0) return 'credito-plan-row credito-plan-row--vencido'
  if (estado === 'CRE') return 'credito-plan-row credito-plan-row--creado'
  if ((row.pagoLibre ?? 0) > 0) return 'credito-plan-row credito-plan-row--parcial'
  return 'credito-plan-row credito-plan-row--pendiente'
}

export function EstadoCreditoPage() {
  const [creditoId, setCreditoId] = useState<number | null>(null)

  const consulta = useMutation({
    mutationFn: (id: number) => fetchRptEstadoCredito(id),
  })

  const csv = useMutation({
    mutationFn: (id: number) => downloadRptEstadoCreditoCsv(id),
  })

  const pdf = useMutation({
    mutationFn: (id: number) => downloadRptEstadoCreditoPdf(id),
  })

  const consultar = () => {
    if (creditoId != null && creditoId >= 1) {
      consulta.mutate(creditoId)
    }
  }

  const cuotas = consulta.data?.cuotas ?? []
  const stats = useInformeStats({ ...consulta, data: cuotas })
  const cab = consulta.data?.cabecera

  const resumen = useMemo(() => {
    const pagadas = cuotas.filter((r) => r.estado?.trim().toUpperCase() === 'PAG').length
    return {
      amortizacion: sumMoney(cuotas.map((r) => r.amortizacion ?? 0)),
      interes: sumMoney(cuotas.map((r) => r.interes ?? 0)),
      gastosAdm: sumMoney(cuotas.map((r) => r.gastosAdm ?? 0)),
      cuota: sumMoney(cuotas.map((r) => r.cuota ?? 0)),
      mora: sumMoney(cuotas.map((r) => r.importeMora ?? 0)),
      descuento: sumMoney(cuotas.map((r) => r.descuento ?? 0)),
      cargo: sumMoney(cuotas.map((r) => r.cargo ?? 0)),
      pagoLibre: sumMoney(cuotas.map((r) => r.pagoLibre ?? 0)),
      pagado: totalPagadoPlanCuotas(cuotas),
      saldo: totalSaldoPorPagarPlanCuotas(cuotas),
      pagadas,
    }
  }, [cuotas])

  const columns: ColumnsType<EstadoPlanPagoCuota> = [
    { title: 'N°', dataIndex: 'numero', width: 50 },
    {
      title: 'Capital',
      dataIndex: 'capital',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Amortización',
      dataIndex: 'amortizacion',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Gastos adm.',
      dataIndex: 'gastosAdm',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Vencimiento',
      dataIndex: 'fechaVencimiento',
      width: 105,
      render: formatFecha,
    },
    {
      title: 'Cuota',
      dataIndex: 'cuota',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 70 },
    {
      title: 'Días atraso',
      dataIndex: 'diasAtrazo',
      width: 90,
      render: (v: number | null) => (v != null ? v : '—'),
    },
    {
      title: 'Imp. mora',
      dataIndex: 'importeMora',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Descuento',
      dataIndex: 'descuento',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cargo',
      dataIndex: 'cargo',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Pago libre',
      dataIndex: 'pagoLibre',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'F. pago cuota',
      dataIndex: 'fechaPagoCuota',
      width: 110,
      render: formatFecha,
    },
    {
      title: 'Pagado',
      dataIndex: 'pagoCuota',
      width: 95,
      align: 'right',
      render: (_v: number | null, row) => formatMoney(importePagadoPlanCuota(row)),
    },
  ]

  return (
    <CredixInformePage
      title="Estado de crédito"
      subtitle={undefined}
      breadcrumb={reportesCreditoBreadcrumb('Estado crédito')}
      stats={stats}
      filters={
        <>
          <InputNumber
            min={1}
            placeholder="Crédito ID"
            value={creditoId ?? undefined}
            onChange={(v) => setCreditoId(v ?? null)}
            style={{ width: 140, marginRight: 8 }}
          />
          <Button
            type="primary"
            icon={<SearchOutlined />}
            onClick={consultar}
            loading={consulta.isPending}
            disabled={creditoId == null || creditoId < 1}
          >
            Consultar
          </Button>
        </>
      }
      exportBar={
        creditoId != null && creditoId >= 1 ? (
          <InformeExportBar
            csvLoading={csv.isPending}
            pdfLoading={pdf.isPending}
            onCsv={() => csv.mutate(creditoId)}
            onPdfTabular={() => pdf.mutate(creditoId)}
          />
        ) : null
      }
      panelTitle="Plan de pagos"
    >
      {cab ? (
        <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }} style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Crédito">{cab.creditoId}</Descriptions.Item>
          <Descriptions.Item label="Producto">{cab.producto}</Descriptions.Item>
          <Descriptions.Item label="Cliente">{cab.cliente}</Descriptions.Item>
          <Descriptions.Item label="Modalidad">{cab.modalidad}</Descriptions.Item>
          <Descriptions.Item label="Cuotas">{cab.numeroCuotas}</Descriptions.Item>
          <Descriptions.Item label="Estado">{cab.estado}</Descriptions.Item>
          <Descriptions.Item label="Monto crédito">
            {formatMoney(cab.montoCredito)}
          </Descriptions.Item>
          <Descriptions.Item label="Total (monto + intereses)">
            {formatMoney(cab.total)}
          </Descriptions.Item>
          <Descriptions.Item label="Pagado real">
            {formatMoney(resumen.pagado)}
          </Descriptions.Item>
          <Descriptions.Item label="Saldo por pagar">
            {formatMoney(resumen.saldo)}
          </Descriptions.Item>
          <Descriptions.Item label="Analista">{cab.analista}</Descriptions.Item>
        </Descriptions>
      ) : null}
      <div className="credito-consulta-page">
        <CredixDataTable<EstadoPlanPagoCuota>
          className="credito-plan-table"
          rowKey="planPagoId"
          columns={columns}
          dataSource={cuotas}
          loading={consulta.isPending}
          pagination={{ pageSize: 24 }}
          scroll={{ x: 1400 }}
          rowClassName={estadoPlanRowClass}
          locale={{ emptyText: 'Indique crédito y consulte' }}
          summary={() =>
            cuotas.length === 0 ? null : (
              <Table.Summary fixed>
                <Table.Summary.Row className="credito-plan-summary-row">
                  <Table.Summary.Cell index={0} colSpan={2}>
                    <Typography.Text strong>
                      Total ({resumen.pagadas}/{cuotas.length} pagadas)
                    </Typography.Text>
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={2} align="right">
                    {formatMoney(resumen.amortizacion)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={3} align="right">
                    {formatMoney(resumen.interes)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={4} align="right">
                    {formatMoney(resumen.gastosAdm)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={5}>—</Table.Summary.Cell>
                  <Table.Summary.Cell index={6} align="right">
                    {formatMoney(resumen.cuota)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={7}>—</Table.Summary.Cell>
                  <Table.Summary.Cell index={8}>—</Table.Summary.Cell>
                  <Table.Summary.Cell index={9} align="right">
                    {formatMoney(resumen.mora)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={10} align="right">
                    {formatMoney(resumen.descuento)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={11} align="right">
                    {formatMoney(resumen.cargo)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={12} align="right">
                    {formatMoney(resumen.pagoLibre)}
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={13}>—</Table.Summary.Cell>
                  <Table.Summary.Cell index={14} align="right">
                    {formatMoney(resumen.pagado)}
                  </Table.Summary.Cell>
                </Table.Summary.Row>
              </Table.Summary>
            )
          }
        />
      </div>
    </CredixInformePage>
  )
}
