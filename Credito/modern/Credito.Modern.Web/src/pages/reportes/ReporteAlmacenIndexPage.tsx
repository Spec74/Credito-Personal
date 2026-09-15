import { useState } from 'react'
import { message } from 'antd'
import { CredixPage } from '../../components/credix'
import { CredixReportBox } from '../../components/reportes/CredixReportBox'
import { ReportExportActions } from '../../components/reportes/ReportExportActions'
import { ReporteField, OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import {
  openReporteStockCsvInTab,
  openReporteStockPdfInTab,
  openStockAnuladosCsvInTab,
  openStockAnuladosPdfInTab,
} from '../../api/almacen'
import { useAuth } from '../../auth/useAuth'
import { reportesAlmacenIndexBreadcrumb } from '../../utils/reportesBreadcrumbs'

export function ReporteAlmacenIndexPage() {
  const { session } = useAuth()
  const oficinaSesion = session?.oficinaId ?? 1
  const [stockOficina, setStockOficina] = useState<number | undefined>(oficinaSesion)

  const run = async (label: string, fn: () => Promise<void>) => {
    try {
      await fn()
      message.success(`${label} abierto en nueva pestaña`)
    } catch (e) {
      message.error(e instanceof Error ? e.message : `No se pudo abrir ${label}`)
    }
  }

  return (
    <CredixPage
      title="Reportes de almacén"
      subtitle="Stock general y productos anulados. PDF y Excel se abren en otra pestaña; use Ver pantalla para consultar en tabla."
      breadcrumb={reportesAlmacenIndexBreadcrumb()}
    >
      <p className="credix-reportes-intro">
        Paridad con <strong>Reporte → Almacén</strong> del sistema anterior. Los catálogos de
        artículos y movimientos están en <strong>Almacén</strong> y <strong>Maestros</strong>.
      </p>

      <div className="credix-reporte-grid">
        <CredixReportBox
          title="Stock general de productos"
          actions={
            <ReportExportActions
              screenTo="/informes/reporte-stock"
              screenSearchParams={{ oficinaId: stockOficina ?? oficinaSesion }}
              exports={[
                {
                  label: 'Stock PDF',
                  format: 'pdf',
                  onClick: () =>
                    run('Stock PDF', () =>
                      openReporteStockPdfInTab(stockOficina ?? oficinaSesion),
                    ),
                },
                {
                  label: 'Stock XLS',
                  format: 'xls',
                  onClick: () =>
                    run('Stock Excel', () =>
                      openReporteStockCsvInTab(stockOficina ?? oficinaSesion),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label="Oficina">
            <OficinaSelect value={stockOficina} onChange={setStockOficina} />
          </ReporteField>
        </CredixReportBox>

        <CredixReportBox
          title="Productos anulados"
          actions={
            <ReportExportActions
              screenTo="/informes/stock-anulados"
              exports={[
                {
                  label: 'Anulados PDF',
                  format: 'pdf',
                  onClick: () => run('Anulados PDF', openStockAnuladosPdfInTab),
                },
                {
                  label: 'Anulados XLS',
                  format: 'xls',
                  onClick: () => run('Anulados Excel', openStockAnuladosCsvInTab),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Sin filtros adicionales; lista todos los movimientos anulados del sistema.
          </p>
        </CredixReportBox>
      </div>
    </CredixPage>
  )
}
