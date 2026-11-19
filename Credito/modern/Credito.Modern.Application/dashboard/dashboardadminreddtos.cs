namespace Credito.Modern.Application.Dashboard;

/// <summary>Fila de ranking por sede (tablero red Central).</summary>
public sealed record DashboardAdminRedSedeRowDto(
    int OficinaId,
    string NombreOficina,
    decimal CobradoHoy,
    decimal CobradoAyer,
    decimal? VariacionCobradoHoyPct,
    decimal DesembolsoHoy,
    decimal FlujoNetoHoy,
    decimal PorcentajeMora,
    int ClientesMora,
    int CreditosPorVencerSemana,
    int TotalAnalistas,
    int TotalClientes,
    string Semaforo,
    string MotivoSemaforo);

/// <summary>Consolidado de red + liga de sedes (solo Central).</summary>
public sealed record DashboardAdminRedDto(
    DateTime FechaConsulta,
    int TotalSedes,
    decimal CobradoHoy,
    decimal CobradoAyer,
    decimal? VariacionCobradoHoyPct,
    decimal DesembolsoHoy,
    decimal DesembolsoAyer,
    decimal FlujoNetoHoy,
    decimal FlujoNetoAyer,
    decimal PorcentajeMoraRed,
    int ClientesMora,
    int CreditosPorVencerSemana,
    int TotalAnalistas,
    int TotalClientes,
    int SedesEnRojo,
    int SedesEnAmbar,
    int SedesEnVerde,
    IReadOnlyList<DashboardAdminRedSedeRowDto> Sedes);
