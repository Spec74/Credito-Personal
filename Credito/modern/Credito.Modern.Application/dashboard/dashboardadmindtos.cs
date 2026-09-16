namespace Credito.Modern.Application.Dashboard;

/// <summary>
/// Tablero gerencial de oficina. Paridad de <c>/Dashboard/Admin</c> (usp_DashboardAdmin*),
/// acotado a la oficina del JWT.
/// </summary>
public sealed record DashboardAdminDto(
    string NombreOficina,
    DateTime FechaConsulta,
    DashboardAdminResumenDto Resumen,
    IReadOnlyList<DashboardAdminFlujoRowDto> FlujoCaja,
    IReadOnlyList<DashboardAdminHistoricoPuntoDto> Historico,
    IReadOnlyList<DashboardAdminHistoricoMensualDto> HistoricoMensual,
    IReadOnlyList<DashboardAdminAnalistaRowDto> Analistas);

/// <summary>KPIs + cartera para progressive load del tablero gerencial.</summary>
public sealed record DashboardAdminShellDto(
    string NombreOficina,
    DateTime FechaConsulta,
    DashboardAdminResumenDto Resumen);

/// <summary>Flujo, históricos y analistas (carga diferida).</summary>
public sealed record DashboardAdminDetalleDto(
    IReadOnlyList<DashboardAdminFlujoRowDto> FlujoCaja,
    IReadOnlyList<DashboardAdminHistoricoPuntoDto> Historico,
    IReadOnlyList<DashboardAdminHistoricoMensualDto> HistoricoMensual,
    IReadOnlyList<DashboardAdminAnalistaRowDto> Analistas);

public sealed record DashboardAdminResumenDto(
    int TotalAnalistas,
    int TotalClientes,
    int CreditosHoy,
    int CreditosAyer,
    int CreditosAnteayer,
    int CreditosMesActual,
    int CreditosMesAnteriorComparable,
    decimal? VariacionCreditosHoyPct,
    decimal? VariacionCreditosMesPct,
    decimal DesembolsoHoy,
    decimal DesembolsoAyer,
    decimal DesembolsoAnteayer,
    decimal DesembolsoMesActual,
    decimal DesembolsoMesAnteriorComparable,
    decimal? VariacionDesembolsoHoyPct,
    decimal? VariacionDesembolsoMesPct,
    decimal CobradoHoy,
    decimal CobradoAyer,
    decimal CobradoAnteayer,
    decimal CobradoMesActual,
    decimal CobradoMesAnteriorComparable,
    decimal? VariacionCobradoHoyPct,
    decimal? VariacionCobradoMesPct,
    decimal EntradasHoy,
    decimal SalidasHoy,
    decimal FlujoNetoHoy,
    decimal EntradasAyer,
    decimal SalidasAyer,
    decimal FlujoNetoAyer,
    decimal EntradasAnteayer,
    decimal SalidasAnteayer,
    decimal FlujoNetoAnteayer,
    decimal EntradasMesActual,
    decimal SalidasMesActual,
    decimal FlujoNetoMesActual,
    decimal EntradasMesAnteriorComparable,
    decimal SalidasMesAnteriorComparable,
    decimal FlujoNetoMesAnteriorComparable,
    decimal? VariacionFlujoHoyPct,
    decimal? VariacionFlujoMesPct,
    decimal SaldoCartera,
    decimal SaldoCreditos,
    decimal SaldoMoraCartera,
    decimal SaldoVencido,
    decimal SaldoMorosidad,
    int ClientesMora,
    int CreditosPorVencerSemana);

public sealed record DashboardAdminFlujoRowDto(
    string Operacion,
    bool IndEntrada,
    string Concepto,
    bool EsTransferencia,
    int CantidadHoy,
    decimal ImporteHoy,
    int CantidadAyer,
    decimal ImporteAyer,
    int CantidadMesActual,
    decimal ImporteMesActual,
    int CantidadMesAnteriorComparable,
    decimal ImporteMesAnteriorComparable);

public sealed record DashboardAdminHistoricoPuntoDto(
    DateTime Fecha,
    string Etiqueta,
    int Colocaciones,
    decimal Desembolsado,
    decimal Cobrado,
    decimal Entradas,
    decimal Salidas,
    decimal FlujoNeto,
    decimal FlujoOperativo);

public sealed record DashboardAdminHistoricoMensualDto(
    DateTime FechaMes,
    bool EsMesActual,
    string Etiqueta,
    int Colocaciones,
    decimal Desembolsado,
    decimal Cobrado,
    decimal Entradas,
    decimal Salidas,
    decimal FlujoNeto,
    decimal FlujoOperativo);

public sealed record DashboardAdminAnalistaRowDto(
    int UsuarioId,
    string NombreCompleto,
    int TotalClientes,
    int ClientesNuevosMes,
    int ColocacionesHoy,
    int ColocacionesMes,
    decimal DesembolsoHoy,
    decimal DesembolsoMes,
    decimal CobradoHoy,
    decimal CobradoMes,
    decimal CobradoMesAnteriorComparable,
    decimal? VariacionCobranzaPct,
    int ClientesMora,
    decimal MontoMora,
    decimal PorcentajeMora);
