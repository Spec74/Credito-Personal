import { useMutation } from '@tanstack/react-query'
import { Alert, Button } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchStockAnulados,
  openStockAnuladosCsvInTab,
  openStockAnuladosPdfInTab,
  type RptStockAnuladoRow,
} from '../../api/almacen'
import { ApiError } from '../../api/errors'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import { reportesAlmacenBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { formatFecha } from '../../utils/formatFecha'

export function StockAnuladosPage() {
  const consulta = useMutation({ mutationFn: fetchStockAnulados })

  const stats = useInformeStats(consulta)
  const columns: ColumnsType<RptStockAnuladoRow> = [
    { title: 'Mov.', dataIndex: 'movimientoId', width: 70 },
    { title: 'Tipo', dataIndex: 'movimiento', width: 120, ellipsis: true },
    { title: 'Observación', dataIndex: 'observacion', width: 160, ellipsis: true },
    { title: 'Fecha', dataIndex: 'fecha', width: 110, render: formatFecha },
    { title: 'Cant.', dataIndex: 'cantidad', width: 70, align: 'right' },
    { title: 'Detalle', dataIndex: 'detalle', ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Productos anulados"
      subtitle="Movimientos de stock anulados (misma consulta que el informe legacy)."
      breadcrumb={reportesAlmacenBreadcrumb('Productos anulados')}
      stats={stats}
      filters={
        <Button
          type="primary"
          icon={<SearchOutlined />}
          onClick={() => consulta.mutate()}
          loading={consulta.isPending}
        >
          Consultar
        </Button>
      }
      exportBar={
        <InformeExportBar
          csvDisabled={!consulta.isSuccess}
          pdfDisabled={!consulta.isSuccess}
          onCsv={() => openStockAnuladosCsvInTab()}
          onPdfTabular={() => openStockAnuladosPdfInTab()}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError ? consulta.error.message : 'Error en la consulta'
            }
          />
        ) : null
      }
    >
      <CredixDataTable<RptStockAnuladoRow>
        rowKey="movimientoId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 20 }}
        locale={{ emptyText: 'Ejecute Consultar para cargar datos' }}
      />
    </CredixInformePage>
  )
}
