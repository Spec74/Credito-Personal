namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Sesión abierta de <c>CajaChicaDiario</c> del usuario JWT.</summary>
public sealed record CajaChicaSesionDto(
    int Id,
    int UsuarioId,
    decimal SaldoInicial,
    decimal Entradas,
    decimal Salidas,
    decimal SaldoFinal,
    DateTime FechaIniOperacion,
    bool IndCierre);
