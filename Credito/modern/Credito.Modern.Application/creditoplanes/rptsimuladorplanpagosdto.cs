namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad cabecera <c>Reporte/ReporteSimuladorPlanPagos</c> (RDLC rptSimuladorPlanPago).</summary>
public sealed record RptSimuladorPlanPagosCabeceraDto(
    string Monto,
    string Cuotas,
    string Producto,
    string Fecha,
    string Modalidad,
    string Cliente,
    string Tem,
    string Desembolso,
    string GastosAdm,
    string TipoDocumento,
    string NroDocumento,
    string DireccionCliente,
    string DireccionNegocio,
    string PrendaDescripcion,
    string Asesor,
    string TelefonoCliente,
    string InteresesTotales,
    string TotalDevolver,
    string CuotaReferencial,
    string FechaUltimoPago);

public sealed record RptSimuladorPlanPagosInformeDto(
    RptSimuladorPlanPagosCabeceraDto Cabecera,
    IReadOnlyList<SimuladorCreditoCuotaDto> Cuotas);

/// <summary>Parámetros alineados a query MVC <c>ReporteSimuladorPlanPagos</c>.</summary>
public sealed record RptSimuladorPlanPagosQuery(
    int ProductoId,
    decimal Monto,
    int NroCuotas,
    decimal InteresMensual,
    DateTime FechaPrimerPago,
    string FormaPago,
    decimal GastosAdm,
    string Ga,
    string? Cliente,
    string? TipoDocumento,
    string? NroDocumento,
    string? DireccionCliente,
    string? DireccionNegocio,
    string? PrendaDescripcion,
    string? Asesor,
    string? TelefonoCliente);
