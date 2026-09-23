export interface LoginRequest {
  nombreUsuario: string
  clave: string
  oficinaId: number
  clienteAcceso?: string | null
  /** Solo SPA: persiste el refresh en localStorage. No se envÃ­a al API. */
  recordarSesion?: boolean
}

export interface LoginTokenResponse {
  accessToken: string
  expiresInSeconds: number
  refreshToken: string
  refreshExpiresInSeconds: number
  usuarioId: number
  oficinaId: number
  usuarioOficinaId: number
}

export interface AuthMeResponse {
  usuarioId: number
  oficinaId: number
  usuarioOficinaId: number | null
  roles: string[]
}

export interface RefreshRequest {
  refreshToken: string
}

export interface OficinaListItem {
  oficinaId: number
  denominacion: string | null
  indPrincipal: boolean
  estado: boolean
}

export interface MenuItemDto {
  menuId: number
  denominacion: string | null
  modulo: string | null
  url: string | null
  icono: string | null
  indPadre: boolean | null
  orden: number | null
  referencia: number | null
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  type?: string
}

export interface MarcaListItem {
  marcaId: number
  denominacion: string
  estado: boolean
}

export interface ModeloListItem {
  modeloId: number
  denominacion: string
  marcaId: number | null
  estado: boolean
}

export interface TipoArticuloListItem {
  tipoArticuloId: number
  denominacion: string
  descripcion: string | null
  indTieneCodigo: boolean
  indMovimientoAlmacen: boolean
  estado: boolean
}

export interface ArticuloListItem {
  articuloId: number
  modeloId: number | null
  tipoArticuloId: number | null
  codArticulo: string
  denominacion: string
  descripcion: string | null
  indPerecible: boolean | null
  indImportado: boolean | null
  indCanjeable: boolean | null
  estado: boolean
}

export interface AlmacenListItem {
  almacenId: number
  oficinaId: number | null
  denominacion: string
  descripcion: string | null
  indEstadoApertura: boolean | null
  fechaApertura: string | null
  estado: boolean
}

export interface ListaPrecioListItem {
  listaPrecioId: number
  articuloId: number | null
  monto: number | null
  descuento: number | null
  estado: boolean
  puntos: number | null
  puntosCanje: number | null
}

export interface ListarSaldoCarteraRow {
  agenteId: number
  oficinaId: number
  nroDesembolsos: number | null
  montoDesembolsos: number | null
  saldoCartera: number | null
  nroClientesSaldoCartera: number | null
  saldoMoraCartera: number
  nroClientesSaldoMoraCartera: number
  saldoVencido: number
  saldoMorosidad: number
  nroClientesNuevos: number
  fechaCierre: string | null
}

export interface CreditoCicloOperacionResult {
  creditoId: number
  ok: boolean
}

export interface SessionInfo {
  usuarioId: number
  oficinaId: number
  usuarioOficinaId: number
  roles: string[]
}

export interface CajaDiarioVentaRapida {
  cajaDiarioId: number
  cajaId: number
  cajaDenominacion: string
  fechaIniOperacion: string
}

export interface CajaDiarioSesion {
  cajaDiarioId: number
  cajaId: number
  cajaDenominacion: string
  fechaIniOperacion: string
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  indCierre: boolean
  esCajaCentral: boolean
}

export interface ClienteBuscarItem {
  personaId: number
  label: string
}

export interface CreditoPorPersonaRow {
  creditoId: number
  descripcion: string
  montoCredito: number
}

export interface ValorTablaListItem {
  tablaId: number
  itemId: number
  denominacion: string
  desCorta: string | null
  valor: string | null
}

export interface CuotasPendientesRow {
  planPagoId: number | null
  glosa: string | null
  fechaVencimiento: string | null
  amortizacion: number | null
  interes: number | null
  gastosAdm: number | null
  cuota: number | null
  diasAtrazo: number | null
  importeMora: number | null
  descuento: number | null
  cargo: number | null
  pagoLibre: number | null
  pagoCuota: number | null
}

export interface RptSaldosCajaRow {
  movimientoCajaId: number
  operacion: string
  fechaReg: string
  codigo: string | null
  cliente: string | null
  importePago: number
  indEntrada: boolean
  glosa: string | null
  tipoPago: string
  /** false = anulado (arqueo con incluirAnulados) */
  estadoActivo?: boolean
}

export interface CuentaPorCobrarPendienteRow {
  ordenVentaId: number
  cuentaxCobrarId: number
  personaCodigo: string
  personaNombre: string
  operacion: string
  origen: string
  monto: number
  estado: string
  fechaReg: string
}

export interface DesembolsoPendienteRow {
  creditoId: number
  personaCodigo: string
  personaNombre: string
  montoCredito: number
  montoGastosAdm: number
  montoDesembolso: number
  estado: string
}

export interface TipoOperacionListItem {
  tipoOperacionId: number
  codigo: string
  denominacion: string
  indEntrada: boolean
  indCajaDiario: boolean
  indBoveda: boolean
  indCajaChica: boolean
}

export interface PagoCajaResult {
  resultId: number | null
}

export interface ValidarCierreCajaDiario {
  puedeCerrar: boolean
  blockers: string[]
}

export interface CompletarImpagosValidacion {
  cantidadImpagosPendientes: number
}

export interface PagosNoVerificadosRow {
  movimientoCajaId: number
  cliente: string | null
  movimiento: string | null
  importePago: number
  tipoPago: string | null
  fechaTransferencia: string | null
  registro: string | null
}

export interface VerificarPagoTransferenciaResult {
  movimientoCajaId: number
  yaVerificado: boolean
}

export interface SimuladorCreditoRequest {
  monto: number
  formaPago: string
  nroCuotas: number
  interesMensual: number
  fechaPrimerPago: string
  gastosAdm?: number | null
}

export interface SimuladorCreditoCuota {
  numero: number | null
  capital: number | null
  fechaPago: string | null
  amortizacion: number | null
  interes: number | null
  gastosAdm: number | null
  cuota: number | null
  saldo: number | null
}

export interface CalcularTemResult {
  tem: number | null
}

export interface EstadoPlanPagoCuota {
  planPagoId: number
  numero: number
  capital: number
  fechaVencimiento: string
  amortizacion: number
  interes: number
  gastosAdm: number
  cuota: number
  estado: string
  diasAtrazo: number | null
  importeMora: number | null
  descuento: number | null
  cargo: number | null
  pagoLibre: number
  fechaPagoCuota: string | null
  pagoCuota: number | null
  movimientoCajaId: number | null
}

export interface RptMovimientoCreditoRow {
  movimientoCajaId: number | null
  fecha: string | null
  operacion: string | null
  glosa: string | null
  importePago: number | null
  saldo: number | null
}

export interface CobroDiarioParams {
  usuarioId: number
  oficinaId: number
}

/** Informes por gestor/oficina; usuarioId omitido = TODOS (paridad MVC). */
export interface GestorInformeParams {
  oficinaId: number
  usuarioId?: number
}

export interface RptClientesBloqueadosRow {
  agente: string | null
  numeroDocumento: string
  cliente: string | null
  direccion: string | null
  direccionRef: string | null
  celular: string | null
  calificacion: string | null
  nota: string | null
}

export interface RptClientesTopeCreditoRow {
  agente: string | null
  numeroDocumento: string
  cliente: string | null
  direccion: string | null
  direccionRef: string | null
  celular: string | null
  calificacion: string | null
  topeCredito: number | null
  nota: string | null
}

export interface ClientesInactivosParams {
  oficinaId: number
  usuarioId?: number
  fechaIni?: string
  fechaFin?: string
}

export type CreditoVencidoFranja = 'todos' | 'menor60' | 'mayor60' | 'irrecuperable'

export interface CreditoVencidoParams {
  oficinaId: number
  franja: CreditoVencidoFranja
}

export interface RptCreditoVencidoRow {
  gestor: string | null
  creditoId: number
  cliente: string | null
  montoCredito: number
  formaPago: string | null
  fechaVencimiento: string
  creditoVencido: number | null
  vencidoMenor60: string | null
  vencidoMayor60: string | null
  vencidoIrrecuperable: string | null
}

export interface RptCreditoObservadoRow {
  oficinaId: number
  oficina: string | null
  creditoId: number
  cliente: string | null
  fechaPrimerPago: string
  fechaVencimiento: string
  montoCredito: number
  interes: number
  agenteId: number
  agente: string | null
  observacion: string | null
  tramiteAdm: number
  centralRiesgo: number
}

export interface ClientesNuevosMesParams {
  oficinaId: number
  usuarioId?: number
  fechaIni: string
  fechaFin: string
}

export interface CreditoCondonadoParams {
  oficinaId: number
  usuarioId?: number
  fechaIni: string
  fechaFin: string
}

export interface RptCreditoCondonadoRow {
  oficinaId: number
  oficina: string | null
  creditoId: number
  cliente: string | null
  fechaPrimerPago: string
  fechaVencimiento: string
  montoCredito: number
  interes: number
  montoCondonado: number
  agenteId: number
  agente: string | null
  observacion: string | null
}

export interface CreditoMorosidadParams {
  oficinaId: number
  hastaFecha: string
  diasAtrazoIni: number
  diasAtrazoFin: number
}

export interface RptCreditoMorosidadRow {
  creditoId: number
  cliente: string | null
  direccion: string | null
  celular: string | null
  fechaDesembolso: string | null
  fechaVcto: string | null
  articulo: string | null
  montoCredito: number
  saldoCredito: number | null
  fechaUltPago: string | null
  capitalAtrazo: number | null
  ga: number | null
  interesAtrazo: number | null
  mora: number | null
  importeLibre: number | null
  diasAtrazo: number | null
  cuotasAtrazo: number | null
  deudaAtrazo: number | null
}

export interface RptClientesInactivosRow {
  personaId: number
  agente: string | null
  codigo: string | null
  dni: string | null
  cliente: string | null
  direccion: string | null
  direccionRef: string | null
  celular: string | null
  calificacion: string | null
  direccionNegocio: string | null
  direccionNegocioRef: string | null
  montoCredito: number
  topeCredito: number
  fechaCancelacion: string | null
  totalCreditos: number
  diasInactividad: number
  depurado: string | null
  clasificacionRiesgoSBS: string | null
}

export interface RptCobroDiarioRow {
  nro: number | null
  orden: number | null
  creditoId: number
  cliente: string | null
  celular: string | null
  montoCredito: number
  interes: number
  cuotaPlan: number | null
  saldo: number | null
  diasAtrazo: number | null
  nroCuotasPen: number | null
  cuotaTotal: number | null
  direccion: string | null
  fechaPago: string | null
  tienePagoReal: boolean | null
  fechaPrimerPago: string
  fechaVencimiento: string
  mora: number | null
  montoTotal: number | null
  negocio: string | null
  formaPago: string
  topeCredito: number | null
  clasificacionRiesgoSbs: string | null
}

export interface RptCajasAsignadasRow {
  cajaDiarioId: number
  caja: string
  modo: string
  cajero: string | null
  fechaIniOperacion: string
  fechaFinOperacion: string | null
  saldoInicial: number
  salidas: number
  entradas: number
  saldoFinal: number
  resumen: string | null
}

export interface CajaDiarioInformeParams {
  oficinaId: number
  fechaIni: string
  fechaFin: string
  usuarioId?: number
}

export interface RptCajaDiarioRow {
  cajaDiarioId: number
  oficina: string | null
  caja: string | null
  agente: string | null
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  fechaIniOperacion: string
  fechaFinOperacion: string | null
}

export interface CreditoRentabilidadParams {
  oficinaId: number
  fechaIni: string
  fechaFin: string
  estadoCredito: string
}

export interface RptCreditoRentabilidadRow {
  creditoId: number
  oficina: string | null
  codigo: string | null
  cliente: string | null
  fechaDesembolso: string | null
  fechaPago: string | null
  numeroCuotas: number
  formaPago: string | null
  estado: string | null
  montoCredito: number
  interes: number
  sumCuota: number | null
  cuotasPagadas: number | null
  montoGastosAdm: number
  sumInteres: number | null
  sumMora: number | null
  sumPago: number | null
}

export interface InformeRangoGestorParams {
  oficinaId: number
  fechaIni: string
  fechaFin: string
  usuarioId?: number
}

export interface CreditoAprobacionInformeParams {
  oficinaId: number
  fechaAprobacion: string
  usuarioId?: number
}

export interface RptCreditoAprobacionRow {
  creditoId: number
  oficina: string | null
  cliente: string | null
  fechaAprobacion: string | null
  montoCredito: number
  interes: number
  numeroCuotas: number
  montoDesembolso: number
  gestor: string | null
}

export interface RptCreditosActivosRow {
  nro: number | null
  estado: string | null
  agente: string | null
  creditoId: number
  codigo: string | null
  cliente: string | null
  montoCredito: number
  formaPago: string | null
  numeroCuotas: number
  interes: number
  montoInteres: number | null
  montoCreditoTotal: number | null
  montoGastosAdm: number
  centralRiesgo: number
  fechaPrimerPago: string
  fechaVencimiento: string
  nroCuotasPagado: number | null
  pagado: number | null
  interesPagado: number
  nroCuotasPen: number | null
  saldoCapital: number | null
  saldoInteres: number | null
  saldo: number | null
  diasAtrazo: number | null
  mora: number | null
}

export interface RptCreditosCierresRow {
  creditoId: number
  estado: string | null
  agente: string | null
  codigo: string | null
  cliente: string | null
  montoCredito: number
  formaPago: string | null
  numeroCuotas: number
  interes: number
  montoGastosAdm: number
  centralRiesgo: number
  fechaPrimerPago: string
  fechaVencimiento: string
  sumAmortizacion: number | null
  sumInteres: number | null
  sumCuota: number | null
  sumNroCuota: number | null
}

export interface RptCreditosMorososPagadosRow {
  creditoId: number
  cliente: string | null
  montoCredito: number
  interes: number
  formaPago: string | null
  numeroCuotas: number
  montoGastosAdm: number
  centralRiesgo: number
  fechaPrimerPago: string
  fechaVencimiento: string
  fechaPagado: string | null
  agente: string | null
}

export interface ComprobantesCajaChicaParams {
  fechaIni: string
  fechaFin: string
}

export interface RptComprobantesCajaChicaRow {
  gasto: string | null
  fecha: string
  documento: string | null
  serie: string | null
  numero: string | null
  ruc: string | null
  razonSocial: string | null
  detalleGasto: string | null
  importe: number
}

export interface RptPlanPagosRow {
  numero: number
  capital: number
  fechaPago: string
  amortizacion: number
  interes: number
  gastosAdm: number
  cuota: number
}

export interface RptEstadoCreditoCabecera {
  creditoId: number
  personaId: number
  producto: string
  fechaPrimerPago: string
  fechaVencimiento: string
  montoCredito: number
  modalidad: string
  numeroCuotas: number
  interes: number
  estado: string
  codigoPersona: string
  cliente: string
  analista: string
  montoGastosAdm: number
  total: number
}

export interface RptEstadoCreditoInforme {
  cabecera: RptEstadoCreditoCabecera
  cuotas: EstadoPlanPagoCuota[]
}

export interface RptCreditoTareaRow {
  nro: number
  tareaId: number
  creditoId: number
  cliente: string
  analista: string
  subtareasResumen: string
  detalleSubtareas: string
  estado: string
}

export interface CreditoTareaReportParams {
  estado?: string
}

export interface RptClienteFicha {
  personaId: number
  creditosDesembolsados: number
  cliente: string
  numeroDocumento: string
  fechaNacimiento: string | null
  sexo: string | null
  direccion: string | null
  direccionRef: string | null
  celular: string | null
  conyugue: string
  conyugueDni: string
  conyugueCelular: string
  tipoVivienda: string
  estadoCivil: string
  distrito: string
  actividadEconomica: string
  nota: string | null
  direccionNegocio: string | null
  direccionNegocioRef: string | null
}

export interface RptClienteInforme {
  ficha: RptClienteFicha
  avales: RptAvalRow[]
}

export interface CodigoBarrasLstRow {
  serie1: string | null
  articulo1: string | null
  precio1: number
  serie2: string | null
  articulo2: string | null
  precio2: number
}

export interface MovimientoCajaAnuladoParams {
  oficinaId: number
  fechaIni: string
  fechaFin: string
}

export interface RptMovimientoCajaAnuladoRow {
  movimientoCajaId: number
  operacion: string | null
  importePago: number
  persona: string | null
  descripcion: string | null
  fechaReg: string
  usuarioRegistro: string | null
  motivoAnulacion: string | null
  fechaAnulacion: string | null
  usuarioAnulacion: string | null
}

export interface ReporteCreditoParams {
  oficinaId: number
  fechaIni: string
  fechaFin: string
  estadoCredito: string
  gestorId?: number
}

export interface RptCreditoRow {
  producto: string | null
  cliente: string | null
  creditoId: number
  fechaDesembolso: string | null
  fechaVcto: string | null
  formaPago: string | null
  numeroCuotas: number
  interes: number
  estado: string | null
  montoProducto: number
  montoInicial: number
  montoCredito: number
  tipoGastoAdm: string | null
  montoGastosAdm: number
  montoDesembolso: number
  observacion: string | null
}

export interface SaldoCarteraCajaDiarioParams {
  oficinaId: number
  anioIni: number
  mesIni: number
  anioFin: number
  mesFin: number
  usuarioId?: number
}

export interface RptSaldoCarteraCajaDiarioRow {
  agenteId: number
  oficina: string | null
  caja: string | null
  agente: string | null
  fechaCierreIni: string | null
  salidasIni: number
  montoCobradoIni: number | null
  pocentajeCobroIni: number | null
  saldoCarteraSinMoraIni: number
  nroClientesCarteraSinMoraIni: number
  saldoMoraCarteraIni: number
  nroClientesSaldoMoraCarteraIni: number
  nroClientesNuevosIni: number
  saldoVencidoIni: number
  saldoMorosidadIni: number
  fechaCierreFin: string | null
  salidasFin: number
  montoCobradoFin: number
  pocentajeCobroFin: number | null
  saldoCarteraSinMoraFin: number
  nroClientesCarteraSinMoraFin: number
  saldoMoraCarteraFin: number
  nroClientesSaldoMoraCarteraFin: number
  nroClientesNuevosFin: number
  saldoVencidoFin: number
  saldoMorosidadFin: number
}

export interface RptCobroDiarioDetalleRow {
  nro: number | null
  cliente: string | null
  formaPago: string
  montoCredito: number
  interes: number
  montoTotal: number | null
  fechaPrimerPago: string
  fechaVencimiento: string
  saldo: number | null
  totalPago: number | null
  diasAtrazoMora: number | null
  pagos: string | null
}

export interface RptAvalRow {
  grupo: string
  creditoId: number
  montoCredito: number
  estado: string
  persona: string | null
  dni: string | null
  celular: string | null
}

export interface ListaPrecioInformeParams {
  marcaId?: number
  indDescuento: boolean
  indPuntos: boolean
}

export interface RptListaPrecioGeneralRow {
  articuloId: number
  tipoArticulo: string | null
  articuloDes: string | null
  monto: number
  descuento: number | null
  puntosCanje: number | null
}

export interface GenerarKardexParams {
  oficinaId: number
  articuloId: number
  almacenId: number
}

export interface GenerarKardexRow {
  movimientoDetId: number | null
  fecha: string | null
  concepto: string | null
  cantEnt: number | null
  puEnt: number | null
  totalEnt: number | null
  cantSal: number | null
  puSal: number | null
  totalSal: number | null
  cantSaldo: number | null
  puSaldo: number | null
  totalSaldo: number | null
}

export interface CajaDiarioOperacionResponse {
  resultCode: number
  cajaDiarioId: number | null
}

export interface ValidarAnularCreditoResponse {
  puedeAnular: boolean
}

export interface CreditoCicloOperacionResponse {
  creditoId: number
  ok?: boolean
}

export interface CrearSolicitudCreditoResponse {
  solicitudCreditoId: number
}

export interface CrearCreditoResponse {
  mensaje: string
}

export interface ProductoListItem {
  productoId: number
  denominacion: string
  interesMinima: number
  interesMaxima: number
  diasGracia: number
  importeMoratorio: number
  estado: boolean
  indMora: boolean
}

