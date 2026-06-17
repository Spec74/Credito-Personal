import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from '../components/layout/AppShell'
import { ProtectedRoute } from '../auth/ProtectedRoute'
import { CreditoOperacionRoute } from '../components/credito/CreditoOperacionRoute'
import { LoginPage } from '../pages/LoginPage'
import { ReportViewerPage } from '../pages/reportes/ReportViewerPage'
import * as Pages from './lazyPages'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/reportes/visor"
        element={
          <ProtectedRoute>
            <ReportViewerPage />
          </ProtectedRoute>
        }
      />
      <Route
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<Navigate to="/inicio" replace />} />
        <Route path="/inicio" element={<Pages.HomePage />} />
        <Route path="/maestros" element={<Pages.MaestrosHubPage />} />
        <Route path="/maestros/marcas" element={<Pages.MarcasPage />} />
        <Route path="/maestros/modelos" element={<Pages.ModelosPage />} />
        <Route path="/maestros/tipos-articulo" element={<Pages.TipoArticuloPage />} />
        <Route path="/maestros/articulos" element={<Pages.ArticulosPage />} />
        <Route path="/maestros/almacenes" element={<Pages.AlmacenesPage />} />
        <Route path="/ventas" element={<Pages.VentasHubPage />} />
        <Route path="/ventas/lista-precios" element={<Pages.ListaPreciosPage />} />
        <Route
          path="/ventas/informe-lista-precios"
          element={<Pages.ListaPrecioInformePage />}
        />
        <Route path="/ventas/venta-rapida" element={<Pages.VentaRapidaPage />} />
        <Route path="/ventas/orden-venta" element={<Pages.OrdenVentaPage />} />
        <Route path="/ventas/canjear-puntos" element={<Pages.CanjearPuntosPage />} />
        <Route
          path="/informes/rentabilidad-venta"
          element={<Pages.RentabilidadVentaPage />}
        />
        <Route path="/almacen" element={<Pages.AlmacenHubPage />} />
        <Route path="/almacen/kardex" element={<Pages.KardexPage />} />
        <Route path="/almacen/entrada" element={<Pages.EntradaAlmacenPage />} />
        <Route path="/almacen/salida" element={<Pages.SalidaAlmacenPage />} />
        <Route path="/almacen/transferencia" element={<Pages.TransferenciaAlmacenPage />} />
        <Route path="/almacen/movimiento" element={<Pages.MovimientoAlmacenPage />} />
        <Route path="/informes" element={<Pages.InformesHubPage />} />
        <Route path="/informes/cobertura" element={<Pages.InformesCoberturaPage />} />
        <Route path="/informes/reporte-stock" element={<Pages.ReporteStockPage />} />
        <Route path="/informes/stock-anulados" element={<Pages.StockAnuladosPage />} />
        <Route path="/clientes" element={<Pages.ClientesPage />} />
        <Route path="/clientes/nuevo" element={<Pages.ClienteFormPage />} />
        <Route path="/clientes/editar/:personaId" element={<Pages.ClienteFormPage />} />
        <Route path="/informes/saldo-cartera" element={<Pages.SaldoCarteraPage />} />
        <Route path="/informes/cobro-diario" element={<Pages.CobroDiarioPage />} />
        <Route
          path="/informes/clientes-inactivos"
          element={<Pages.ClientesInactivosPage />}
        />
        <Route path="/informes/morosidad-gestor" element={<Pages.MorosidadGestorPage />} />
        <Route path="/informes/credito-vencido" element={<Pages.CreditoVencidoPage />} />
        <Route
          path="/informes/clientes-bloqueados"
          element={<Pages.ClientesBloqueadosPage />}
        />
        <Route
          path="/informes/clientes-tope-credito"
          element={<Pages.ClientesTopeCreditoPage />}
        />
        <Route
          path="/informes/creditos-observados"
          element={<Pages.CreditoObservadoPage />}
        />
        <Route path="/informes/credito-morosidad" element={<Pages.CreditoMorosidadPage />} />
        <Route
          path="/informes/clientes-nuevos-mes"
          element={<Pages.ClientesNuevosMesPage />}
        />
        <Route path="/informes/credito-condonado" element={<Pages.CreditoCondonadoPage />} />
        <Route path="/informes/cajas-asignadas" element={<Pages.CajasAsignadasPage />} />
        <Route path="/informes/caja-diario" element={<Pages.CajaDiarioInformePage />} />
        <Route
          path="/informes/credito-rentabilidad"
          element={<Pages.CreditoRentabilidadPage />}
        />
        <Route path="/informes/credito-aprobacion" element={<Pages.CreditoAprobacionPage />} />
        <Route path="/informes/creditos-activos" element={<Pages.CreditosActivosPage />} />
        <Route path="/informes/creditos-cierres" element={<Pages.CreditosCierresPage />} />
        <Route
          path="/informes/creditos-morosos-pagados"
          element={<Pages.CreditosMorososPagadosPage />}
        />
        <Route
          path="/informes/movimientos-caja-anulados"
          element={<Pages.MovimientoCajaAnuladoPage />}
        />
        <Route
          path="/informes/comprobantes-caja-chica"
          element={<Pages.ComprobantesCajaChicaPage />}
        />
        <Route path="/informes/reporte-creditos" element={<Pages.ReporteCreditoPage />} />
        <Route
          path="/informes/saldo-cartera-caja-diario"
          element={<Pages.SaldoCarteraCajaDiarioPage />}
        />
        <Route path="/informes/cobro-diario-detalle" element={<Pages.CobroDiarioDetallePage />} />
        <Route path="/informes/aval-persona" element={<Pages.AvalPersonaPage />} />
        <Route path="/informes/central-riesgo" element={<Pages.CentralRiesgoPage />} />
        <Route path="/informes/plan-pagos" element={<Pages.PlanPagosPage />} />
        <Route path="/informes/estado-credito" element={<Pages.EstadoCreditoPage />} />
        <Route path="/informes/credito-tarea" element={<Pages.CreditoTareaPage />} />
        <Route path="/informes/reporte-cliente" element={<Pages.ReporteClientePage />} />
        <Route path="/almacen/codigo-barras" element={<Pages.CodigoBarrasPage />} />
        <Route path="/almacen/constancia" element={<Pages.ConstanciaAlmacenPage />} />
        <Route path="/reportes/credito" element={<Pages.ReporteCreditoIndexPage />} />
        <Route path="/reportes/almacen" element={<Pages.ReporteAlmacenIndexPage />} />
        <Route path="/reportes/cobranza" element={<Pages.CobranzaPagosPage />} />
        <Route path="/reportes/venta" element={<Pages.ReporteVentaIndexPage />} />
        <Route path="/credito" element={<Pages.CreditoHubPage />} />
        <Route
          path="/credito/simulador"
          element={
            <CreditoOperacionRoute>
              <Pages.SimuladorCreditoPage />
            </CreditoOperacionRoute>
          }
        />
        <Route
          path="/credito/parametros-simulador"
          element={
            <CreditoOperacionRoute>
              <Pages.ParametrosSimuladorPage />
            </CreditoOperacionRoute>
          }
        />
        <Route
          path="/credito/consulta"
          element={
            <CreditoOperacionRoute>
              <Pages.ConsultaCreditoPage />
            </CreditoOperacionRoute>
          }
        />
        <Route
          path="/credito/persona/:personaId"
          element={
            <CreditoOperacionRoute>
              <Pages.CreditoPersonaPage />
            </CreditoOperacionRoute>
          }
        />
        <Route
          path="/credito/tareas"
          element={
            <CreditoOperacionRoute>
              <Pages.TareasPage />
            </CreditoOperacionRoute>
          }
        />
        <Route path="/credito/aprobar" element={<Pages.CreditoAprobarPage />} />
        <Route path="/caja" element={<Pages.CajaHubPage />} />
        <Route path="/caja/maestro" element={<Pages.CajaMaestroPage />} />
        <Route path="/caja/diario" element={<Pages.CajaDiarioPage />} />
        <Route path="/caja/chica" element={<Pages.CajaChicaPage />} />
        <Route path="/caja/asignar" element={<Pages.AsignarCajaPage />} />
        <Route path="/caja/saldos" element={<Pages.SaldosPage />} />
        <Route path="/caja/verificar-pagos" element={<Pages.VerificarPagosPage />} />
        <Route path="/admin" element={<Pages.AdminHubPage />} />
        <Route path="/mantenimiento/oficinas" element={<Pages.OficinasPage />} />
        <Route path="/mantenimiento/cajas" element={<Pages.CajaMaestroPage />} />
        <Route
          path="/admin/oficinas"
          element={<Navigate to="/mantenimiento/oficinas" replace />}
        />
        <Route path="/admin/usuarios" element={<Pages.UsuariosPage />} />
        <Route path="/admin/roles" element={<Pages.RolesPage />} />
        <Route path="/admin/comisiones" element={<Pages.ComisionesPage />} />
        <Route path="/tesoreria" element={<Pages.TesoreriaHubPage />} />
        <Route path="/tesoreria/boveda" element={<Pages.BovedaPage />} />
        <Route
          path="/tesoreria/movimiento-boveda"
          element={<Pages.MovimientoBovedaPage />}
        />
        <Route path="/modulo/:menuId" element={<Pages.ModulePlaceholderPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/inicio" replace />} />
    </Routes>
  )
}
