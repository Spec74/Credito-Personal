import { lazy, type ComponentType } from 'react'

/** Carga diferida de páginas — un chunk por módulo (ver vite manualChunks). */
function lazyNamed<T extends Record<string, ComponentType<object>>>(
  factory: () => Promise<T>,
  exportName: keyof T & string,
) {
  return lazy(() =>
    factory().then((module) => ({
      default: module[exportName] as ComponentType<object>,
    })),
  )
}

export const HomePage = lazyNamed(
  () => import('../pages/HomePage'),
  'HomePage',
)
export const ModulePlaceholderPage = lazyNamed(
  () => import('../pages/ModulePlaceholderPage'),
  'ModulePlaceholderPage',
)

export const MaestrosHubPage = lazyNamed(
  () => import('../pages/masters/MaestrosHubPage'),
  'MaestrosHubPage',
)
export const MarcasPage = lazyNamed(
  () => import('../pages/masters/MarcasPage'),
  'MarcasPage',
)
export const ModelosPage = lazyNamed(
  () => import('../pages/masters/ModelosPage'),
  'ModelosPage',
)
export const TipoArticuloPage = lazyNamed(
  () => import('../pages/masters/TipoArticuloPage'),
  'TipoArticuloPage',
)
export const ArticulosPage = lazyNamed(
  () => import('../pages/masters/ArticulosPage'),
  'ArticulosPage',
)
export const AlmacenesPage = lazyNamed(
  () => import('../pages/masters/AlmacenesPage'),
  'AlmacenesPage',
)

export const VentasHubPage = lazyNamed(
  () => import('../pages/ventas/VentasHubPage'),
  'VentasHubPage',
)
export const ListaPreciosPage = lazyNamed(
  () => import('../pages/ventas/ListaPreciosPage'),
  'ListaPreciosPage',
)
export const ListaPrecioInformePage = lazyNamed(
  () => import('../pages/ventas/ListaPrecioInformePage'),
  'ListaPrecioInformePage',
)
export const VentaRapidaPage = lazyNamed(
  () => import('../pages/ventas/VentaRapidaPage'),
  'VentaRapidaPage',
)
export const OrdenVentaPage = lazyNamed(
  () => import('../pages/ventas/OrdenVentaPage'),
  'OrdenVentaPage',
)
export const CanjearPuntosPage = lazyNamed(
  () => import('../pages/ventas/CanjearPuntosPage'),
  'CanjearPuntosPage',
)

export const AlmacenHubPage = lazyNamed(
  () => import('../pages/almacen/AlmacenHubPage'),
  'AlmacenHubPage',
)
export const KardexPage = lazyNamed(
  () => import('../pages/almacen/KardexPage'),
  'KardexPage',
)
export const EntradaAlmacenPage = lazyNamed(
  () => import('../pages/almacen/EntradaAlmacenPage'),
  'EntradaAlmacenPage',
)
export const SalidaAlmacenPage = lazyNamed(
  () => import('../pages/almacen/SalidaAlmacenPage'),
  'SalidaAlmacenPage',
)
export const TransferenciaAlmacenPage = lazyNamed(
  () => import('../pages/almacen/TransferenciaAlmacenPage'),
  'TransferenciaAlmacenPage',
)
export const MovimientoAlmacenPage = lazyNamed(
  () => import('../pages/almacen/MovimientoAlmacenPage'),
  'MovimientoAlmacenPage',
)
export const CodigoBarrasPage = lazyNamed(
  () => import('../pages/almacen/CodigoBarrasPage'),
  'CodigoBarrasPage',
)
export const ConstanciaAlmacenPage = lazyNamed(
  () => import('../pages/almacen/ConstanciaAlmacenPage'),
  'ConstanciaAlmacenPage',
)

export const InformesHubPage = lazyNamed(
  () => import('../pages/informes/InformesHubPage'),
  'InformesHubPage',
)
export const InformesCoberturaPage = lazyNamed(
  () => import('../pages/informes/InformesCoberturaPage'),
  'InformesCoberturaPage',
)
export const RentabilidadVentaPage = lazyNamed(
  () => import('../pages/informes/RentabilidadVentaPage'),
  'RentabilidadVentaPage',
)
export const ReporteStockPage = lazyNamed(
  () => import('../pages/informes/ReporteStockPage'),
  'ReporteStockPage',
)
export const StockAnuladosPage = lazyNamed(
  () => import('../pages/informes/StockAnuladosPage'),
  'StockAnuladosPage',
)
export const CobroDiarioPage = lazyNamed(
  () => import('../pages/informes/CobroDiarioPage'),
  'CobroDiarioPage',
)
export const ClientesInactivosPage = lazyNamed(
  () => import('../pages/informes/ClientesInactivosPage'),
  'ClientesInactivosPage',
)
export const MorosidadGestorPage = lazyNamed(
  () => import('../pages/informes/MorosidadGestorPage'),
  'MorosidadGestorPage',
)
export const CreditoVencidoPage = lazyNamed(
  () => import('../pages/informes/CreditoVencidoPage'),
  'CreditoVencidoPage',
)
export const ClientesBloqueadosPage = lazyNamed(
  () => import('../pages/informes/ClientesBloqueadosPage'),
  'ClientesBloqueadosPage',
)
export const ClientesTopeCreditoPage = lazyNamed(
  () => import('../pages/informes/ClientesTopeCreditoPage'),
  'ClientesTopeCreditoPage',
)
export const CreditoObservadoPage = lazyNamed(
  () => import('../pages/informes/CreditoObservadoPage'),
  'CreditoObservadoPage',
)
export const CreditoMorosidadPage = lazyNamed(
  () => import('../pages/informes/CreditoMorosidadPage'),
  'CreditoMorosidadPage',
)
export const ClientesNuevosMesPage = lazyNamed(
  () => import('../pages/informes/ClientesNuevosMesPage'),
  'ClientesNuevosMesPage',
)
export const CreditoCondonadoPage = lazyNamed(
  () => import('../pages/informes/CreditoCondonadoPage'),
  'CreditoCondonadoPage',
)
export const CajasAsignadasPage = lazyNamed(
  () => import('../pages/informes/CajasAsignadasPage'),
  'CajasAsignadasPage',
)
export const CajaDiarioInformePage = lazyNamed(
  () => import('../pages/informes/CajaDiarioInformePage'),
  'CajaDiarioInformePage',
)
export const CreditoRentabilidadPage = lazyNamed(
  () => import('../pages/informes/CreditoRentabilidadPage'),
  'CreditoRentabilidadPage',
)
export const CreditoAprobacionPage = lazyNamed(
  () => import('../pages/informes/CreditoAprobacionPage'),
  'CreditoAprobacionPage',
)
export const CreditosActivosPage = lazyNamed(
  () => import('../pages/informes/CreditosActivosPage'),
  'CreditosActivosPage',
)
export const CreditosCierresPage = lazyNamed(
  () => import('../pages/informes/CreditosCierresPage'),
  'CreditosCierresPage',
)
export const CreditosMorososPagadosPage = lazyNamed(
  () => import('../pages/informes/CreditosMorososPagadosPage'),
  'CreditosMorososPagadosPage',
)
export const MovimientoCajaAnuladoPage = lazyNamed(
  () => import('../pages/informes/MovimientoCajaAnuladoPage'),
  'MovimientoCajaAnuladoPage',
)
export const ComprobantesCajaChicaPage = lazyNamed(
  () => import('../pages/informes/ComprobantesCajaChicaPage'),
  'ComprobantesCajaChicaPage',
)
export const ReporteCreditoPage = lazyNamed(
  () => import('../pages/informes/ReporteCreditoPage'),
  'ReporteCreditoPage',
)
export const SaldoCarteraCajaDiarioPage = lazyNamed(
  () => import('../pages/informes/SaldoCarteraCajaDiarioPage'),
  'SaldoCarteraCajaDiarioPage',
)
export const CobroDiarioDetallePage = lazyNamed(
  () => import('../pages/informes/CobroDiarioDetallePage'),
  'CobroDiarioDetallePage',
)
export const AvalPersonaPage = lazyNamed(
  () => import('../pages/informes/AvalPersonaPage'),
  'AvalPersonaPage',
)
export const CentralRiesgoPage = lazyNamed(
  () => import('../pages/informes/CentralRiesgoPage'),
  'CentralRiesgoPage',
)
export const PlanPagosPage = lazyNamed(
  () => import('../pages/informes/PlanPagosPage'),
  'PlanPagosPage',
)
export const EstadoCreditoPage = lazyNamed(
  () => import('../pages/informes/EstadoCreditoPage'),
  'EstadoCreditoPage',
)
export const CreditoTareaPage = lazyNamed(
  () => import('../pages/informes/CreditoTareaPage'),
  'CreditoTareaPage',
)
export const ReporteClientePage = lazyNamed(
  () => import('../pages/informes/ReporteClientePage'),
  'ReporteClientePage',
)

export const SaldoCarteraPage = lazyNamed(
  () => import('../pages/reports/SaldoCarteraPage'),
  'SaldoCarteraPage',
)

export const ClientesPage = lazyNamed(
  () => import('../pages/clientes/ClientesPage'),
  'ClientesPage',
)
export const ClienteFormPage = lazyNamed(
  () => import('../pages/clientes/ClienteFormPage'),
  'ClienteFormPage',
)

export const ReporteCreditoIndexPage = lazyNamed(
  () => import('../pages/reportes/ReporteCreditoIndexPage'),
  'ReporteCreditoIndexPage',
)
export const ReporteAlmacenIndexPage = lazyNamed(
  () => import('../pages/reportes/ReporteAlmacenIndexPage'),
  'ReporteAlmacenIndexPage',
)
export const CobranzaPagosPage = lazyNamed(
  () => import('../pages/reportes/CobranzaPagosPage'),
  'CobranzaPagosPage',
)
export const ReporteVentaIndexPage = lazyNamed(
  () => import('../pages/reportes/ReporteVentaIndexPage'),
  'ReporteVentaIndexPage',
)
export const CreditoHubPage = lazyNamed(
  () => import('../pages/credito/CreditoHubPage'),
  'CreditoHubPage',
)
export const SimuladorCreditoPage = lazyNamed(
  () => import('../pages/credito/SimuladorCreditoPage'),
  'SimuladorCreditoPage',
)
export const ParametrosSimuladorPage = lazyNamed(
  () => import('../pages/credito/ParametrosSimuladorPage'),
  'ParametrosSimuladorPage',
)
export const ConsultaCreditoPage = lazyNamed(
  () => import('../pages/credito/ConsultaCreditoPage'),
  'ConsultaCreditoPage',
)
export const CreditoPrendarioPage = lazyNamed(
  () => import('../pages/credito/CreditoPrendarioPage'),
  'CreditoPrendarioPage',
)
export const CreditoPrendarioNuevoPage = lazyNamed(
  () => import('../pages/credito/CreditoPrendarioNuevoPage'),
  'CreditoPrendarioNuevoPage',
)
export const CreditoPrendarioGestionPage = lazyNamed(
  () => import('../pages/credito/CreditoPrendarioGestionPage'),
  'CreditoPrendarioGestionPage',
)
export const CreditoPersonaPage = lazyNamed(
  () => import('../pages/credito/CreditoPersonaPage'),
  'CreditoPersonaPage',
)
export const TareasPage = lazyNamed(
  () => import('../pages/credito/TareasPage'),
  'TareasPage',
)
export const CreditoAprobarPage = lazyNamed(
  () => import('../pages/credito/CreditoAprobarPage'),
  'CreditoAprobarPage',
)
export const CreditoCondonacionesPage = lazyNamed(
  () => import('../pages/credito/CreditoCondonacionesPage'),
  'CreditoCondonacionesPage',
)

export const CajaHubPage = lazyNamed(
  () => import('../pages/caja/CajaHubPage'),
  'CajaHubPage',
)
export const CajaMaestroPage = lazyNamed(
  () => import('../pages/caja/CajaMaestroPage'),
  'CajaMaestroPage',
)
export const CajaDiarioPage = lazyNamed(
  () => import('../pages/caja/CajaDiarioPage'),
  'CajaDiarioPage',
)
export const CajaChicaPage = lazyNamed(
  () => import('../pages/caja/CajaChicaPage'),
  'CajaChicaPage',
)
export const AsignarCajaPage = lazyNamed(
  () => import('../pages/caja/AsignarCajaPage'),
  'AsignarCajaPage',
)
export const SaldosPage = lazyNamed(
  () => import('../pages/caja/saldospage'),
  'SaldosPage',
)
export const VerificarPagosPage = lazyNamed(
  () => import('../pages/caja/VerificarPagosPage'),
  'VerificarPagosPage',
)

export const AdminHubPage = lazyNamed(
  () => import('../pages/admin/AdminHubPage'),
  'AdminHubPage',
)
export const OficinasPage = lazyNamed(
  () => import('../pages/admin/OficinasPage'),
  'OficinasPage',
)
export const UsuariosPage = lazyNamed(
  () => import('../pages/admin/UsuariosPage'),
  'UsuariosPage',
)
export const RolesPage = lazyNamed(
  () => import('../pages/admin/RolesPage'),
  'RolesPage',
)
export const ComisionesPage = lazyNamed(
  () => import('../pages/admin/ComisionesPage'),
  'ComisionesPage',
)

export const TesoreriaHubPage = lazyNamed(
  () => import('../pages/tesoreria/tesoreriahubpage'),
  'TesoreriaHubPage',
)
export const BovedaPage = lazyNamed(
  () => import('../pages/tesoreria/bovedapage'),
  'BovedaPage',
)
export const MovimientoBovedaPage = lazyNamed(
  () => import('../pages/tesoreria/movimientobovedapage'),
  'MovimientoBovedaPage',
)
