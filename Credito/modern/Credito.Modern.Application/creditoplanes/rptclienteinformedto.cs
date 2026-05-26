namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>ReporteCliente</c>: ficha + <c>dsReporteAval</c>.</summary>
public sealed record RptClienteInformeDto(
    RptClienteFichaDto Ficha,
    IReadOnlyList<RptAvalRowDto> Avales);
