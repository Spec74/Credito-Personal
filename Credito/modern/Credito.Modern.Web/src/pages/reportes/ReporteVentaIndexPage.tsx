import dayjs from 'dayjs'
import { CredixPage } from '../../components/credix'
import { CredixReportBox } from '../../components/reportes/CredixReportBox'
import { ReportExportActions } from '../../components/reportes/ReportExportActions'
import { useAuth } from '../../auth/useAuth'
import { downloadListaPrecioInformeCsv, downloadListaPrecioInformePdf } from '../../api/ventasInformes'
import { downloadRentabilidadVentaCsv, downloadRentabilidadVentaPdf } from '../../api/ventas'
import { reportesVentaIndexBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { runOpenReport } from '../../utils/reportExport'

export function ReporteVentaIndexPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const fechaIni = dayjs().startOf('month').format('YYYY-MM-DD')
  const fechaFin = dayjs().endOf('month').format('YYYY-MM-DD')

  return (
    <CredixPage
      title="Reportes de venta"
      subtitle="Rentabilidad de ventas y lista de precios. PDF y Excel (CSV) usan la API moderna."
      breadcrumb={reportesVentaIndexBreadcrumb()}
    >
      <p className="credix-reportes-intro">
        Paridad con <strong>Reporte → Venta</strong> del sistema anterior. Los filtros completos
        están en cada pantalla; desde aquí se exporta el mes en curso de la oficina de la sesión.
      </p>

      <div className="credix-reporte-grid">
        <CredixReportBox
          title="Rentabilidad de ventas"
          actions={
            <ReportExportActions
              screenTo="/informes/rentabilidad-venta"
              screenLabel="Ver informe"
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Rentabilidad PDF', () =>
                      downloadRentabilidadVentaPdf({
                        oficinaId,
                        fechaIni,
                        fechaFin,
                      }),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Rentabilidad Excel', () =>
                      downloadRentabilidadVentaCsv({
                        oficinaId,
                        fechaIni,
                        fechaFin,
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Equivale a <strong>ReporteAvanceVenta</strong> (<code>usp_RptRentabilidadVenta</code>).
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Lista de precios"
          actions={
            <ReportExportActions
              screenTo="/ventas/informe-lista-precios"
              screenLabel="Ver informe"
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Lista precios PDF', () =>
                      downloadListaPrecioInformePdf({
                        indDescuento: false,
                        indPuntos: false,
                      }),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Lista precios Excel', () =>
                      downloadListaPrecioInformeCsv({
                        indDescuento: false,
                        indPuntos: false,
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Informe comercial del módulo Ventas. Marca y flags de descuento/puntos se eligen en
            pantalla.
          </p>
        </CredixReportBox>
      </div>
    </CredixPage>
  )
}
