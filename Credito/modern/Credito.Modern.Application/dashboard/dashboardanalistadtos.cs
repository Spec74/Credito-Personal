namespace Credito.Modern.Application.Dashboard;

/// <summary>
/// Tablero personal del analista. Paridad funcional de <c>usp_DashboardGestor</c> y
/// vistas asociadas, con variaciones e insights resueltos en servidor.
/// Ranking/podio se conservan vacíos: producción retiró esas comparaciones.
/// </summary>
public sealed record DashboardAnalistaDto(
    string NombreAnalista,
    DateTime FechaConsulta,
    DashboardAnalistaKpisDto Kpis,
    IReadOnlyList<DashboardProductividadPuntoDto> Productividad,
    IReadOnlyList<DashboardRankingRowDto> Ranking,
    IReadOnlyList<DashboardPodioRowDto> PodioMesAnterior,
    IReadOnlyList<DashboardInsightDto> Insights);

public sealed record DashboardAnalistaKpisDto(
    int TotalClientes,
    int ClientesNuevosActual,
    int ClientesNuevosAnterior,
    decimal? VariacionClientesNuevosPct,
    int CreditosActual,
    int CreditosAnterior,
    decimal? VariacionCreditosPct,
    decimal CobradoActual,
    decimal CobradoAnterior,
    decimal? VariacionCobradoPct,
    decimal CobradoHoy,
    decimal CobradoAyer,
    decimal SaldoActual,
    decimal MontoMora,
    int ClientesMora,
    int ClientesMoraSinPago,
    int ClientesMoraNuncaPagaron,
    int ClientesMoraDejaronPagar,
    int ClientesMoraPagandoConAtraso,
    decimal PorcentajeMora,
    int PorVencerSemana);

public sealed record DashboardProductividadPuntoDto(
    DateTime Fecha,
    string Etiqueta,
    decimal MontoCobrado);

public sealed record DashboardRankingRowDto(
    int UsuarioId,
    string NombreCompleto,
    decimal TotalCobrado,
    int Posicion,
    bool EsUsuarioActual);

public sealed record DashboardPodioRowDto(
    int UsuarioId,
    string NombreCompleto,
    decimal TotalCobrado,
    int Posicion);

public sealed record DashboardInsightDto(
    string Tipo,
    string Titulo,
    string Mensaje,
    string? Accion);
