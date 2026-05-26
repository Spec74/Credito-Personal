namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila jqGrid <c>ListarBovedaJgrid</c>.</summary>
public sealed record BovedaListadoRowDto(
    int BovedaId,
    string Tipo,
    decimal SaldoInicial,
    decimal Entradas,
    decimal Salidas,
    decimal SaldoFinal,
    DateTime FechaIniOperacion,
    DateTime? FechaFinOperacion,
    bool IndCierre);
