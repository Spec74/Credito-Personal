import { message } from 'antd'
import { CredixPage } from '../../components/credix'
import { CredixReportBox } from '../../components/reportes/CredixReportBox'
import { ReportExportActions } from '../../components/reportes/ReportExportActions'
import { openLegacyReporteVentaIndex } from '../../config/legacyReportUrls'
import { reportesVentaIndexBreadcrumb } from '../../utils/reportesBreadcrumbs'

export function ReporteVentaIndexPage() {
  const openLegacyIndex = () => {
    openLegacyReporteVentaIndex()
    message.success('Reporte legacy abierto en nueva pestaña')
  }

  return (
    <CredixPage
      title="Reportes de venta"
      subtitle="Rentabilidad de ventas y lista de precios con datos modernos; RDLC legacy disponible como respaldo operativo."
      breadcrumb={reportesVentaIndexBreadcrumb()}
    >
      <p className="credix-reportes-intro">
        Paridad con <strong>Reporte → Venta</strong> del sistema anterior. Los informes
        principales ya tienen pantalla SPA; el índice RDLC queda disponible durante el
        cutover strangler.
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
                  label: 'RDLC legacy',
                  format: 'pdf',
                  onClick: openLegacyIndex,
                  title: 'Abre el índice MVC Reporte/Venta como respaldo',
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Equivale a <strong>ReporteAvanceVenta</strong>; permite filtrar fechas,
            contado/crédito y oficina desde la pantalla moderna.
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
                  label: 'Índice legacy',
                  format: 'pdf',
                  onClick: openLegacyIndex,
                  title: 'Mantiene acceso al índice MVC hasta retirar RDLC',
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Informe comercial conectado al módulo Ventas; usa los mismos filtros
            operativos que el legado.
          </p>
        </CredixReportBox>
      </div>
    </CredixPage>
  )
}
